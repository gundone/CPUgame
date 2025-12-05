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

    private CameraController _camera = null!;
    private SelectionManager _selection = null!;
    private ComponentBuilder _componentBuilder = null!;
    private DialogService _dialogService = null!;
    private MainMenu _mainMenu = null!;

    private int ScreenWidth => GraphicsDevice?.Viewport.Width ?? _graphics.PreferredBackBufferWidth;
    private int ScreenHeight => GraphicsDevice?.Viewport.Height ?? _graphics.PreferredBackBufferHeight;

    public GameField(IPlatformServices platformServices, IInputHandler inputHandler,
        IStatusService statusService, ICircuitManager circuitManager, IWireManager wireManager,
        ICommandHandler commandHandler, IToolboxManager toolboxManager, IGameRenderer gameRenderer)
    {
        _platformServices = platformServices;
        _inputHandler = inputHandler;
        _statusService = statusService;
        _circuitManager = circuitManager;
        _wireManager = wireManager;
        _commandHandler = commandHandler;
        _toolboxManager = toolboxManager;
        _gameRenderer = gameRenderer;

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
    }

    protected override void Update(GameTime gameTime)
    {
        _inputState.Clear();
        _inputHandler.Update(_inputState, gameTime.ElapsedGameTime.TotalSeconds);
        var mousePos = _inputState.PointerPosition;
        var circuit = _circuitManager.Circuit;

        _statusService.Update(gameTime.ElapsedGameTime.TotalSeconds);

        if (_dialogService.IsActive)
        {
            _dialogService.HandleInput(_inputState, _inputHandler);
            base.Update(gameTime);
            return;
        }

        _mainMenu.Update(mousePos, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, ScreenWidth);
        if (_mainMenu.ContainsPoint(mousePos)) { base.Update(gameTime); return; }

        if (_inputState.CtrlHeld && _inputState.ScrollDelta != 0)
            _camera.HandleZoom(_inputState.ScrollDelta, mousePos, _camera.ScreenToWorld);

        var worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        bool clickedOnEmpty = !_toolboxManager.ContainsPoint(mousePos) &&
                              circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y) == null &&
                              circuit.GetPinAt(worldMousePos.X, worldMousePos.Y) == null;

        if ((_inputState.MiddleJustPressed || _inputState.SecondaryJustPressed) && !_toolboxManager.ContainsPoint(mousePos))
            _camera.StartPan(mousePos);

        if (_inputState.PrimaryJustPressed && clickedOnEmpty && !_toolboxManager.IsInteracting && !_camera.IsPanning)
            _selection.StartSelectionRect(worldMousePos, _inputState.CtrlHeld);

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
        _gameRenderer.DrawUI(_spriteBatch, _toolboxManager, _mainMenu, _statusService, _dialogService, _camera, _inputState.PointerPosition, ScreenWidth, ScreenHeight, _font);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
