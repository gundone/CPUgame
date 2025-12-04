using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CPUgame.Components;
using CPUgame.Core;
using CPUgame.Rendering;
using CPUgame.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CPUgame;

public class GameField : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;

    private Circuit _circuit = null!;
    private CircuitRenderer _renderer = null!;
    private Toolbox _toolbox = null!;
    private MainMenu _mainMenu = null!;

    // Custom components library
    private readonly Dictionary<string, CircuitData> _customComponents = new();
    private readonly string _componentsFolder;
    private Toolbox _userComponentsToolbox = null!;

    // Text input dialog state
    private bool _isNamingComponent;
    private string _componentNameInput = "";
    private List<Component>? _pendingComponentSelection;

    // Interaction state
    private MouseState _prevMouse;
    private KeyboardState _prevKeyboard;
    private Component? _draggingComponent;
    private Point _dragOffset;
    private bool _isDraggingMultiple;
    private Point _multiDragStart;
    private Dictionary<Component, Point>? _multiDragOffsets;
    private Pin? _wireStartPin;
    private Pin? _hoveredPin;
    private bool _isDraggingWire;
    private Pin? _selectedWire; // The input pin of the selected wire connection

    // Status message
    private string _statusMessage = "";
    private double _statusTime;

    // Global display settings
    private bool _showPinValues = false;

    // Zoom/Camera
    private float _zoom = 1.0f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 3.0f;
    private const float ZoomStep = 0.1f;
    private Vector2 _cameraOffset = Vector2.Zero;
    private bool _isPanning;
    private Point _panStartMouse;
    private Vector2 _panStartCamera;

    // Selection rectangle
    private bool _isSelecting;
    private Point _selectionStart;
    private Point _selectionEnd;

    // Screen dimensions
    private int ScreenWidth => GraphicsDevice?.Viewport.Width ?? _graphics.PreferredBackBufferWidth;
    private int ScreenHeight => GraphicsDevice?.Viewport.Height ?? _graphics.PreferredBackBufferHeight;

    public GameField()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        // Set up components folder
        _componentsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Components");
        if (!Directory.Exists(_componentsFolder))
            Directory.CreateDirectory(_componentsFolder);
    }

    protected override void Initialize()
    {
        // Load localization (loads saved language preference)
        LocalizationManager.Initialize();

        _circuit = new Circuit { Name = "My Circuit" };
        UpdateWindowTitle();

        // Handle window resize
        Window.ClientSizeChanged += OnWindowResize;

        // Load custom components
        LoadCustomComponents();

        // Set initial status message
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
        _userComponentsToolbox.OnDeleteComponent += DeleteUserComponent;

        _font = Content.Load<SpriteFont>("DefaultFont");
        _renderer.SetFont(_font);

        // Create main menu
        _mainMenu = new MainMenu();
        _mainMenu.OnNewCircuit += NewCircuit;
        _mainMenu.OnLoadCircuit += LoadCircuit;
        _mainMenu.OnSaveCircuit += SaveCircuit;
        _mainMenu.OnExit += () => Exit();
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
        _selectedWire = null;
        ShowStatus(LocalizationManager.Get("status.ready"));
    }

    private void LoadCustomComponents()
    {
        if (!Directory.Exists(_componentsFolder)) return;

        foreach (var file in Directory.GetFiles(_componentsFolder, "*.json"))
        {
            try
            {
                var data = CircuitSerializer.LoadCustomComponentData(file);
                if (data.IsCustomComponent)
                {
                    _customComponents[data.Name] = data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load component {file}: {ex.Message}");
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var mousePos = new Point(mouse.X, mouse.Y);
        var mouseJustPressed = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        var mouseJustReleased = mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed;
        var mousePressed = mouse.LeftButton == ButtonState.Pressed;

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
            HandleNamingDialogInput(keyboard);
            _prevMouse = mouse;
            _prevKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        // Update main menu
        _mainMenu.Update(mousePos, mouseJustPressed, mouseJustReleased, ScreenWidth);

        // If menu is open, don't process other interactions
        if (_mainMenu.ContainsPoint(mousePos))
        {
            _prevMouse = mouse;
            _prevKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        // Handle zoom with Ctrl+MouseScroll
        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (ctrl && scrollDelta != 0)
        {
            float oldZoom = _zoom;
            var worldMouseBefore = ScreenToWorld(mousePos);

            if (scrollDelta > 0)
                _zoom = Math.Min(_zoom + ZoomStep, MaxZoom);
            else
                _zoom = Math.Max(_zoom - ZoomStep, MinZoom);

            // Zoom towards mouse position
            if (Math.Abs(_zoom - oldZoom) > 0.001f)
            {
                var worldMouseAfter = ScreenToWorld(mousePos);
                _cameraOffset += worldMouseBefore - worldMouseAfter;
            }
        }

        // Handle panning with middle mouse button or right mouse button
        bool middlePressed = mouse.MiddleButton == ButtonState.Pressed;
        bool middleJustPressed = middlePressed && _prevMouse.MiddleButton == ButtonState.Released;
        bool rightPressed = mouse.RightButton == ButtonState.Pressed;
        bool rightJustPressed = rightPressed && _prevMouse.RightButton == ButtonState.Released;

        // Check if clicking on empty space for selection rectangle
        var worldMousePos = ScreenToWorldPoint(mousePos);
        bool clickedOnEmpty = !_toolbox.ContainsPoint(mousePos) &&
                              !_userComponentsToolbox.ContainsPoint(mousePos) &&
                              _circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y) == null &&
                              _circuit.GetPinAt(worldMousePos.X, worldMousePos.Y) == null;

        // Start panning with middle or right mouse button
        if ((middleJustPressed || rightJustPressed) && !_toolbox.ContainsPoint(mousePos) && !_userComponentsToolbox.ContainsPoint(mousePos))
        {
            _isPanning = true;
            _panStartMouse = mousePos;
            _panStartCamera = _cameraOffset;
        }

        // Start selection rectangle with left mouse button on empty space
        if (mouseJustPressed && clickedOnEmpty && !_toolbox.IsDraggingItem && !_userComponentsToolbox.IsDraggingItem && !_isPanning)
        {
            _isSelecting = true;
            _selectionStart = worldMousePos;
            _selectionEnd = worldMousePos;

            // Clear previous selection if not holding Ctrl
            ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            if (!ctrl)
            {
                _circuit.ClearSelection();
                _selectedWire = null;
            }
        }

        // Update selection rectangle
        if (_isSelecting)
        {
            _selectionEnd = worldMousePos;

            if (mouseJustReleased)
            {
                // Select all components within the rectangle
                SelectComponentsInRect(_selectionStart, _selectionEnd);
                _isSelecting = false;
            }
        }

        if (_isPanning)
        {
            if (middlePressed || rightPressed)
            {
                float deltaX = (mousePos.X - _panStartMouse.X) / _zoom;
                float deltaY = (mousePos.Y - _panStartMouse.Y) / _zoom;
                _cameraOffset = _panStartCamera - new Vector2(deltaX, deltaY);
            }
            else
            {
                _isPanning = false;
            }
        }

        // Handle keyboard shortcuts
        HandleKeyboardShortcuts(keyboard, gameTime.ElapsedGameTime.TotalSeconds);

        // Update toolboxes
        _toolbox.Update(mousePos, mousePressed, mouseJustPressed, mouseJustReleased);
        _userComponentsToolbox.Update(mousePos, mousePressed, mouseJustPressed, mouseJustReleased);

        // Convert mouse position to world coordinates for circuit interactions
        worldMousePos = ScreenToWorldPoint(mousePos);

        // Check if dropping a component from main toolbox
        if (mouseJustReleased && (_toolbox.DraggingTool != null || _toolbox.DraggingCustomComponent != null))
        {
            // Only place if dropping outside toolbox
            if (!_toolbox.ContainsPoint(mousePos))
            {
                PlaceComponentFromToolbox(worldMousePos);
            }
            _toolbox.ClearDragState();
        }

        // Check if dropping a component from user components toolbox
        if (mouseJustReleased && _userComponentsToolbox.DraggingCustomComponent != null)
        {
            // Only place if dropping outside toolbox
            if (!_userComponentsToolbox.ContainsPoint(mousePos))
            {
                PlaceComponentFromUserToolbox(worldMousePos);
            }
            _userComponentsToolbox.ClearDragState();
        }

        // Don't process circuit interaction if dragging from toolbox or interacting with toolbox window
        if (_toolbox.IsDraggingItem || _toolbox.IsDraggingWindow || _userComponentsToolbox.IsDraggingItem || _userComponentsToolbox.IsDraggingWindow)
        {
            // Cancel any wire operation
            if (_isDraggingWire)
            {
                _isDraggingWire = false;
                _wireStartPin = null;
            }
        }
        else if (!_toolbox.ContainsPoint(mousePos) && !_userComponentsToolbox.ContainsPoint(mousePos))
        {
            // Update hovered pin (use world coordinates)
            _hoveredPin = _circuit.GetPinAt(worldMousePos.X, worldMousePos.Y);

            // Update clocks
            foreach (var component in _circuit.Components.OfType<Clock>())
            {
                component.Update(gameTime.ElapsedGameTime.TotalSeconds);
            }

            // Handle circuit interactions (select mode by default) - use world coordinates
            HandleMouseInteraction(worldMousePos, mouseJustPressed, mouseJustReleased, mousePressed);

            // Simulate circuit
            _circuit.Simulate();
        }

        // Update cursor based on drag state
        if (_isPanning || _draggingComponent != null || _isDraggingMultiple)
        {
            Mouse.SetCursor(MouseCursor.SizeAll);
        }
        else if (_toolbox.IsDraggingItem || _toolbox.IsDraggingWindow || _userComponentsToolbox.IsDraggingItem || _userComponentsToolbox.IsDraggingWindow)
        {
            Mouse.SetCursor(MouseCursor.SizeAll);
        }
        else
        {
            Mouse.SetCursor(MouseCursor.Arrow);
        }

        _prevMouse = mouse;
        _prevKeyboard = keyboard;

        base.Update(gameTime);
    }

    private void HandleKeyboardShortcuts(KeyboardState keyboard, double deltaTime)
    {
        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);

        // Delete selected component
        if ((keyboard.IsKeyDown(Keys.Delete) || keyboard.IsKeyDown(Keys.Back)) &&
            !(_prevKeyboard.IsKeyDown(Keys.Delete) || _prevKeyboard.IsKeyDown(Keys.Back)))
        {
            DeleteSelectedComponents();
            return;
        }

        // Ctrl+S - Save circuit
        if (ctrl && keyboard.IsKeyDown(Keys.S) && !_prevKeyboard.IsKeyDown(Keys.S))
        {
            SaveCircuit();
            return;
        }

        // Ctrl+O - Load circuit
        if (ctrl && keyboard.IsKeyDown(Keys.O) && !_prevKeyboard.IsKeyDown(Keys.O))
        {
            LoadCircuit();
            return;
        }

        // Ctrl+B - Build selected into component
        if (ctrl && keyboard.IsKeyDown(Keys.B) && !_prevKeyboard.IsKeyDown(Keys.B))
        {
            BuildCustomComponent();
            return;
        }

        // Escape - Clear selection and cancel operations
        if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
        {
            _circuit.ClearSelection();
            _selectedWire = null;
            _wireStartPin = null;
            _isDraggingWire = false;
            _toolbox.ClearDragState();
            return;
        }

        // V - Toggle pin values display globally
        if (keyboard.IsKeyDown(Keys.V) && !_prevKeyboard.IsKeyDown(Keys.V))
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

        // Apply commands to selected components (includes arrow key movement)
        var selected = GetSelectedElements();
        foreach (var element in selected)
        {
            element.GridSize = _renderer.GridSize;

            // Handle BusInput resize specially - needs circuit context for wire preservation
            if (element is BusInput busInput)
            {
                HandleBusInputResize(busInput, keyboard);
            }

            element.ApplyCommand(keyboard, _prevKeyboard, deltaTime);
        }
    }



    private void DeleteSelectedComponents()
    {
        // Check if a wire is selected
        if (_selectedWire != null)
        {
            _selectedWire.Disconnect();
            _selectedWire = null;
            ShowStatus(LocalizationManager.Get("status.wire_disconnected"));
            return;
        }

        var toDelete = GetSelectedElements();
        foreach (var component in toDelete)
        {
            _circuit.RemoveComponent(component);
        }
        if (toDelete.Count > 0)
            ShowStatus(LocalizationManager.Get("status.deleted", toDelete.Count));
    }

    private void SaveCircuit()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "circuit.json");
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
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "circuit.json");
        if (!File.Exists(path))
        {
            ShowStatus(LocalizationManager.Get("status.no_saved_circuit"));
            return;
        }

        try
        {
            _circuit = CircuitSerializer.LoadCircuit(path, _customComponents);
            _selectedWire = null;
            ShowStatus(LocalizationManager.Get("status.loaded"));
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationManager.Get("status.load_failed", ex.Message));
        }
    }

    private void BuildCustomComponent()
    {
        var selected = GetSelectedElements();
        if (selected.Count == 0)
        {
            ShowStatus(LocalizationManager.Get("status.select_first"));
            return;
        }

        // Check for inputs and outputs (BusInput and BusOutput)
        int inputs = selected.Count(c => c is BusInput);
        int outputs = selected.Count(c => c is BusOutput);

        if (inputs == 0 || outputs == 0)
        {
            ShowStatus(LocalizationManager.Get("status.need_inputs_outputs"));
            return;
        }

        // Store selection and show naming dialog
        _pendingComponentSelection = selected;
        _componentNameInput = "";
        _isNamingComponent = true;
    }

    private void FinishBuildingComponent(string name)
    {
        if (_pendingComponentSelection == null || _pendingComponentSelection.Count == 0)
            return;

        var selected = _pendingComponentSelection;

        // Create a new circuit with only selected components
        var subCircuit = new Circuit { Name = name };
        var componentMap = new Dictionary<Component, Component>();

        // Clone components
        foreach (var comp in selected)
        {
            Component? clone = comp switch
            {
                NandGate => new NandGate(comp.X, comp.Y),
                InputSwitch sw => new InputSwitch(comp.X, comp.Y, sw.IsOn),
                OutputLed => new OutputLed(comp.X, comp.Y),
                Clock clk => new Clock(comp.X, comp.Y) { Frequency = clk.Frequency },
                BusInput busIn => new BusInput(comp.X, comp.Y, busIn.BitCount, _renderer.GridSize) { Value = busIn.Value },
                BusOutput busOut => new BusOutput(comp.X, comp.Y, busOut.BitCount, _renderer.GridSize),
                CustomComponent custom => CreateCustomComponentClone(custom, comp.X, comp.Y),
                _ => null
            };

            if (clone != null)
            {
                subCircuit.AddComponent(clone);
                componentMap[comp] = clone;
            }
        }

        // Clone internal connections
        foreach (var comp in selected)
        {
            if (!componentMap.TryGetValue(comp, out var cloneComp)) continue;

            for (int i = 0; i < comp.Inputs.Count; i++)
            {
                var input = comp.Inputs[i];
                if (input.ConnectedTo != null &&
                    componentMap.TryGetValue(input.ConnectedTo.Owner, out var fromClone))
                {
                    var fromPinIndex = input.ConnectedTo.Owner.Outputs.IndexOf(input.ConnectedTo);
                    if (fromPinIndex >= 0 && fromPinIndex < fromClone.Outputs.Count)
                    {
                        fromClone.Outputs[fromPinIndex].Connect(cloneComp.Inputs[i]);
                    }
                }
            }
        }

        // Save as custom component
        var filePath = Path.Combine(_componentsFolder, $"{name}.json");
        try
        {
            CircuitSerializer.SaveCustomComponent(subCircuit, name, filePath);
            var data = CircuitSerializer.LoadCustomComponentData(filePath);
            _customComponents[name] = data;
            _userComponentsToolbox.AddCustomComponent(name);
            ShowStatus(LocalizationManager.Get("status.component_created", name));
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationManager.Get("status.component_failed", ex.Message));
        }

        _pendingComponentSelection = null;
    }

    private CustomComponent? CreateCustomComponentClone(CustomComponent original, int x, int y)
    {
        if (_customComponents.TryGetValue(original.ComponentName, out var data))
        {
            var internalCircuit = CircuitSerializer.DeserializeCircuit(data, _customComponents);
            return new CustomComponent(x, y, original.ComponentName, internalCircuit);
        }
        return null;
    }

    private List<Component> GetSelectedElements()
    {
        return _circuit.Components.Where(c => c.IsSelected).ToList();
    }

    private void SelectComponentsInRect(Point start, Point end)
    {
        // Normalize rectangle
        int minX = Math.Min(start.X, end.X);
        int maxX = Math.Max(start.X, end.X);
        int minY = Math.Min(start.Y, end.Y);
        int maxY = Math.Max(start.Y, end.Y);

        foreach (var component in _circuit.Components)
        {
            // Check if component is within selection rectangle
            bool intersects = component.X < maxX && component.X + component.Width > minX &&
                              component.Y < maxY && component.Y + component.Height > minY;

            if (intersects)
            {
                component.IsSelected = true;
            }
        }
    }

    private void HandleBusInputResize(BusInput busInput, KeyboardState keyboard)
    {
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        if (!shift) return;

        bool increase = (IsKeyJustPressed(Keys.OemPlus, keyboard) || IsKeyJustPressed(Keys.Add, keyboard));
        bool decrease = (IsKeyJustPressed(Keys.OemMinus, keyboard) || IsKeyJustPressed(Keys.Subtract, keyboard));

        if (!increase && !decrease) return;

        // Collect all input pins from the circuit that might be connected to this BusInput
        var allInputPins = new List<Pin>();
        foreach (var comp in _circuit.Components)
        {
            allInputPins.AddRange(comp.Inputs);
        }

        busInput.ResizeBits(increase, allInputPins);
    }

    private bool IsKeyJustPressed(Keys key, KeyboardState current)
    {
        return current.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
    }

    private void HandleMouseInteraction(Point mousePos, bool mouseJustPressed, bool mouseJustReleased, bool mousePressed)
    {
        // Wire dragging from pins
        if (mouseJustPressed && _hoveredPin != null)
        {
            // Start wire drag
            _wireStartPin = _hoveredPin;
            _isDraggingWire = true;
            return;
        }

        if (_isDraggingWire && mouseJustReleased)
        {
            // Complete or cancel wire
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

        if (_isDraggingWire)
        {
            // Still dragging wire, don't do other interactions
            return;
        }

        // Component interaction (select, drag, toggle)
        if (mouseJustPressed)
        {
            var component = _circuit.GetComponentAt(mousePos.X, mousePos.Y);

            if (component != null)
            {
                // Clear wire selection when selecting component
                _selectedWire = null;

                // Handle selection
                var keyboard = Keyboard.GetState();
                bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);

                // Check if clicking on an already selected component (for multi-drag)
                if (component.IsSelected && GetSelectedElements().Count > 1)
                {
                    // Start multi-component drag
                    _isDraggingMultiple = true;
                    _multiDragStart = mousePos;
                    _multiDragOffsets = new Dictionary<Component, Point>();
                    foreach (var selected in GetSelectedElements())
                    {
                        _multiDragOffsets[selected] = new Point(selected.X, selected.Y);
                    }
                }
                else
                {
                    if (!ctrl)
                    {
                        _circuit.ClearSelection();
                    }

                    component.IsSelected = !component.IsSelected || !ctrl;

                    // Start single component dragging
                    _draggingComponent = component;
                    _dragOffset = new Point(mousePos.X - component.X, mousePos.Y - component.Y);
                }
            }
            else
            {
                // Check if clicking on a wire
                var wire = _circuit.GetWireAt(mousePos.X, mousePos.Y);
                if (wire != null)
                {
                    _circuit.ClearSelection();
                    _selectedWire = wire;
                    ShowStatus(LocalizationManager.Get("status.wire_selected"));
                }
                else
                {
                    _circuit.ClearSelection();
                    _selectedWire = null;
                }
            }
        }

        // Drag single component
        if (_draggingComponent != null && mousePressed)
        {
            var gridSize = _renderer.GridSize;
            _draggingComponent.X = ((mousePos.X - _dragOffset.X) / gridSize) * gridSize;
            _draggingComponent.Y = ((mousePos.Y - _dragOffset.Y) / gridSize) * gridSize;
        }

        // Drag multiple components
        if (_isDraggingMultiple && mousePressed && _multiDragOffsets != null)
        {
            var gridSize = _renderer.GridSize;
            int deltaX = mousePos.X - _multiDragStart.X;
            int deltaY = mousePos.Y - _multiDragStart.Y;

            foreach (var kvp in _multiDragOffsets)
            {
                var comp = kvp.Key;
                var originalPos = kvp.Value;
                comp.X = ((originalPos.X + deltaX) / gridSize) * gridSize;
                comp.Y = ((originalPos.Y + deltaY) / gridSize) * gridSize;
            }
        }

        if (mouseJustReleased)
        {
            _draggingComponent = null;
            _isDraggingMultiple = false;
            _multiDragOffsets = null;
        }
    }

    private void PlaceComponentFromToolbox(Point mousePos)
    {
        var gridSize = _renderer.GridSize;
        var x = (mousePos.X / gridSize) * gridSize;
        var y = (mousePos.Y / gridSize) * gridSize;

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
            // Apply global pin display setting to new Bus components
            if (newComponent is BusInput busInput)
                busInput.ShowPinValues = _showPinValues;
            else if (newComponent is BusOutput busOutput)
                busOutput.ShowPinValues = _showPinValues;

            _circuit.AddComponent(newComponent);
            ShowStatus(LocalizationManager.Get("status.placed", newComponent.Name));
        }
    }

    private void PlaceComponentFromUserToolbox(Point mousePos)
    {
        var gridSize = _renderer.GridSize;
        var x = (mousePos.X / gridSize) * gridSize;
        var y = (mousePos.Y / gridSize) * gridSize;

        if (_userComponentsToolbox.DraggingCustomComponent != null &&
            _customComponents.TryGetValue(_userComponentsToolbox.DraggingCustomComponent, out var data))
        {
            var internalCircuit = CircuitSerializer.DeserializeCircuit(data, _customComponents);
            var newComponent = new CustomComponent(x, y, _userComponentsToolbox.DraggingCustomComponent, internalCircuit);
            _circuit.AddComponent(newComponent);
            ShowStatus(LocalizationManager.Get("status.placed", newComponent.Name));
        }
    }

    private void HandleNamingDialogInput(KeyboardState keyboard)
    {
        // Handle Escape to cancel
        if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
        {
            _isNamingComponent = false;
            _pendingComponentSelection = null;
            _componentNameInput = "";
            ShowStatus(LocalizationManager.Get("status.cancelled"));
            return;
        }

        // Handle Enter to confirm
        if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
        {
            if (!string.IsNullOrWhiteSpace(_componentNameInput))
            {
                // Validate name doesn't already exist
                if (_customComponents.ContainsKey(_componentNameInput))
                {
                    ShowStatus(LocalizationManager.Get("status.name_exists"));
                    return;
                }

                _isNamingComponent = false;
                FinishBuildingComponent(_componentNameInput);
                _componentNameInput = "";
            }
            return;
        }

        // Handle Backspace
        if (keyboard.IsKeyDown(Keys.Back) && !_prevKeyboard.IsKeyDown(Keys.Back))
        {
            if (_componentNameInput.Length > 0)
            {
                _componentNameInput = _componentNameInput[..^1];
            }
            return;
        }

        // Handle character input
        foreach (Keys key in keyboard.GetPressedKeys())
        {
            if (_prevKeyboard.IsKeyDown(key)) continue;

            char? character = GetCharacterFromKey(key, keyboard);
            if (character.HasValue && _componentNameInput.Length < 20)
            {
                _componentNameInput += character.Value;
            }
        }
    }

    private char? GetCharacterFromKey(Keys key, KeyboardState keyboard)
    {
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (key >= Keys.A && key <= Keys.Z)
        {
            char c = (char)('a' + (key - Keys.A));
            return shift ? char.ToUpper(c) : c;
        }
        if (key >= Keys.D0 && key <= Keys.D9 && !shift)
        {
            return (char)('0' + (key - Keys.D0));
        }
        if (key == Keys.Space)
        {
            return ' ';
        }
        if (key == Keys.OemMinus || key == Keys.Subtract)
        {
            return shift ? '_' : '-';
        }

        return null;
    }

    private void DeleteUserComponent(string name)
    {
        // Remove from custom components dictionary
        _customComponents.Remove(name);

        // Remove from toolbox
        _userComponentsToolbox.RemoveCustomComponent(name);

        // Delete the file
        var filePath = Path.Combine(_componentsFolder, $"{name}.json");
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                ShowStatus(LocalizationManager.Get("status.component_deleted", name));
            }
            catch (Exception ex)
            {
                ShowStatus(LocalizationManager.Get("status.delete_failed", ex.Message));
            }
        }
    }

    private void ShowStatus(string message)
    {
        _statusMessage = message;
        _statusTime = 3.0;
    }

    private Matrix GetCameraTransform()
    {
        return Matrix.CreateTranslation(-_cameraOffset.X, -_cameraOffset.Y, 0) *
               Matrix.CreateScale(_zoom, _zoom, 1);
    }

    private Vector2 ScreenToWorld(Point screenPos)
    {
        return new Vector2(
            screenPos.X / _zoom + _cameraOffset.X,
            screenPos.Y / _zoom + _cameraOffset.Y);
    }

    private Point ScreenToWorldPoint(Point screenPos)
    {
        var world = ScreenToWorld(screenPos);
        return new Point((int)world.X, (int)world.Y);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(CircuitRenderer.BackgroundColor);

        // Draw world-space content with camera transform
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: GetCameraTransform());

        // Draw infinite grid
        _renderer.DrawGrid(_spriteBatch, _cameraOffset.X, _cameraOffset.Y, ScreenWidth, ScreenHeight, _zoom);

        // Draw circuit
        _renderer.DrawCircuit(_spriteBatch, _circuit, _selectedWire);

        // Draw hovered pin highlight
        if (_hoveredPin != null && !_toolbox.IsDraggingItem)
        {
            _renderer.DrawPinHighlight(_spriteBatch, _hoveredPin);
        }

        // Draw wire preview while dragging
        if (_isDraggingWire && _wireStartPin != null)
        {
            var worldMousePos = ScreenToWorld(Mouse.GetState().Position);
            _renderer.DrawWirePreview(_spriteBatch,
                new Vector2(_wireStartPin.WorldX, _wireStartPin.WorldY),
                worldMousePos);
        }

        // Draw selection rectangle
        if (_isSelecting)
        {
            DrawSelectionRectangle();
        }

        _spriteBatch.End();

        // Draw UI elements without transform (screen space)
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw zoom indicator (below menu)
        DrawZoomIndicator();

        // Draw toolbox
        _toolbox.Draw(_spriteBatch, _renderer.Pixel, _font, Mouse.GetState().Position);

        // Draw user components toolbox
        _userComponentsToolbox.Draw(_spriteBatch, _renderer.Pixel, _font, Mouse.GetState().Position);

        // Draw status bar
        DrawStatusBar();

        // Draw help text
        DrawHelpText();

        // Draw main menu (always on top)
        _mainMenu.Draw(_spriteBatch, _renderer.Pixel, _font, ScreenWidth, Mouse.GetState().Position);

        // Draw naming dialog if active
        if (_isNamingComponent)
        {
            DrawNamingDialog();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawStatusBar()
    {
        var barHeight = 24;
        var barY = ScreenHeight - barHeight;
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(0, barY, ScreenWidth, barHeight), new Color(40, 40, 50));
        _spriteBatch.DrawString(_font, _statusMessage, new Vector2(8, barY + 4), new Color(200, 200, 210));
    }

    private void DrawHelpText()
    {
        var helpText = LocalizationManager.Get("help.shortcuts");
        var helpSize = _font.MeasureString(helpText);
        _spriteBatch.DrawString(_font, helpText,
            new Vector2(ScreenWidth - helpSize.X - 8, ScreenHeight - 24 + 4),
            new Color(120, 120, 140));
    }

    private void DrawZoomIndicator()
    {
        var zoomText = $"Zoom: {_zoom:P0}";
        _spriteBatch.DrawString(_font, zoomText, new Vector2(8, _mainMenu.Height + 8), new Color(150, 150, 170));
    }

    private void DrawSelectionRectangle()
    {
        int minX = Math.Min(_selectionStart.X, _selectionEnd.X);
        int maxX = Math.Max(_selectionStart.X, _selectionEnd.X);
        int minY = Math.Min(_selectionStart.Y, _selectionEnd.Y);
        int maxY = Math.Max(_selectionStart.Y, _selectionEnd.Y);

        var rect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        var fillColor = new Color(70, 130, 180, 50);
        var borderColor = new Color(70, 130, 180, 200);

        // Fill
        _spriteBatch.Draw(_renderer.Pixel, rect, fillColor);

        // Border
        int thickness = Math.Max(1, (int)(1 / _zoom));
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), borderColor);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), borderColor);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), borderColor);
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), borderColor);
    }

    private void DrawNamingDialog()
    {
        // Semi-transparent overlay
        _spriteBatch.Draw(_renderer.Pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 150));

        // Calculate dialog size based on text content
        var titleText = LocalizationManager.Get("dialog.name_component");
        var hintText = LocalizationManager.Get("dialog.name_hint");
        var titleSize = _font.MeasureString(titleText);
        var hintSize = _font.MeasureString(hintText);

        // Dialog width based on longest text + padding
        int minWidth = 300;
        int contentWidth = Math.Max((int)titleSize.X, (int)hintSize.X) + 60;
        int dialogWidth = Math.Max(minWidth, contentWidth);
        int dialogHeight = 120;
        int dialogX = (ScreenWidth - dialogWidth) / 2;
        int dialogY = (ScreenHeight - dialogHeight) / 2;
        var dialogRect = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);

        // Background
        _spriteBatch.Draw(_renderer.Pixel, dialogRect, new Color(45, 45, 55));

        // Border
        DrawDialogBorder(dialogRect, new Color(80, 80, 100), 2);

        // Title
        _spriteBatch.DrawString(_font, titleText,
            new Vector2(dialogX + (dialogWidth - titleSize.X) / 2, dialogY + 10),
            new Color(220, 220, 230));

        // Input field background
        var inputRect = new Rectangle(dialogX + 20, dialogY + 40, dialogWidth - 40, 30);
        _spriteBatch.Draw(_renderer.Pixel, inputRect, new Color(30, 30, 40));
        DrawDialogBorder(inputRect, new Color(100, 100, 120), 1);

        // Input text with cursor
        var displayText = _componentNameInput + "_";
        _spriteBatch.DrawString(_font, displayText,
            new Vector2(inputRect.X + 5, inputRect.Y + 5),
            new Color(220, 220, 230));

        // Hint text
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
