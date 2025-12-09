using System.Linq;
using CPUgame.Core;
using CPUgame.Core.Components;
using CPUgame.Core.Input;
using CPUgame.Core.Primitives;
using CPUgame.Converters;
using CPUgame.Core.Circuit;
using CPUgame.Core.Levels;
using CPUgame.Core.Localization;
using CPUgame.Core.Selection;
using CPUgame.Core.Services;
using CPUgame.Rendering;
using CPUgame.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame;

public class GameField : Game, IGameField
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private readonly IInputHandler _inputHandler;
    private readonly InputState _inputState = new();

    private readonly IStatusService _statusService;
    private readonly ICircuitManager _circuitManager;
    private readonly ICameraController _camera;
    private readonly IComponentBuilder _componentBuilder;
    private readonly IDialogService _dialogService;
    private readonly IFontService _fontService;
    private readonly IWireManager _wireManager;
    private readonly IManualWireService _manualWireService;
    private readonly ICommandHandler _commandHandler;
    private readonly IToolboxManager _toolboxManager;
    private readonly IGameRenderer _gameRenderer;
    private readonly ITruthTableService _truthTableService;
    private readonly ILevelService _levelService;
    private readonly IProfileService _profileService;

    private ISelectionManager _selection = null!;
    private MainMenu _mainMenu = null!;
    private ProfileDialog _profileDialog = null!;
    private LevelSelectionPopup _levelSelectionPopup = null!;
    private LevelDescriptionPopup _levelDescriptionPopup = null!;
    private LevelCompletedPopup _levelCompletedPopup = null!;
    private ComponentEditDialog _componentEditDialog = null!;
    private ControlsPopup _controlsPopup = null!;

    private int ScreenWidth => GraphicsDevice?.Viewport.Width ?? _graphics.PreferredBackBufferWidth;
    private int ScreenHeight => GraphicsDevice?.Viewport.Height ?? _graphics.PreferredBackBufferHeight;

    public GameField(IPlatformServices platformServices, IInputHandler inputHandler,
        IStatusService statusService, ICircuitManager circuitManager, ICameraController cameraController,
        IComponentBuilder componentBuilder, IDialogService dialogService, IFontService fontService,
        IWireManager wireManager, IManualWireService manualWireService, ICommandHandler commandHandler,
        IToolboxManager toolboxManager, IGameRenderer gameRenderer, ITruthTableService truthTableService,
        ILevelService levelService, IProfileService profileService)
    {
        _inputHandler = inputHandler;
        _statusService = statusService;
        _circuitManager = circuitManager;
        _camera = cameraController;
        _componentBuilder = componentBuilder;
        _dialogService = dialogService;
        _fontService = fontService;
        _wireManager = wireManager;
        _manualWireService = manualWireService;
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

        platformServices.EnsureDirectoryExists(platformServices.GetComponentsFolder());
    }

    protected override void Initialize()
    {
        LocalizationManager.Initialize();

        _selection = new SelectionManager(_circuitManager.Circuit);

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
        _fontService.Initialize(GraphicsDevice);
        _gameRenderer.Initialize(GraphicsDevice, _fontService);
        _toolboxManager.Initialize(ScreenWidth, _componentBuilder);
        _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
        _truthTableService.Initialize(ScreenWidth);

        _mainMenu = new MainMenu();
        _mainMenu.OnNewCircuit += _circuitManager.NewCircuit;
        _mainMenu.OnLoadCircuit += _circuitManager.LoadCircuit;
        _mainMenu.OnSaveCircuit += _circuitManager.SaveCircuit;
        _mainMenu.OnSaveCircuitAs += _circuitManager.SaveCircuitAs;
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
        _componentEditDialog = new ComponentEditDialog();
        _controlsPopup = new ControlsPopup();

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
                _truthTableService.Show(_circuitManager.Circuit, _fontService.GetFont());
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
        _mainMenu.OnShowControls += () =>
        {
            _controlsPopup.Show();
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
                _truthTableService.Show(_circuitManager.Circuit, _fontService.GetFont());
            }
        };

        _profileDialog.OnProfileSelected += () =>
        {
            _mainMenu.SetProfileName(_profileService.CurrentProfile?.Name);
            _toolboxManager.SetLevelModeFilter(true, _profileService.GetUnlockedComponents(_levelService));
            _levelService.SetMode(GameMode.Levels);
            _mainMenu.SetCurrentMode(GameMode.Levels);
            _levelSelectionPopup.Show();
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
        var mousePosMonoGame = mousePos.ToMonoGame();
        var circuit = _circuitManager.Circuit;

        _statusService.Update(gameTime.ElapsedGameTime.TotalSeconds);

        // Handle profile dialog (modal, blocks everything else)
        if (_profileDialog.IsVisible)
        {
            _profileDialog.HandleInput(_inputState, _inputHandler);
            _profileDialog.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight, _inputHandler);
            base.Update(gameTime);
            return;
        }

        // Handle level selection popup (modal, blocks everything else)
        if (_levelSelectionPopup.IsVisible)
        {
            _levelSelectionPopup.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, _inputState.ScrollDelta, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle level description popup (modal)
        if (_levelDescriptionPopup.IsVisible)
        {
            _levelDescriptionPopup.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle level completed popup (modal)
        if (_levelCompletedPopup.IsVisible)
        {
            _levelCompletedPopup.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle controls popup (modal)
        if (_controlsPopup.IsVisible)
        {
            _controlsPopup.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle component edit dialog (modal)
        if (_componentEditDialog.IsVisible)
        {
            _componentEditDialog.HandleInput(_inputState, _inputHandler);
            _componentEditDialog.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        if (_dialogService.IsActive)
        {
            _dialogService.HandleInput(_inputState, _inputHandler);
            base.Update(gameTime);
            return;
        }

        _mainMenu.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, ScreenWidth);
        if (_mainMenu.ContainsPoint(mousePosMonoGame)) { base.Update(gameTime); return; }

        // Update truth table window
        _truthTableService.Update(mousePosMonoGame, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, _inputState.ScrollDelta, circuit, gameTime.ElapsedGameTime.TotalSeconds);
        if (_truthTableService.ContainsPoint(mousePosMonoGame)) { base.Update(gameTime); return; }

        if (_inputState.CtrlHeld && _inputState.ScrollDelta != 0)
        {
            _camera.HandleZoom(_inputState.ScrollDelta, mousePos, _camera.ScreenToWorld);
        }

        var worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        bool clickedOnEmpty = !_toolboxManager.ContainsPoint(mousePosMonoGame) && !_truthTableService.ContainsPoint(mousePosMonoGame) &&
                              circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y) == null &&
                              circuit.GetPinAt(worldMousePos.X, worldMousePos.Y) == null;

        if ((_inputState.MiddleJustPressed || _inputState.SecondaryJustPressed) && !_toolboxManager.ContainsPoint(mousePosMonoGame) && !_truthTableService.ContainsPoint(mousePosMonoGame))
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
        _toolboxManager.Update(mousePosMonoGame, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased);

        worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        var placedComponent = _toolboxManager.HandleDrops(mousePosMonoGame, worldMousePos.ToMonoGame(), circuit, _gameRenderer.GridSize, _commandHandler.ShowPinValues, _inputState.PrimaryJustReleased, _statusService, _componentBuilder);
        if (placedComponent != null)
        {
            _selection.SelectComponent(placedComponent);
        }

        if (_toolboxManager.IsInteracting)
        {
            _wireManager.Cancel();
        }
        else if (!_toolboxManager.ContainsPoint(mousePosMonoGame))
        {
            foreach (var clock in circuit.Components.OfType<Clock>())
                clock.Update(gameTime.ElapsedGameTime.TotalSeconds);

            HandleCircuitInteraction(worldMousePos, circuit);
            circuit.Simulate();
        }

        _inputHandler.SetCursor(_camera.IsPanning || _selection.IsDragging || _toolboxManager.IsInteracting ? CursorType.Move : CursorType.Arrow);
        base.Update(gameTime);
    }

    private void HandleCircuitInteraction(Point2 worldMousePos, Circuit circuit)
    {
        // Handle manual wire mode
        if (_manualWireService.IsActive)
        {
            HandleManualWireMode(worldMousePos, circuit);
            return;
        }

        // Handle wire node editing mode
        if (_manualWireService.IsEditingWire)
        {
            HandleWireNodeEditing(worldMousePos);
            return;
        }

        // Shift+Click on pin starts auto wire mode
        _wireManager.Update(circuit, worldMousePos, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, _inputState.ShiftHeld);
        if (_wireManager.IsDraggingWire)
        {
            return;
        }

        // Click on pin (without Shift) starts manual wire mode
        if (_inputState.PrimaryJustPressed && !_inputState.ShiftHeld)
        {
            var hoveredPin = circuit.GetPinAt(worldMousePos.X, worldMousePos.Y);
            if (hoveredPin != null)
            {
                _manualWireService.Start(hoveredPin, _gameRenderer.GridSize);
                return;
            }
        }

        // Handle double-click for component/pin title editing
        if (_inputState.PrimaryDoubleClick)
        {
            var component = circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);
            if (component != null)
            {
                _componentEditDialog.Show(component);
                _inputHandler.BeginTextInput();
                return;
            }
        }

        if (_inputState.PrimaryJustPressed)
        {
            var component = circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);
            if (component != null)
            {
                // Clicking on a component deselects wire and stops editing
                if (_selection.SelectedWire != null)
                {
                    _manualWireService.StopEditingWire();
                }
                _selection.HandleComponentClick(component, _inputState.CtrlHeld, worldMousePos);
            }
            else
            {
                var wire = circuit.GetWireAt(worldMousePos.X, worldMousePos.Y);
                if (wire != null)
                {
                    _selection.HandleWireClick(wire);
                    // Enter editing mode (auto-converts to manual wire if needed)
                    _manualWireService.StartEditingWire(wire, _gameRenderer.GridSize);
                    if (_manualWireService.IsEditingWire)
                    {
                        _statusService.Show(LocalizationManager.Get("status.wire_edit_mode"));
                    }
                    else
                    {
                        _statusService.Show(LocalizationManager.Get("status.wire_selected"));
                    }
                }
                else
                {
                    // Clicking on empty space deselects wire and stops editing
                    _manualWireService.StopEditingWire();
                    _selection.HandleEmptyClick(_inputState.CtrlHeld);
                }
            }
        }

        if (_inputState.PrimaryPressed)
        {
            _selection.UpdateDrag(worldMousePos, _gameRenderer.GridSize);
            // Update wire endpoints in real-time as components move
            if (_selection.IsDragging)
            {
                UpdateManualWireEndpointsForDraggedComponents();
            }
        }
        if (_inputState.PrimaryJustReleased)
        {
            _selection.EndDrag();
        }
    }

    private void UpdateManualWireEndpointsForDraggedComponents()
    {
        var draggedComponents = _selection.GetSelectedComponents();
        if (draggedComponents.Count == 0)
        {
            return;
        }

        var manualWires = _circuitManager.Circuit.GetManualWiresForComponents(draggedComponents);
        foreach (var wire in manualWires)
        {
            _manualWireService.UpdateWireEndpoints(wire);
        }
    }

    private void HandleWireNodeEditing(Point2 worldMousePos)
    {
        // Escape to exit editing mode
        if (_inputState.EscapeCommand)
        {
            _manualWireService.StopEditingWire();
            _selection.SelectedWire = null;
            return;
        }

        // Handle Shift+click for adding/removing nodes
        if (_inputState is { PrimaryJustPressed: true, ShiftHeld: true })
        {
            var nodeIndex = _manualWireService.GetNodeAtPosition(worldMousePos);
            if (nodeIndex >= 0)
            {
                // Shift+click on existing node - remove it
                if (_manualWireService.RemoveNode(nodeIndex))
                {
                    _statusService.Show(LocalizationManager.Get("status.node_removed"));
                }
            }
            else
            {
                // Shift+click on wire segment - add new node
                if (_manualWireService.AddNodeAtPosition(worldMousePos))
                {
                    _statusService.Show(LocalizationManager.Get("status.node_added"));
                }
            }
            return;
        }

        // Handle node dragging (without Shift)
        if (_inputState.PrimaryJustPressed)
        {
            var nodeIndex = _manualWireService.GetNodeAtPosition(worldMousePos);
            if (nodeIndex >= 0)
            {
                _manualWireService.StartDraggingNode(nodeIndex);
            }
            else
            {
                // Clicked outside nodes - exit editing mode and check what was clicked
                _manualWireService.StopEditingWire();
                _selection.SelectedWire = null;

                // Check if clicking on something else
                var component = _circuitManager.Circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);
                if (component != null)
                {
                    _selection.HandleComponentClick(component, _inputState.CtrlHeld, worldMousePos);
                }
                else
                {
                    var wire = _circuitManager.Circuit.GetWireAt(worldMousePos.X, worldMousePos.Y);
                    if (wire != null)
                    {
                        _selection.HandleWireClick(wire);
                        // Enter editing mode (auto-converts to manual wire if needed)
                        _manualWireService.StartEditingWire(wire, _gameRenderer.GridSize);
                        if (_manualWireService.IsEditingWire)
                        {
                            _statusService.Show(LocalizationManager.Get("status.wire_edit_mode"));
                        }
                    }
                }
            }
        }

        if (_inputState.PrimaryPressed && _manualWireService.DraggingNodeIndex >= 0)
        {
            _manualWireService.UpdateDraggingNode(worldMousePos);
        }

        if (_inputState.PrimaryJustReleased)
        {
            _manualWireService.StopDraggingNode();
        }
    }

    private void HandleManualWireMode(Point2 worldMousePos, Circuit circuit)
    {
        // Escape to cancel
        if (_inputState.EscapeCommand)
        {
            _manualWireService.Cancel();
            return;
        }

        // Right-click or Backspace to undo last point
        if (_inputState.SecondaryJustPressed || _inputState.BackspacePressed)
        {
            _manualWireService.RemoveLastPoint();
            return;
        }

        if (_inputState.PrimaryJustPressed)
        {
            // Check if clicking on a valid target pin
            var hoveredPin = circuit.GetPinAt(worldMousePos.X, worldMousePos.Y);
            if (hoveredPin != null && hoveredPin != _manualWireService.StartPin)
            {
                // Try to complete the connection
                _manualWireService.Complete(hoveredPin);
            }
            else
            {
                // Add point to path
                _manualWireService.AddPoint(worldMousePos);
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(CircuitRenderer.BackgroundColor);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: _camera.GetTransform());
        _gameRenderer.DrawWorld(_spriteBatch, _circuitManager.Circuit, _camera, _selection, _wireManager, _manualWireService, _wireManager.HoveredPin, _inputState.PointerPosition, ScreenWidth, ScreenHeight, _toolboxManager.MainToolbox.IsDraggingItem);
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        var uiFont = _fontService.GetFont();
        _gameRenderer.DrawUI(_spriteBatch, _toolboxManager, _mainMenu, _statusService, _dialogService, _truthTableService, _camera, _inputState.PointerPosition, ScreenWidth, ScreenHeight, uiFont);

        // Draw modal dialogs on top
        _profileDialog.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight);
        _levelSelectionPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight);
        _levelDescriptionPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight);
        _levelCompletedPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight);
        _componentEditDialog.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight);
        _controlsPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
