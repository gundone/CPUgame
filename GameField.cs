using System.Linq;
using CPUgame.Components;
using CPUgame.Core;
using CPUgame.Input;
using CPUgame.Rendering;
using CPUgame.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame;

public class GameField : Game, IGameField
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;

    private readonly IPlatformServices _platformServices;
    private readonly IInputHandler _inputHandler;
    private readonly InputState _inputState = new();

    private readonly IStatusService _statusService;
    private readonly ICircuitManager _circuitManager;
    private readonly IWireManager _wireManager;
    private readonly ICommandHandler _commandHandler;
    private readonly IToolboxManager _toolboxManager;
    private readonly IGameRenderer _gameRenderer;
    private readonly ITruthTableService _truthTableService;
    private readonly ILevelService _levelService;
    private readonly IProfileService _profileService;

    private CameraController _camera = null!;
    private SelectionManager _selection = null!;
    private ComponentBuilder _componentBuilder = null!;
    private DialogService _dialogService = null!;
    private MainMenu _mainMenu = null!;
    private ProfileDialog _profileDialog = null!;
    private LevelSelectionPopup _levelSelectionPopup = null!;
    private LevelDescriptionPopup _levelDescriptionPopup = null!;
    private LevelCompletedPopup _levelCompletedPopup = null!;

    private int ScreenWidth => GraphicsDevice?.Viewport.Width ?? _graphics.PreferredBackBufferWidth;
    private int ScreenHeight => GraphicsDevice?.Viewport.Height ?? _graphics.PreferredBackBufferHeight;

    public GameField(IPlatformServices platformServices, IInputHandler inputHandler,
        IStatusService statusService, ICircuitManager circuitManager, IWireManager wireManager,
        ICommandHandler commandHandler, IToolboxManager toolboxManager, IGameRenderer gameRenderer,
        ITruthTableService truthTableService, ILevelService levelService, IProfileService profileService)
    {
        _platformServices = platformServices;
        _inputHandler = inputHandler;
        _statusService = statusService;
        _circuitManager = circuitManager;
        _wireManager = wireManager;
        _commandHandler = commandHandler;
        _toolboxManager = toolboxManager;
        _gameRenderer = gameRenderer;
        _truthTableService = truthTableService;
        _levelService = levelService;
        _profileService = profileService;

        _graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 1280, PreferredBackBufferHeight = 720 };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        _platformServices.EnsureDirectoryExists(_platformServices.GetComponentsFolder());
    }

    protected override void Initialize()
    {
        LocalizationManager.Initialize();

        _camera = new CameraController();
        _selection = new SelectionManager(_circuitManager.Circuit);
        _componentBuilder = new ComponentBuilder(_circuitManager.CustomComponents, _platformServices);
        _dialogService = new DialogService(_statusService, _componentBuilder);

        _componentBuilder.OnComponentCreated += name =>
        {
            _toolboxManager.UserToolbox.AddCustomComponent(name);
            _statusService.Show(LocalizationManager.Get("status.component_created", name));
        };
        _componentBuilder.OnComponentDeleted += name =>
        {
            _toolboxManager.UserToolbox.RemoveCustomComponent(name);
            _statusService.Show(LocalizationManager.Get("status.component_deleted", name));
        };
        _componentBuilder.OnError += msg => _statusService.Show(LocalizationManager.Get("status.component_failed", msg));

        _commandHandler.OnBuildComponent += () =>
        {
            _dialogService.StartNaming(_selection.GetSelectedComponents());
            if (_dialogService.IsActive)
            {
                _inputHandler.BeginTextInput();
            }
        };

        _dialogService.OnNameConfirmed += (name, components) =>
            _componentBuilder.BuildComponent(name, components, _gameRenderer.GridSize);

        _dialogService.OnTitleConfirmed += (component, newTitle) =>
        {
            component.Title = newTitle;
        };

        _circuitManager.OnCircuitChanged += () => _selection = new SelectionManager(_circuitManager.Circuit);

        Window.Title = LocalizationManager.Get("app.title");
        Window.ClientSizeChanged += (_, _) =>
        {
            if (Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();
            }
        };

        _componentBuilder.LoadCustomComponents();
        _statusService.Show(LocalizationManager.Get("help.drag"));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
        _gameRenderer.Initialize(GraphicsDevice, _font);
        _toolboxManager.Initialize(ScreenWidth, _componentBuilder);
        _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
        _truthTableService.Initialize(ScreenWidth);

        _mainMenu = new MainMenu();
        _mainMenu.OnNewCircuit += _circuitManager.NewCircuit;
        _mainMenu.OnLoadCircuit += _circuitManager.LoadCircuit;
        _mainMenu.OnSaveCircuit += _circuitManager.SaveCircuit;
        _mainMenu.OnExit += Exit;
        _mainMenu.OnLanguageChanged += code =>
        {
            LocalizationManager.LoadLanguage(code);
            Window.Title = LocalizationManager.Get("app.title");
            _statusService.Show(LocalizationManager.Get("status.ready"));
        };
        _mainMenu.OnTitleFontSizeChanged += scale =>
        {
            _gameRenderer.TitleFontScale = scale;
        };
        _mainMenu.OnToggleTruthTable += () =>
        {
            _truthTableService.IsVisible = !_truthTableService.IsVisible;
        };

        // Level service setup
        _levelService.LoadLevels();
        _mainMenu.SetLevels(_levelService.Levels);
        _mainMenu.SetCurrentMode(_levelService.CurrentMode);

        // Profile and level selection dialogs
        _profileDialog = new ProfileDialog(_profileService);
        _levelSelectionPopup = new LevelSelectionPopup(_levelService, _profileService);
        _levelDescriptionPopup = new LevelDescriptionPopup();
        _levelCompletedPopup = new LevelCompletedPopup();

        _mainMenu.OnSandboxMode += () =>
        {
            _levelService.SetMode(GameMode.Sandbox);
            _mainMenu.SetCurrentMode(GameMode.Sandbox);
            _mainMenu.SetProfileName(null);
            _truthTableService.SetCurrentLevel(null);
            _toolboxManager.SetLevelModeFilter(false, null);
            _statusService.Show(LocalizationManager.Get("status.mode_sandbox"));
        };
        _mainMenu.OnLevelsMode += () =>
        {
            if (!_profileService.HasProfile)
            {
                _profileDialog.Show();
                _statusService.Show(LocalizationManager.Get("status.profile_required"));
                return;
            }
            _levelService.SetMode(GameMode.Levels);
            _mainMenu.SetCurrentMode(GameMode.Levels);
            _mainMenu.SetProfileName(_profileService.CurrentProfile?.Name);
            _toolboxManager.SetLevelModeFilter(true, _profileService.GetUnlockedComponents(_levelService));
            if (_levelService.CurrentLevel != null)
            {
                _truthTableService.SetCurrentLevel(_levelService.CurrentLevel);
                _truthTableService.IsVisible = true;
            }
            _statusService.Show(LocalizationManager.Get("status.mode_levels"));
        };
        _mainMenu.OnSelectLevelPopup += () =>
        {
            if (!_profileService.HasProfile)
            {
                _profileDialog.Show();
                _statusService.Show(LocalizationManager.Get("status.profile_required"));
                return;
            }
            _levelSelectionPopup.Show();
        };

        _levelSelectionPopup.OnLevelSelected += index =>
        {
            _levelService.SelectLevel(index);
            if (_levelService.CurrentLevel != null)
            {
                // Show level description popup instead of immediately starting
                _levelDescriptionPopup.Show(_levelService.CurrentLevel);
            }
        };

        _levelDescriptionPopup.OnStartLevel += () =>
        {
            if (_levelService.CurrentLevel != null)
            {
                _levelService.SetupLevelCircuit(_circuitManager.Circuit, _gameRenderer.GridSize);
                _selection = new SelectionManager(_circuitManager.Circuit);
                _truthTableService.SetCurrentLevel(_levelService.CurrentLevel);
                _truthTableService.IsVisible = true;
            }
        };

        _profileDialog.OnProfileSelected += () =>
        {
            _mainMenu.SetProfileName(_profileService.CurrentProfile?.Name);
            _toolboxManager.SetLevelModeFilter(true, _profileService.GetUnlockedComponents(_levelService));
            _levelService.SetMode(GameMode.Levels);
            _mainMenu.SetCurrentMode(GameMode.Levels);
        };

        _truthTableService.OnLevelPassed += () =>
        {
            if (_levelService.CurrentLevel != null)
            {
                var level = _levelService.CurrentLevel;

                // Auto-build the component from the circuit
                var componentsToSave = _circuitManager.Circuit.Components.ToList();
                if (componentsToSave.Count > 0)
                {
                    _componentBuilder.BuildComponent(level.ComponentName, componentsToSave, _gameRenderer.GridSize);
                }

                _profileService.CompleteLevel(level.Id);
                _toolboxManager.SetLevelModeFilter(true, _profileService.GetUnlockedComponents(_levelService));
                // Refresh custom components in toolbox
                _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
                // Show completed popup
                _levelCompletedPopup.Show(level, _levelService.NextLevelInfo);
            }
        };

        _levelCompletedPopup.OnNextLevel += () =>
        {
            _levelService.NextLevel();
            if (_levelService.CurrentLevel != null)
            {
                _levelDescriptionPopup.Show(_levelService.CurrentLevel);
            }
        };

        _levelCompletedPopup.OnClose += () =>
        {
            // Just close, stay on current level
        };
    }

    protected override void Update(GameTime gameTime)
    {
        _inputState.Clear();
        _inputHandler.Update(_inputState, gameTime.ElapsedGameTime.TotalSeconds);
        var mousePos = _inputState.PointerPosition;
        var circuit = _circuitManager.Circuit;

        _statusService.Update(gameTime.ElapsedGameTime.TotalSeconds);

        // Handle profile dialog (modal, blocks everything else)
        if (_profileDialog.IsVisible)
        {
            _profileDialog.HandleInput(_inputState, _inputHandler);
            _profileDialog.Update(mousePos, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight, _inputHandler);
            base.Update(gameTime);
            return;
        }

        // Handle level selection popup (modal, blocks everything else)
        if (_levelSelectionPopup.IsVisible)
        {
            _levelSelectionPopup.Update(mousePos, _inputState.PrimaryJustPressed, _inputState.ScrollDelta, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle level description popup (modal)
        if (_levelDescriptionPopup.IsVisible)
        {
            _levelDescriptionPopup.Update(mousePos, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle level completed popup (modal)
        if (_levelCompletedPopup.IsVisible)
        {
            _levelCompletedPopup.Update(mousePos, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        if (_dialogService.IsActive)
        {
            _dialogService.HandleInput(_inputState, _inputHandler);
            base.Update(gameTime);
            return;
        }

        _mainMenu.Update(mousePos, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, ScreenWidth);
        if (_mainMenu.ContainsPoint(mousePos)) { base.Update(gameTime); return; }

        // Update truth table window
        _truthTableService.Update(mousePos, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, _inputState.ScrollDelta, circuit, gameTime.ElapsedGameTime.TotalSeconds);
        if (_truthTableService.ContainsPoint(mousePos)) { base.Update(gameTime); return; }

        if (_inputState.CtrlHeld && _inputState.ScrollDelta != 0)
        {
            _camera.HandleZoom(_inputState.ScrollDelta, mousePos, _camera.ScreenToWorld);
        }

        var worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        bool clickedOnEmpty = !_toolboxManager.ContainsPoint(mousePos) && !_truthTableService.ContainsPoint(mousePos) &&
                              circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y) == null &&
                              circuit.GetPinAt(worldMousePos.X, worldMousePos.Y) == null;

        if ((_inputState.MiddleJustPressed || _inputState.SecondaryJustPressed) && !_toolboxManager.ContainsPoint(mousePos) && !_truthTableService.ContainsPoint(mousePos))
        {
            _camera.StartPan(mousePos);
        }

        if (_inputState.PrimaryJustPressed && clickedOnEmpty && !_toolboxManager.IsInteracting && !_truthTableService.IsInteracting && !_camera.IsPanning)
        {
            _selection.StartSelectionRect(worldMousePos, _inputState.CtrlHeld);
        }

        if (_selection.IsSelecting)
        {
            _selection.UpdateSelectionRect(worldMousePos);
            if (_inputState.PrimaryJustReleased) _selection.CompleteSelectionRect();
        }

        if (_camera.IsPanning)
        {
            if (_inputState.MiddlePressed || _inputState.SecondaryPressed) _camera.UpdatePan(mousePos);
            else _camera.EndPan();
        }

        _commandHandler.HandleCommands(_inputState, _selection, circuit, _wireManager, _gameRenderer.GridSize);
        _toolboxManager.Update(mousePos, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased);

        worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        var placedComponent = _toolboxManager.HandleDrops(mousePos, worldMousePos, circuit, _gameRenderer.GridSize, _commandHandler.ShowPinValues, _inputState.PrimaryJustReleased, _statusService, _componentBuilder);
        if (placedComponent != null)
            _selection.SelectComponent(placedComponent);

        if (_toolboxManager.IsInteracting)
            _wireManager.Cancel();
        else if (!_toolboxManager.ContainsPoint(mousePos))
        {
            foreach (var clock in circuit.Components.OfType<Clock>())
                clock.Update(gameTime.ElapsedGameTime.TotalSeconds);

            HandleCircuitInteraction(worldMousePos, circuit);
            circuit.Simulate();
        }

        _inputHandler.SetCursor(_camera.IsPanning || _selection.IsDragging || _toolboxManager.IsInteracting ? CursorType.Move : CursorType.Arrow);
        base.Update(gameTime);
    }

    private void HandleCircuitInteraction(Point worldMousePos, Circuit circuit)
    {
        _wireManager.Update(circuit, worldMousePos, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased);
        if (_wireManager.IsDraggingWire)
        {
            return;
        }

        // Handle double-click for title editing
        if (_inputState.PrimaryDoubleClick)
        {
            var component = circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);
            if (component != null)
            {
                _dialogService.StartEditingTitle(component);
                _inputHandler.BeginTextInput();
                return;
            }
        }

        if (_inputState.PrimaryJustPressed)
        {
            var component = circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);
            if (component != null)
            {
                _selection.HandleComponentClick(component, _inputState.CtrlHeld, worldMousePos);
            }
            else
            {
                var wire = circuit.GetWireAt(worldMousePos.X, worldMousePos.Y);
                if (wire != null)
                {
                    _selection.HandleWireClick(wire);
                    _statusService.Show(LocalizationManager.Get("status.wire_selected"));
                }
                else
                {
                    _selection.HandleEmptyClick(_inputState.CtrlHeld);
                }
            }
        }

        if (_inputState.PrimaryPressed)
        {
            _selection.UpdateDrag(worldMousePos, _gameRenderer.GridSize);
        }
        if (_inputState.PrimaryJustReleased)
        {
            _selection.EndDrag();
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(CircuitRenderer.BackgroundColor);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetTransform());
        _gameRenderer.DrawWorld(_spriteBatch, _circuitManager.Circuit, _camera, _selection, _wireManager, _wireManager.HoveredPin, _inputState.PointerPosition, ScreenWidth, ScreenHeight, _toolboxManager.MainToolbox.IsDraggingItem);
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _gameRenderer.DrawUI(_spriteBatch, _toolboxManager, _mainMenu, _statusService, _dialogService, _truthTableService, _camera, _inputState.PointerPosition, ScreenWidth, ScreenHeight, _font);

        // Draw modal dialogs on top
        _profileDialog.Draw(_spriteBatch, _gameRenderer.Pixel, _font, ScreenWidth, ScreenHeight);
        _levelSelectionPopup.Draw(_spriteBatch, _gameRenderer.Pixel, _font, ScreenWidth, ScreenHeight);
        _levelDescriptionPopup.Draw(_spriteBatch, _gameRenderer.Pixel, _font, ScreenWidth, ScreenHeight);
        _levelCompletedPopup.Draw(_spriteBatch, _gameRenderer.Pixel, _font, ScreenWidth, ScreenHeight);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
