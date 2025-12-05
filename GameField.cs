using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Components;
using CPUgame.Core;
using CPUgame.Input;
using CPUgame.Platform;
using CPUgame.Rendering;
using CPUgame.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame;

public class GameField : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;

    // Platform abstractions
    private readonly IPlatformServices _platformServices;
    private readonly IInputHandler _inputHandler;
    private readonly InputState _inputState = new();

    // Core systems
    private Circuit _circuit = null!;
    private CircuitRenderer _renderer = null!;
    private CameraController _camera = null!;
    private SelectionManager _selection = null!;
    private ComponentBuilder _componentBuilder = null!;

    // UI
    private Toolbox _toolbox = null!;
    private Toolbox _userComponentsToolbox = null!;
    private MainMenu _mainMenu = null!;

    // Custom components library
    private readonly Dictionary<string, CircuitData> _customComponents = new();

    // Wire interaction state
    private Pin? _wireStartPin;
    private Pin? _hoveredPin;
    private bool _isDraggingWire;

    // Text input dialog state
    private bool _isNamingComponent;
    private string _componentNameInput = "";
    private List<Component>? _pendingComponentSelection;

    // Status message
    private string _statusMessage = "";
    private double _statusTime;

    // Global display settings
    private bool _showPinValues;

    // Screen dimensions
    private int ScreenWidth => GraphicsDevice?.Viewport.Width ?? _graphics.PreferredBackBufferWidth;
    private int ScreenHeight => GraphicsDevice?.Viewport.Height ?? _graphics.PreferredBackBufferHeight;

    public GameField() : this(new DesktopPlatformServices(), new DesktopInputHandler())
    {
    }

    public GameField(IPlatformServices platformServices, IInputHandler inputHandler)
    {
        _platformServices = platformServices;
        _inputHandler = inputHandler;

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        // Ensure components folder exists
        _platformServices.EnsureDirectoryExists(_platformServices.GetComponentsFolder());
    }

    protected override void Initialize()
    {
        LocalizationManager.Initialize();

        _circuit = new Circuit { Name = "My Circuit" };
        _camera = new CameraController();
        _selection = new SelectionManager(_circuit);
        _componentBuilder = new ComponentBuilder(_customComponents, _platformServices);

        // Wire up component builder events
        _componentBuilder.OnComponentCreated += name =>
        {
            _userComponentsToolbox.AddCustomComponent(name);
            ShowStatus(LocalizationManager.Get("status.component_created", name));
        };
        _componentBuilder.OnComponentDeleted += name =>
        {
            _userComponentsToolbox.RemoveCustomComponent(name);
            ShowStatus(LocalizationManager.Get("status.component_deleted", name));
        };
        _componentBuilder.OnError += msg => ShowStatus(LocalizationManager.Get("status.component_failed", msg));

        UpdateWindowTitle();
        Window.ClientSizeChanged += OnWindowResize;

        // Load custom components
        _componentBuilder.LoadCustomComponents();

        _statusMessage = LocalizationManager.Get("help.drag");

        base.Initialize();
    }

    private void UpdateWindowTitle()
    {
        Window.Title = LocalizationManager.Get("app.title");
    }

    private void OnWindowResize(object? sender, EventArgs e)
    {
        if (Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderer = new CircuitRenderer(GraphicsDevice);
        _toolbox = new Toolbox(ScreenWidth - 200, 60);
        _userComponentsToolbox = new Toolbox(ScreenWidth - 200, 300, isUserComponents: true);

        // Add loaded custom components to user components toolbox
        foreach (var name in _customComponents.Keys)
        {
            _userComponentsToolbox.AddCustomComponent(name);
        }

        // Wire up delete event
        _userComponentsToolbox.OnDeleteComponent += name => _componentBuilder.DeleteComponent(name);

        _font = Content.Load<SpriteFont>("DefaultFont");
        _renderer.SetFont(_font);

        // Create main menu
        _mainMenu = new MainMenu();
        _mainMenu.OnNewCircuit += NewCircuit;
        _mainMenu.OnLoadCircuit += LoadCircuit;
        _mainMenu.OnSaveCircuit += SaveCircuit;
        _mainMenu.OnExit += Exit;
        _mainMenu.OnLanguageChanged += ChangeLanguage;
    }

    private void ChangeLanguage(string langCode)
    {
        LocalizationManager.LoadLanguage(langCode);
        UpdateWindowTitle();
        _statusMessage = LocalizationManager.Get("status.ready");
    }

    private void NewCircuit()
    {
        _circuit = new Circuit { Name = "New Circuit" };
        _selection = new SelectionManager(_circuit);
        ShowStatus(LocalizationManager.Get("status.ready"));
    }

    protected override void Update(GameTime gameTime)
    {
        // Update input state
        _inputState.Clear();
        _inputHandler.Update(_inputState);

        var mousePos = _inputState.PointerPosition;

        // Update status message timer
        if (_statusTime > 0)
        {
            _statusTime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_statusTime <= 0)
                _statusMessage = LocalizationManager.Get("status.ready");
        }

        // Handle naming dialog
        if (_isNamingComponent)
        {
            HandleNamingDialogInput();
            base.Update(gameTime);
            return;
        }

        // Update main menu
        _mainMenu.Update(mousePos, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, ScreenWidth);

        if (_mainMenu.ContainsPoint(mousePos))
        {
            base.Update(gameTime);
            return;
        }

        // Handle zoom
        if (_inputState.CtrlHeld && _inputState.ScrollDelta != 0)
        {
            _camera.HandleZoom(_inputState.ScrollDelta, mousePos, _camera.ScreenToWorld);
        }

        // Handle panning
        var worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        bool clickedOnEmpty = !_toolbox.ContainsPoint(mousePos) &&
                              !_userComponentsToolbox.ContainsPoint(mousePos) &&
                              _circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y) == null &&
                              _circuit.GetPinAt(worldMousePos.X, worldMousePos.Y) == null;

        if ((_inputState.MiddleJustPressed || _inputState.SecondaryJustPressed) &&
            !_toolbox.ContainsPoint(mousePos) && !_userComponentsToolbox.ContainsPoint(mousePos))
        {
            _camera.StartPan(mousePos);
        }

        // Start selection rectangle
        if (_inputState.PrimaryJustPressed && clickedOnEmpty &&
            !_toolbox.IsDraggingItem && !_userComponentsToolbox.IsDraggingItem && !_camera.IsPanning)
        {
            _selection.StartSelectionRect(worldMousePos, _inputState.CtrlHeld);
        }

        // Update selection rectangle
        if (_selection.IsSelecting)
        {
            _selection.UpdateSelectionRect(worldMousePos);
            if (_inputState.PrimaryJustReleased)
            {
                _selection.CompleteSelectionRect();
            }
        }

        // Update panning
        if (_camera.IsPanning)
        {
            if (_inputState.MiddlePressed || _inputState.SecondaryPressed)
            {
                _camera.UpdatePan(mousePos);
            }
            else
            {
                _camera.EndPan();
            }
        }

        // Handle keyboard shortcuts
        HandleCommands(gameTime.ElapsedGameTime.TotalSeconds);

        // Update toolboxes
        _toolbox.Update(mousePos, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased);
        _userComponentsToolbox.Update(mousePos, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased);

        worldMousePos = _camera.ScreenToWorldPoint(mousePos);

        // Handle toolbox drops
        HandleToolboxDrops(mousePos, worldMousePos);

        // Handle circuit interaction
        if (_toolbox.IsDraggingItem || _toolbox.IsDraggingWindow ||
            _userComponentsToolbox.IsDraggingItem || _userComponentsToolbox.IsDraggingWindow)
        {
            // Cancel wire operation when interacting with toolbox
            if (_isDraggingWire)
            {
                _wireStartPin = null;
                _isDraggingWire = false;
            }
        }
        else if (!_toolbox.ContainsPoint(mousePos) && !_userComponentsToolbox.ContainsPoint(mousePos))
        {
            _hoveredPin = _circuit.GetPinAt(worldMousePos.X, worldMousePos.Y);

            foreach (var component in _circuit.Components.OfType<Clock>())
            {
                component.Update(gameTime.ElapsedGameTime.TotalSeconds);
            }

            HandleCircuitInteraction(worldMousePos);
            _circuit.Simulate();
        }

        // Update cursor
        UpdateCursor();

        base.Update(gameTime);
    }

    private void HandleCommands(double deltaTime)
    {
        if (_inputState.DeleteCommand)
        {
            DeleteSelected();
            return;
        }

        if (_inputState.SaveCommand)
        {
            SaveCircuit();
            return;
        }

        if (_inputState.LoadCommand)
        {
            LoadCircuit();
            return;
        }

        if (_inputState.BuildCommand)
        {
            BuildCustomComponent();
            return;
        }

        if (_inputState.EscapeCommand)
        {
            _selection.ClearAll();
            _wireStartPin = null;
            _isDraggingWire = false;
            _toolbox.ClearDragState();
            return;
        }

        if (_inputState.TogglePinValuesCommand)
        {
            _showPinValues = !_showPinValues;
            foreach (var comp in _circuit.Components)
            {
                if (comp is BusInput busInput)
                    busInput.ShowPinValues = _showPinValues;
                else if (comp is BusOutput busOutput)
                    busOutput.ShowPinValues = _showPinValues;
            }
            return;
        }

        // Apply commands to selected components
        ApplyComponentCommands(deltaTime);
    }

    private void ApplyComponentCommands(double deltaTime)
    {
        var selected = _selection.GetSelectedComponents();
        foreach (var element in selected)
        {
            element.GridSize = _renderer.GridSize;

            if (element is BusInput busInput)
            {
                if (_inputState.ShiftHeld && (_inputState.IncreaseCommand || _inputState.DecreaseCommand))
                {
                    // Shift+/- resizes the bus
                    var allInputPins = _circuit.Components.SelectMany(c => c.Inputs).ToList();
                    busInput.ResizeBits(_inputState.IncreaseCommand, allInputPins);
                }
                else if (_inputState.IncreaseCommand)
                {
                    // + increments value
                    busInput.Value = (busInput.Value + 1) % (1 << busInput.BitCount);
                }
                else if (_inputState.DecreaseCommand)
                {
                    // - decrements value
                    busInput.Value = (busInput.Value - 1 + (1 << busInput.BitCount)) % (1 << busInput.BitCount);
                }

                if (_inputState.NumberInput.HasValue)
                {
                    busInput.ToggleBit(_inputState.NumberInput.Value - '0');
                }
            }

            // Apply movement and toggle commands via InputState
            if (_inputState.MoveUp) element.Y -= element.GridSize;
            if (_inputState.MoveDown) element.Y += element.GridSize;
            if (_inputState.MoveLeft) element.X -= element.GridSize;
            if (_inputState.MoveRight) element.X += element.GridSize;

            if (_inputState.ToggleCommand && element is InputSwitch sw)
                sw.IsOn = !sw.IsOn;
        }
    }

    private void HandleToolboxDrops(Point mousePos, Point worldMousePos)
    {
        if (_inputState.PrimaryJustReleased && (_toolbox.DraggingTool != null || _toolbox.DraggingCustomComponent != null))
        {
            if (!_toolbox.ContainsPoint(mousePos))
            {
                PlaceComponentFromToolbox(worldMousePos);
            }
            _toolbox.ClearDragState();
        }

        if (_inputState.PrimaryJustReleased && _userComponentsToolbox.DraggingCustomComponent != null)
        {
            if (!_userComponentsToolbox.ContainsPoint(mousePos))
            {
                PlaceComponentFromUserToolbox(worldMousePos);
            }
            _userComponentsToolbox.ClearDragState();
        }
    }

    private void HandleCircuitInteraction(Point worldMousePos)
    {
        // Wire dragging
        if (_inputState.PrimaryJustPressed && _hoveredPin != null)
        {
            _wireStartPin = _hoveredPin;
            _isDraggingWire = true;
            return;
        }

        if (_isDraggingWire && _inputState.PrimaryJustReleased)
        {
            if (_hoveredPin != null && _wireStartPin != null &&
                _hoveredPin != _wireStartPin &&
                _hoveredPin.Owner != _wireStartPin.Owner &&
                _hoveredPin.Type != _wireStartPin.Type)
            {
                _wireStartPin.Connect(_hoveredPin);
                ShowStatus(LocalizationManager.Get("status.wire_connected"));
            }
            _wireStartPin = null;
            _isDraggingWire = false;
            return;
        }

        if (_isDraggingWire) return;

        // Component interaction
        if (_inputState.PrimaryJustPressed)
        {
            var component = _circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);

            if (component != null)
            {
                _selection.HandleComponentClick(component, _inputState.CtrlHeld, worldMousePos);
            }
            else
            {
                var wire = _circuit.GetWireAt(worldMousePos.X, worldMousePos.Y);
                if (wire != null)
                {
                    _selection.HandleWireClick(wire);
                    ShowStatus(LocalizationManager.Get("status.wire_selected"));
                }
                else
                {
                    _selection.HandleEmptyClick(_inputState.CtrlHeld);
                }
            }
        }

        // Update dragging
        if (_inputState.PrimaryPressed)
        {
            _selection.UpdateDrag(worldMousePos, _renderer.GridSize);
        }

        if (_inputState.PrimaryJustReleased)
        {
            _selection.EndDrag();
        }
    }

    private void UpdateCursor()
    {
        if (_camera.IsPanning || _selection.IsDragging)
        {
            _inputHandler.SetCursor(CursorType.Move);
        }
        else if (_toolbox.IsDraggingItem || _toolbox.IsDraggingWindow ||
                 _userComponentsToolbox.IsDraggingItem || _userComponentsToolbox.IsDraggingWindow)
        {
            _inputHandler.SetCursor(CursorType.Move);
        }
        else
        {
            _inputHandler.SetCursor(CursorType.Arrow);
        }
    }

    private void DeleteSelected()
    {
        if (_selection.DeleteSelectedWire())
        {
            ShowStatus(LocalizationManager.Get("status.wire_disconnected"));
            return;
        }

        int count = _selection.DeleteSelectedComponents();
        if (count > 0)
            ShowStatus(LocalizationManager.Get("status.deleted", count));
    }

    private void SaveCircuit()
    {
        var path = _platformServices.GetDefaultCircuitPath();
        try
        {
            CircuitSerializer.SaveCircuit(_circuit, path);
            ShowStatus(LocalizationManager.Get("status.saved", path));
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationManager.Get("status.save_failed", ex.Message));
        }
    }

    private void LoadCircuit()
    {
        var path = _platformServices.GetDefaultCircuitPath();
        if (!_platformServices.FileExists(path))
        {
            ShowStatus(LocalizationManager.Get("status.no_saved_circuit"));
            return;
        }

        try
        {
            _circuit = CircuitSerializer.LoadCircuit(path, _customComponents);
            _selection = new SelectionManager(_circuit);
            ShowStatus(LocalizationManager.Get("status.loaded"));
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationManager.Get("status.load_failed", ex.Message));
        }
    }

    private void BuildCustomComponent()
    {
        var selected = _selection.GetSelectedComponents();

        if (!_componentBuilder.ValidateSelection(selected, out var error))
        {
            ShowStatus(LocalizationManager.Get(error!));
            return;
        }

        _pendingComponentSelection = selected;
        _componentNameInput = "";
        _isNamingComponent = true;
        _inputHandler.BeginTextInput();
    }

    private void FinishBuildingComponent(string name)
    {
        if (_pendingComponentSelection == null) return;

        _componentBuilder.BuildComponent(name, _pendingComponentSelection, _renderer.GridSize);
        _pendingComponentSelection = null;
    }

    private void PlaceComponentFromToolbox(Point worldMousePos)
    {
        var gridSize = _renderer.GridSize;
        var x = (worldMousePos.X / gridSize) * gridSize;
        var y = (worldMousePos.Y / gridSize) * gridSize;

        Component? newComponent = null;

        if (_toolbox.DraggingTool != null)
        {
            newComponent = _toolbox.DraggingTool switch
            {
                ToolType.PlaceNand => new NandGate(x, y),
                ToolType.PlaceSwitch => new InputSwitch(x, y),
                ToolType.PlaceLed => new OutputLed(x, y),
                ToolType.PlaceClock => new Clock(x, y),
                ToolType.PlaceBusInput => new BusInput(x, y, _toolbox.BusInputBits, gridSize),
                ToolType.PlaceBusOutput => new BusOutput(x, y, _toolbox.BusOutputBits, gridSize),
                _ => null
            };
        }

        if (newComponent != null)
        {
            if (newComponent is BusInput busInput)
                busInput.ShowPinValues = _showPinValues;
            else if (newComponent is BusOutput busOutput)
                busOutput.ShowPinValues = _showPinValues;

            _circuit.AddComponent(newComponent);
            ShowStatus(LocalizationManager.Get("status.placed", newComponent.Name));
        }
    }

    private void PlaceComponentFromUserToolbox(Point worldMousePos)
    {
        var gridSize = _renderer.GridSize;
        var x = (worldMousePos.X / gridSize) * gridSize;
        var y = (worldMousePos.Y / gridSize) * gridSize;

        var componentName = _userComponentsToolbox.DraggingCustomComponent;
        if (componentName != null)
        {
            var newComponent = _componentBuilder.CreateInstance(componentName, x, y);
            if (newComponent != null)
            {
                _circuit.AddComponent(newComponent);
                ShowStatus(LocalizationManager.Get("status.placed", newComponent.Name));
            }
        }
    }

    private void HandleNamingDialogInput()
    {
        if (_inputState.EscapeCommand)
        {
            _isNamingComponent = false;
            _pendingComponentSelection = null;
            _componentNameInput = "";
            _inputHandler.EndTextInput();
            ShowStatus(LocalizationManager.Get("status.cancelled"));
            return;
        }

        if (_inputState.EnterPressed)
        {
            if (!string.IsNullOrWhiteSpace(_componentNameInput))
            {
                if (!_componentBuilder.ValidateName(_componentNameInput, out var error))
                {
                    ShowStatus(LocalizationManager.Get(error!));
                    return;
                }

                _isNamingComponent = false;
                _inputHandler.EndTextInput();
                FinishBuildingComponent(_componentNameInput);
                _componentNameInput = "";
            }
            return;
        }

        if (_inputState.BackspacePressed && _componentNameInput.Length > 0)
        {
            _componentNameInput = _componentNameInput[..^1];
            return;
        }

        if (_inputState.CharacterInput.HasValue && _componentNameInput.Length < 20)
        {
            _componentNameInput += _inputState.CharacterInput.Value;
        }
    }

    private void ShowStatus(string message)
    {
        _statusMessage = message;
        _statusTime = 3.0;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(CircuitRenderer.BackgroundColor);

        // Draw world-space content
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetTransform());

        _renderer.DrawGrid(_spriteBatch, _camera.Offset.X, _camera.Offset.Y, ScreenWidth, ScreenHeight, _camera.Zoom);
        _renderer.DrawCircuit(_spriteBatch, _circuit, _selection.SelectedWire);

        if (_hoveredPin != null && !_toolbox.IsDraggingItem)
        {
            _renderer.DrawPinHighlight(_spriteBatch, _hoveredPin);
        }

        if (_isDraggingWire && _wireStartPin != null)
        {
            var worldMousePos = _camera.ScreenToWorld(_inputState.PointerPosition);
            _renderer.DrawWirePreview(_spriteBatch,
                new Vector2(_wireStartPin.WorldX, _wireStartPin.WorldY),
                worldMousePos);
        }

        if (_selection.IsSelecting)
        {
            DrawSelectionRectangle();
        }

        _spriteBatch.End();

        // Draw UI
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawZoomIndicator();
        _toolbox.Draw(_spriteBatch, _renderer.Pixel, _font, _inputState.PointerPosition);
        _userComponentsToolbox.Draw(_spriteBatch, _renderer.Pixel, _font, _inputState.PointerPosition);
        DrawStatusBar();
        _mainMenu.Draw(_spriteBatch, _renderer.Pixel, _font, ScreenWidth, _inputState.PointerPosition);

        if (_isNamingComponent)
        {
            DrawNamingDialog();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawStatusBar()
    {
        var barHeight = 44;
        var barY = ScreenHeight - barHeight;
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(0, barY, ScreenWidth, barHeight), new Color(40, 40, 50));

        var helpText = LocalizationManager.Get("help.shortcuts");
        _spriteBatch.DrawString(_font, helpText, new Vector2(8, barY + 4), new Color(120, 120, 140));
        _spriteBatch.DrawString(_font, _statusMessage, new Vector2(8, barY + 24), new Color(200, 200, 210));
    }

    private void DrawZoomIndicator()
    {
        var zoomText = $"Zoom: {_camera.Zoom:P0}";
        _spriteBatch.DrawString(_font, zoomText, new Vector2(8, _mainMenu.Height + 8), new Color(150, 150, 170));
    }

    private void DrawSelectionRectangle()
    {
        int minX = Math.Min(_selection.SelectionStart.X, _selection.SelectionEnd.X);
        int maxX = Math.Max(_selection.SelectionStart.X, _selection.SelectionEnd.X);
        int minY = Math.Min(_selection.SelectionStart.Y, _selection.SelectionEnd.Y);
        int maxY = Math.Max(_selection.SelectionStart.Y, _selection.SelectionEnd.Y);

        var rect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        var fillColor = new Color(70, 130, 180, 50);
        var borderColor = new Color(70, 130, 180, 200);

        _spriteBatch.Draw(_renderer.Pixel, rect, fillColor);

        int thickness = Math.Max(1, (int)(1 / _camera.Zoom));
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), borderColor);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), borderColor);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), borderColor);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), borderColor);
    }

    private void DrawNamingDialog()
    {
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 150));

        var titleText = LocalizationManager.Get("dialog.name_component");
        var hintText = LocalizationManager.Get("dialog.name_hint");
        var titleSize = _font.MeasureString(titleText);
        var hintSize = _font.MeasureString(hintText);

        int minWidth = 300;
        int contentWidth = Math.Max((int)titleSize.X, (int)hintSize.X) + 60;
        int dialogWidth = Math.Max(minWidth, contentWidth);
        int dialogHeight = 120;
        int dialogX = (ScreenWidth - dialogWidth) / 2;
        int dialogY = (ScreenHeight - dialogHeight) / 2;
        var dialogRect = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);

        _spriteBatch.Draw(_renderer.Pixel, dialogRect, new Color(45, 45, 55));
        DrawDialogBorder(dialogRect, new Color(80, 80, 100), 2);

        _spriteBatch.DrawString(_font, titleText,
            new Vector2(dialogX + (dialogWidth - titleSize.X) / 2, dialogY + 10),
            new Color(220, 220, 230));

        var inputRect = new Rectangle(dialogX + 20, dialogY + 40, dialogWidth - 40, 30);
        _spriteBatch.Draw(_renderer.Pixel, inputRect, new Color(30, 30, 40));
        DrawDialogBorder(inputRect, new Color(100, 100, 120), 1);

        var displayText = _componentNameInput + "_";
        _spriteBatch.DrawString(_font, displayText,
            new Vector2(inputRect.X + 5, inputRect.Y + 5),
            new Color(220, 220, 230));

        _spriteBatch.DrawString(_font, hintText,
            new Vector2(dialogX + (dialogWidth - hintSize.X) / 2, dialogY + dialogHeight - 30),
            new Color(150, 150, 170));
    }

    private void DrawDialogBorder(Rectangle rect, Color color, int thickness)
    {
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
