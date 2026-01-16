using System;
using System.Linq;
using CPUgame.Core;
using CPUgame.Core.Components;
using CPUgame.Core.Designer;
using CPUgame.Core.Input;
using CPUgame.Core.Primitives;
using CPUgame.Converters;
using CPUgame.Core.Circuit;
using CPUgame.Core.Levels;
using CPUgame.Core.Localization;
using CPUgame.Core.Selection;
using CPUgame.Core.Serialization;
using CPUgame.Core.Services;
using CPUgame.Rendering;
using CPUgame.UI;
using CPUgame.UI.Designer;
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
    private readonly IAppearanceService _appearanceService;
    private readonly IPreferencesService _preferencesService;

    private ISelectionManager _selection = null!;
    private DesignerMode _designerMode = null!;
    private MainMenu _mainMenu = null!;
    private ProfileDialog _profileDialog = null!;
    private LevelSelectionPopup _levelSelectionPopup = null!;
    private LevelDescriptionPopup _levelDescriptionPopup = null!;
    private LevelCompletedPopup _levelCompletedPopup = null!;
    private ComponentEditDialog _componentEditDialog = null!;
    private ControlsPopup _controlsPopup = null!;
    private LevelInfoWindow _levelInfoWindow = null!;
    private MainGameMenu _mainGameMenu = null!;
    private OptionsDialog _optionsDialog = null!;
    private ConfirmationDialog _confirmationDialog = null!;
    private ComponentContextMenu _componentContextMenu = null!;
    private TabBar _tabBar = null!;

    // Component editing state
    private CustomComponent? _componentBeingEdited;
    private string? _editingComponentName;
    private int _tabCloseRequestIndex = -1;

    // Top menu visibility state (hidden by default, toggle with Ctrl+Shift+M)
    private bool _isMenuVisible;
    private const int _menuHeight = 24;
    private const int _tabBarHeight = 32;

    // Text input from Window.TextInput event (supports Unicode)
    private char? _pendingTextInput;
    private bool _pendingPaste;

    private int ScreenWidth => GraphicsDevice?.Viewport.Width ?? _graphics.PreferredBackBufferWidth;
    private int ScreenHeight => GraphicsDevice?.Viewport.Height ?? _graphics.PreferredBackBufferHeight;

    public GameField(IPlatformServices platformServices, IInputHandler inputHandler,
        IStatusService statusService, ICircuitManager circuitManager, ICameraController cameraController,
        IComponentBuilder componentBuilder, IDialogService dialogService, IFontService fontService,
        IWireManager wireManager, IManualWireService manualWireService, ICommandHandler commandHandler,
        IToolboxManager toolboxManager, IGameRenderer gameRenderer, ITruthTableService truthTableService,
        ILevelService levelService, IProfileService profileService, IAppearanceService appearanceService,
        IPreferencesService preferencesService)
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
        _appearanceService = appearanceService;
        _preferencesService = preferencesService;

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
            // Check if we're editing a component - if so, save it instead of creating new
            if (_editingComponentName != null)
            {
                SaveEditedComponent();
            }
            else
            {
                _dialogService.StartNaming(_selection.GetSelectedComponents());
                if (_dialogService.IsActive)
                {
                    _inputHandler.BeginTextInput();
                }
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

        // Use Window.TextInput for proper Unicode character input
        Window.TextInput += OnTextInput;

        // Load preferences first to get the last used profile
        _preferencesService.Load();

        // If there's a last profile, load it so components from that profile are available
        if (!string.IsNullOrEmpty(_preferencesService.LastProfile))
        {
            _profileService.LoadProfile(_preferencesService.LastProfile);
        }

        // Load components (will load from global folder + profile folder if profile is loaded)
        _componentBuilder.LoadCustomComponents();
        _statusService.Show(LocalizationManager.Get("help.drag"));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _fontService.Initialize(GraphicsDevice);
        _gameRenderer.Initialize(GraphicsDevice, _fontService);
        _toolboxManager.Initialize(ScreenWidth, _componentBuilder, _appearanceService);
        _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
        _truthTableService.Initialize(ScreenWidth);

        // Set camera viewport offset to position working area below tab bar (menu hidden by default)
        UpdateViewportOffset();

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
        _levelInfoWindow = new LevelInfoWindow(50, 80);
        _designerMode = new DesignerMode(_appearanceService, _componentBuilder, _fontService);
        _designerMode.SetClipboardGetter(GetClipboardText);
        _designerMode.OnAppearanceSaved += ApplyAppearancesToCircuit;

        _circuitManager.OnCircuitChanged += ApplyAppearancesToCircuit;

        // Initialize new game menu and dialogs
        _mainGameMenu = new MainGameMenu(_preferencesService, _fontService);
        _optionsDialog = new OptionsDialog();
        _confirmationDialog = new ConfirmationDialog();
        _componentContextMenu = new ComponentContextMenu();
        _tabBar = new TabBar();

        // Initialize main circuit tab (start in sandbox mode)
        _tabBar.AddTab(new EditorTab("Sandbox", _circuitManager.Circuit, GameMode.Sandbox, level: null, isMainCircuit: true));

        // Tab bar event handlers
        _tabBar.OnTabSelected += HandleTabSelected;
        _tabBar.OnTabCloseRequested += HandleTabCloseRequested;

        // Update window title based on active tab
        UpdateWindowTitle();

        // Component context menu event handlers
        _componentContextMenu.OnEdit += HandleEditComponent;
        _componentContextMenu.OnDelete += HandleDeleteComponent;

        // Confirmation dialog event handlers
        _confirmationDialog.OnYes += () =>
        {
            _confirmationDialog.Hide();

            // Handle tab close confirmation
            if (_tabCloseRequestIndex >= 0)
            {
                // Save the tab before closing
                var tab = _tabBar.GetTab(_tabCloseRequestIndex);
                if (tab != null && !tab.IsMainCircuit)
                {
                    // Switch to the tab, save it, then close
                    _tabBar.SetActiveTab(_tabCloseRequestIndex);
                    SaveEditedComponent();
                }
                _tabBar.RemoveTab(_tabCloseRequestIndex);
                _tabCloseRequestIndex = -1;
            }
        };

        _confirmationDialog.OnNo += () =>
        {
            _confirmationDialog.Hide();

            // Handle tab close confirmation - close without saving
            if (_tabCloseRequestIndex >= 0)
            {
                _tabBar.RemoveTab(_tabCloseRequestIndex);
                _tabCloseRequestIndex = -1;
            }
        };

        // Main game menu event handlers
        _mainGameMenu.OnContinue += HandleContinue;
        _mainGameMenu.OnNewGame += () =>
        {
            _profileDialog.Show();
            _mainGameMenu.Hide();
        };
        _mainGameMenu.OnSandbox += HandleSandboxSwitch;
        _mainGameMenu.OnDesigner += HandleDesignerSwitch;
        _mainGameMenu.OnOptions += () =>
        {
            _optionsDialog.SetCurrentFontSize(_gameRenderer.TitleFontScale);
            _optionsDialog.Show();
            _mainGameMenu.Hide();
        };
        _mainGameMenu.OnQuit += Exit;

        // Options dialog event handlers
        _optionsDialog.OnFontSizeChanged += scale =>
        {
            _gameRenderer.TitleFontScale = scale;
        };
        _optionsDialog.OnLanguageChanged += code =>
        {
            LocalizationManager.LoadLanguage(code);
            Window.Title = LocalizationManager.Get("app.title");
            _statusService.Show(LocalizationManager.Get("status.ready"));
        };
        _optionsDialog.OnShowControls += () =>
        {
            _controlsPopup.Show();
        };
        _optionsDialog.OnClose += () =>
        {
            _mainGameMenu.Show();
        };

        // Show main game menu at startup
        _mainGameMenu.Show();

        _mainMenu.OnSandboxMode += () =>
        {
            // Check if a sandbox tab already exists
            for (int i = 0; i < _tabBar.Tabs.Count; i++)
            {
                if (_tabBar.Tabs[i].GameMode == GameMode.Sandbox)
                {
                    // Switch to existing sandbox tab
                    _tabBar.SetActiveTab(i);
                    return;
                }
            }

            // Create new sandbox tab
            _componentBuilder.LoadCustomComponents();
            _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
            _designerMode.Deactivate();

            var newCircuit = new CPUgame.Core.Circuit.Circuit();
            var newTab = new EditorTab("Sandbox", newCircuit, GameMode.Sandbox, level: null, isMainCircuit: false);
            _tabBar.AddTab(newTab);

            _mainMenu.SetProfileName(null);
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

            // Show level selection popup
            _levelSelectionPopup.Show();
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
        _mainMenu.OnDesignerMode += () =>
        {
            _levelService.SetMode(GameMode.Designer);
            _mainMenu.SetCurrentMode(GameMode.Designer);
            _mainMenu.SetProfileName(null);
            _levelInfoWindow.SetLevel(null, null);
            _designerMode.Activate();
            _statusService.Show(LocalizationManager.Get("status.mode_designer"));
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

        _levelService.OnLevelChanged += () =>
        {
            if (_levelService.CurrentLevel != null && _profileService.HasProfile)
            {
                int levelIndex = _levelService.Levels.FindIndex(l => l.Id == _levelService.CurrentLevel.Id);
                if (levelIndex >= 0)
                {
                    _preferencesService.SetLastSession(_profileService.CurrentProfile!.Name, levelIndex);
                }
            }
        };

        _profileService.OnProfileChanged += () =>
        {
            if (_profileService.HasProfile && _profileService.CurrentProfile != null)
            {
                _preferencesService.LastProfile = _profileService.CurrentProfile.Name;
            }
        };

        _levelDescriptionPopup.OnStartLevel += () =>
        {
            if (_levelService.CurrentLevel != null)
            {
                var level = _levelService.CurrentLevel;

                // Check if this level already has a tab open
                for (int i = 0; i < _tabBar.Tabs.Count; i++)
                {
                    var tab = _tabBar.Tabs[i];
                    if (tab.GameMode == GameMode.Levels && tab.Level?.Id == level.Id)
                    {
                        // Switch to existing level tab
                        _tabBar.SetActiveTab(i);
                        return;
                    }
                }

                // Create new circuit for the level
                var levelCircuit = new CPUgame.Core.Circuit.Circuit();
                _levelService.SetupLevelCircuit(levelCircuit, _gameRenderer.GridSize);

                // Create new tab for this level
                var newTab = new EditorTab(level.Name, levelCircuit, GameMode.Levels, level, isMainCircuit: false);
                _tabBar.AddTab(newTab);

                _componentBuilder.LoadCustomComponents();
                _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
                _mainMenu.SetProfileName(_profileService.CurrentProfile?.Name);
                _toolboxManager.SetLevelModeFilter(true, _profileService.GetUnlockedComponents(_levelService));
                _truthTableService.SetCurrentLevel(level);
                _truthTableService.Show(levelCircuit, _fontService.GetFont());
                _levelInfoWindow.SetLevel(level, _fontService.GetFont());

                // Position windows to avoid overlap
                // Level info window: top-left, below the tab bar
                int topOffset = _isMenuVisible ? _menuHeight + _tabBarHeight : _tabBarHeight;
                _levelInfoWindow.SetPosition(8, topOffset + 8);

                // Truth table window: top-right side of the screen
                _truthTableService.SetPosition(ScreenWidth - 280, topOffset + 8);
            }
        };

        _profileDialog.OnProfileSelected += () =>
        {
            _componentBuilder.LoadCustomComponents();
            _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
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
            // Close current level tab
            int currentTabIndex = _tabBar.ActiveTabIndex;
            if (currentTabIndex >= 0)
            {
                _tabBar.RemoveTab(currentTabIndex);
            }

            // Find first uncompleted level that is unlocked
            GameLevel? nextLevel = null;
            int nextLevelIndex = -1;
            for (int i = 0; i < _levelService.Levels.Count; i++)
            {
                var level = _levelService.Levels[i];
                if (!_profileService.IsLevelCompleted(level.Id) &&
                    _profileService.IsTierUnlocked(level.Tier, _levelService))
                {
                    nextLevel = level;
                    nextLevelIndex = i;
                    break;
                }
            }

            // If there's an uncompleted level, open it
            if (nextLevel != null && nextLevelIndex >= 0)
            {
                _levelService.SetMode(GameMode.Levels);
                _mainMenu.SetCurrentMode(GameMode.Levels);
                _levelService.SelectLevel(nextLevelIndex);
                _levelDescriptionPopup.Show(nextLevel);
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

        // Handle toggle menu visibility (Ctrl+Shift+M)
        if (_inputState.ToggleMenuCommand)
        {
            ToggleMenuVisibility();
        }

        // Handle profile dialog (modal, blocks everything else)
        if (_profileDialog.IsVisible)
        {
            _profileDialog.HandleInput(_inputState, _inputHandler);
            _profileDialog.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight, _inputHandler);
            base.Update(gameTime);
            return;
        }

        // Handle main game menu (modal)
        if (_mainGameMenu.IsVisible)
        {
            _mainGameMenu.Update(_inputState, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle options dialog (modal)
        if (_optionsDialog.IsVisible)
        {
            _optionsDialog.Update(_inputState, mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle confirmation dialog (modal)
        if (_confirmationDialog.IsVisible)
        {
            _confirmationDialog.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, ScreenHeight);
            base.Update(gameTime);
            return;
        }

        // Handle component context menu (modal)
        if (_componentContextMenu.IsVisible)
        {
            _componentContextMenu.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, _inputState.SecondaryJustPressed);
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

        if (_isMenuVisible)
        {
            _mainMenu.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, ScreenWidth);
            if (_mainMenu.ContainsPoint(mousePosMonoGame)) { base.Update(gameTime); return; }
        }

        // Handle Designer mode (full-screen UI, blocks other gameplay)
        if (_designerMode.IsActive)
        {
            // Handle paste (Ctrl+V) - check this first before text input
            if (_pendingPaste)
            {
                var clipboardText = GetClipboardText();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    _designerMode.HandlePaste(clipboardText);
                }
                _pendingPaste = false;
            }

            // Handle text input for editing fields (from Window.TextInput event)
            if (_pendingTextInput.HasValue)
            {
                _designerMode.HandleTextInput(_pendingTextInput.Value);
            }
            _pendingTextInput = null; // Clear after processing

            _designerMode.HandleKeyPress(_inputState.BackspacePressed, _inputState.EnterPressed, _inputState.EscapeCommand);
            _designerMode.Update(mousePosMonoGame, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed,
                _inputState.PrimaryJustReleased, _inputState.SecondaryJustPressed, _inputState.ScrollDelta, ScreenWidth, ScreenHeight, gameTime.ElapsedGameTime.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        // Update truth table window
        _truthTableService.Update(mousePosMonoGame, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, _inputState.ScrollDelta, circuit, gameTime.ElapsedGameTime.TotalSeconds);
        if (_truthTableService.ContainsPoint(mousePosMonoGame)) { base.Update(gameTime); return; }

        // Update level info window
        _levelInfoWindow.Update(mousePosMonoGame, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased, ScreenWidth, ScreenHeight, _fontService.GetFont());
        if (_levelInfoWindow.ContainsPoint(mousePosMonoGame)) { base.Update(gameTime); return; }

        // Update tab bar (position below main menu if visible)
        int tabYOffset = GetTabBarYOffset();
        _tabBar.Update(mousePosMonoGame, _inputState.PrimaryJustPressed, ScreenWidth, yOffset: tabYOffset);
        if (_tabBar.ContainsPoint(mousePosMonoGame, yOffset: tabYOffset)) { base.Update(gameTime); return; }

        // Handle ESC key to open/close main game menu
        if (_inputState.EscapeCommand)
        {
            if (_wireManager.IsDraggingWire)
            {
                _wireManager.Cancel();
            }
            else if (_manualWireService.IsActive)
            {
                _manualWireService.Cancel();
            }
            else if (_manualWireService.IsEditingWire)
            {
                _manualWireService.StopEditingWire();
            }
            else if (_selection.GetSelectedComponents().Count > 0)
            {
                _selection.ClearAll();
            }
            else
            {
                _mainGameMenu.Show();
            }
            base.Update(gameTime);
            return;
        }

        // Handle RMB to cancel wiring
        if (_inputState.SecondaryJustPressed)
        {
            if (_wireManager.IsDraggingWire)
            {
                _wireManager.Cancel();
                base.Update(gameTime);
                return;
            }
            else if (_manualWireService.IsActive)
            {
                _manualWireService.Cancel();
                base.Update(gameTime);
                return;
            }
        }

        if (_inputState.CtrlHeld && _inputState.ScrollDelta != 0)
        {
            _camera.HandleZoom(_inputState.ScrollDelta, mousePos, _camera.ScreenToWorld);
        }

        var worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        bool clickedOnEmpty = !_toolboxManager.ContainsPoint(mousePosMonoGame) && !_truthTableService.ContainsPoint(mousePosMonoGame) &&
                              !_levelInfoWindow.ContainsPoint(mousePosMonoGame) &&
                              circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y) == null &&
                              circuit.GetPinAt(worldMousePos.X, worldMousePos.Y) == null;

        // Show context menu on right-click on custom component (only in sandbox mode)
        if (_inputState.SecondaryJustPressed && !_toolboxManager.ContainsPoint(mousePosMonoGame) &&
            !_truthTableService.ContainsPoint(mousePosMonoGame) && _levelService.CurrentMode == GameMode.Sandbox)
        {
            var component = circuit.GetComponentAt(worldMousePos.X, worldMousePos.Y);
            if (component is CustomComponent customComp)
            {
                _componentBeingEdited = customComp;
                _editingComponentName = customComp.ComponentName;
                _componentContextMenu.Show(mousePosMonoGame.X, mousePosMonoGame.Y, ScreenWidth, ScreenHeight);
                base.Update(gameTime);
                return;
            }
        }

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

        var componentsCountBefore = circuit.Components.Count;
        _commandHandler.HandleCommands(_inputState, _selection, circuit, _wireManager, _gameRenderer.GridSize);
        if (circuit.Components.Count != componentsCountBefore)
        {
            MarkActiveTabDirty();
        }

        _toolboxManager.Update(mousePosMonoGame, _inputState.PrimaryPressed, _inputState.PrimaryJustPressed, _inputState.PrimaryJustReleased);

        worldMousePos = _camera.ScreenToWorldPoint(mousePos);
        var placedComponent = _toolboxManager.HandleDrops(mousePosMonoGame, worldMousePos.ToMonoGame(), circuit, _gameRenderer.GridSize, _commandHandler.ShowPinValues, _inputState.PrimaryJustReleased, _statusService, _componentBuilder);
        if (placedComponent != null)
        {
            _selection.SelectComponent(placedComponent);
            MarkActiveTabDirty();
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

    private void ApplyAppearancesToCircuit()
    {
        foreach (var component in _circuitManager.Circuit.Components)
        {
            _appearanceService.ApplyAppearance(component);
        }

        // Update wire endpoints after pin positions may have changed
        UpdateAllWireEndpoints();
    }

    private void UpdateAllWireEndpoints()
    {
        foreach (var component in _circuitManager.Circuit.Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ManualWirePath != null && input.ConnectedTo != null)
                {
                    _manualWireService.UpdateWireEndpoints(input);
                }
            }
        }
    }

    private static string? GetClipboardText()
    {
        try
        {
            return TextCopy.ClipboardService.GetText();
        }
        catch
        {
            // Clipboard access failed
            return null;
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        // Ctrl+V produces ASCII 22 (0x16) - detect paste
        if (e.Character == '\x16')
        {
            _pendingPaste = true;
            return;
        }

        // Store the character for processing in Update
        // Don't capture control characters except for specific cases
        if (!char.IsControl(e.Character) || e.Character == '\b' || e.Character == '\r' || e.Character == '\t')
        {
            _pendingTextInput = e.Character;
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
                if (_manualWireService.Complete(hoveredPin))
                {
                    MarkActiveTabDirty();
                }
            }
            else
            {
                // Add point to path
                _manualWireService.AddPoint(worldMousePos);
            }
        }
    }

    private void HandleContinue()
    {
        var lastProfile = _preferencesService.LastProfile;

        if (string.IsNullOrEmpty(lastProfile))
        {
            _profileDialog.Show();
            _mainGameMenu.Hide();
            return;
        }

        _profileService.LoadProfile(lastProfile);

        if (!_profileService.HasProfile)
        {
            _statusService.Show(LocalizationManager.Get("status.profile_not_found"));
            _profileDialog.Show();
            _mainGameMenu.Hide();
            return;
        }

        _componentBuilder.LoadCustomComponents();
        _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
        _levelService.SetMode(GameMode.Levels);
        _mainMenu.SetCurrentMode(GameMode.Levels);
        _mainMenu.SetProfileName(_profileService.CurrentProfile?.Name);
        _toolboxManager.SetLevelModeFilter(true, _profileService.GetUnlockedComponents(_levelService));

        int levelIndex = _preferencesService.LastLevelIndex;
        if (levelIndex < 0 || levelIndex >= _levelService.Levels.Count)
        {
            levelIndex = FindFirstIncompleteLevel();
        }

        _levelService.SelectLevel(levelIndex);
        if (_levelService.CurrentLevel != null)
        {
            _levelDescriptionPopup.Show(_levelService.CurrentLevel);
        }

        _mainGameMenu.Hide();
    }

    private int FindFirstIncompleteLevel()
    {
        for (int i = 0; i < _levelService.Levels.Count; i++)
        {
            var level = _levelService.Levels[i];
            if (!_profileService.IsLevelCompleted(level.Id))
            {
                return i;
            }
        }
        return 0;
    }

    private void HandleSandboxSwitch()
    {
        if (_levelService.CurrentMode == GameMode.Levels && _circuitManager.Circuit.Components.Count > 0)
        {
            Action? yesHandler = null;
            Action? noHandler = null;

            yesHandler = () =>
            {
                SwitchToSandboxMode();
                _confirmationDialog.Hide();
                if (yesHandler != null)
                {
                    _confirmationDialog.OnYes -= yesHandler;
                }
                if (noHandler != null)
                {
                    _confirmationDialog.OnNo -= noHandler;
                }
            };

            noHandler = () =>
            {
                _confirmationDialog.Hide();
                if (yesHandler != null)
                {
                    _confirmationDialog.OnYes -= yesHandler;
                }
                if (noHandler != null)
                {
                    _confirmationDialog.OnNo -= noHandler;
                }
            };

            _confirmationDialog.OnYes += yesHandler;
            _confirmationDialog.OnNo += noHandler;

            _confirmationDialog.Show(
                LocalizationManager.Get("confirm.discard_title"),
                LocalizationManager.Get("confirm.discard_progress")
            );
        }
        else
        {
            SwitchToSandboxMode();
        }
    }

    private void SwitchToSandboxMode()
    {
        // Check if a sandbox tab already exists
        for (int i = 0; i < _tabBar.Tabs.Count; i++)
        {
            if (_tabBar.Tabs[i].GameMode == GameMode.Sandbox)
            {
                // Switch to existing sandbox tab
                _tabBar.SetActiveTab(i);
                _mainGameMenu.Hide();
                return;
            }
        }

        // Create new sandbox tab
        _componentBuilder.LoadCustomComponents();
        _toolboxManager.LoadCustomComponents(_circuitManager.CustomComponents.Keys);
        _designerMode.Deactivate();

        var newCircuit = new CPUgame.Core.Circuit.Circuit();
        var newTab = new EditorTab("Sandbox", newCircuit, GameMode.Sandbox, level: null, isMainCircuit: false);
        _tabBar.AddTab(newTab);

        _mainMenu.SetProfileName(null);
        _toolboxManager.SetLevelModeFilter(false, null);
        _statusService.Show(LocalizationManager.Get("status.mode_sandbox"));
        _mainGameMenu.Hide();
    }

    private void HandleDesignerSwitch()
    {
        if (_circuitManager.Circuit.Components.Count > 0)
        {
            Action? yesHandler = null;
            Action? noHandler = null;

            yesHandler = () =>
            {
                _circuitManager.SaveCircuit();
                SwitchToDesignerMode();
                _confirmationDialog.Hide();
                if (yesHandler != null)
                {
                    _confirmationDialog.OnYes -= yesHandler;
                }
                if (noHandler != null)
                {
                    _confirmationDialog.OnNo -= noHandler;
                }
            };

            noHandler = () =>
            {
                SwitchToDesignerMode();
                _confirmationDialog.Hide();
                if (yesHandler != null)
                {
                    _confirmationDialog.OnYes -= yesHandler;
                }
                if (noHandler != null)
                {
                    _confirmationDialog.OnNo -= noHandler;
                }
            };

            _confirmationDialog.OnYes += yesHandler;
            _confirmationDialog.OnNo += noHandler;

            _confirmationDialog.Show(
                LocalizationManager.Get("confirm.save_title"),
                LocalizationManager.Get("confirm.save_before_designer")
            );
        }
        else
        {
            SwitchToDesignerMode();
        }
    }

    private void SwitchToDesignerMode()
    {
        _levelService.SetMode(GameMode.Designer);
        _mainMenu.SetCurrentMode(GameMode.Designer);
        _mainMenu.SetProfileName(null);
        _levelInfoWindow.SetLevel(null, null);
        _designerMode.Activate();
        _statusService.Show(LocalizationManager.Get("status.mode_designer"));
        _mainGameMenu.Hide();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(CircuitRenderer.BackgroundColor);

        // Designer mode has its own full-screen UI
        if (_designerMode.IsActive)
        {
            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            var uiFont = _fontService.GetFont();
            _designerMode.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, ScreenHeight, _inputState.PointerPosition.ToMonoGame());
            _mainMenu.Draw(_spriteBatch, _gameRenderer.Pixel, uiFont, ScreenWidth, _inputState.PointerPosition.ToMonoGame());
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: _camera.GetTransform());
        _gameRenderer.DrawWorld(_spriteBatch, _circuitManager.Circuit, _camera, _selection, _wireManager, _manualWireService, _wireManager.HoveredPin, _inputState.PointerPosition, ScreenWidth, ScreenHeight, _toolboxManager.MainToolbox.IsDraggingItem);
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        var uiFontNormal = _fontService.GetFont();
        var menuFont = _fontService.GetFontAtSize(24);

        // Draw tab bar (position below main menu if visible)
        _tabBar.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, yOffset: GetTabBarYOffset());

        // Draw UI elements (main menu only if visible)
        _gameRenderer.DrawUI(_spriteBatch, _toolboxManager, _mainMenu, _statusService, _dialogService, _truthTableService, _camera, _inputState.PointerPosition, ScreenWidth, ScreenHeight, uiFontNormal, _isMenuVisible);

        // Draw level info window (floating, non-modal)
        _levelInfoWindow.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal);

        // Draw modal dialogs on top
        _profileDialog.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _levelSelectionPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _levelDescriptionPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _levelCompletedPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _componentEditDialog.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _controlsPopup.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _mainGameMenu.Draw(_spriteBatch, _gameRenderer.Pixel, menuFont, ScreenWidth, ScreenHeight);
        _optionsDialog.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _confirmationDialog.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal, ScreenWidth, ScreenHeight);
        _componentContextMenu.Draw(_spriteBatch, _gameRenderer.Pixel, uiFontNormal);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleEditComponent()
    {
        if (_editingComponentName == null || _componentBeingEdited == null)
        {
            return;
        }

        // Load the component's internal circuit for editing
        if (!_circuitManager.CustomComponents.TryGetValue(_editingComponentName, out var circuitData))
        {
            _statusService.Show("Component data not found");
            return;
        }

        // Check if this component is already open in a tab
        for (int i = 0; i < _tabBar.Tabs.Count; i++)
        {
            var tab = _tabBar.Tabs[i];
            if (!tab.IsMainCircuit && tab.Name == _editingComponentName)
            {
                // Switch to existing tab
                _tabBar.SetActiveTab(i);
                _componentBeingEdited = null;
                return;
            }
        }

        // Load the component's internal circuit for editing
        var internalCircuit = CircuitSerializer.DeserializeCircuit(circuitData, _circuitManager.CustomComponents);
        internalCircuit.Name = _editingComponentName;

        // Create a new tab for this component (use Designer mode for component editing)
        var newTab = new EditorTab(_editingComponentName, internalCircuit, GameMode.Designer, level: null, isMainCircuit: false);
        _tabBar.AddTab(newTab);

        // The AddTab method automatically switches to the new tab and triggers OnTabSelected
        _statusService.Show(LocalizationManager.Get("status.editing_component", _editingComponentName));

        // Clear the component being edited reference
        _componentBeingEdited = null;
    }

    private void HandleDeleteComponent()
    {
        if (_editingComponentName == null || _componentBeingEdited == null)
        {
            return;
        }

        // Remove the component from the circuit
        _circuitManager.Circuit.RemoveComponent(_componentBeingEdited);

        // Delete the component definition
        _componentBuilder.DeleteComponent(_editingComponentName);

        // Clear references
        _editingComponentName = null;
        _componentBeingEdited = null;
    }

    private void SaveEditedComponent()
    {
        if (_editingComponentName == null)
        {
            return;
        }

        // Get all components in the current circuit
        var allComponents = _circuitManager.Circuit.Components.ToList();

        // Get existing appearance
        ComponentAppearance? existingAppearance = null;
        if (_circuitManager.CustomComponents.TryGetValue(_editingComponentName, out var existingData))
        {
            existingAppearance = existingData.Appearance;
        }

        // Rebuild the component with the same name
        bool success = _componentBuilder.BuildComponent(_editingComponentName, allComponents, _gameRenderer.GridSize, existingAppearance);

        if (success)
        {
            _statusService.Show(LocalizationManager.Get("status.component_updated", _editingComponentName));

            // Clear editing state and mark tab as clean
            _editingComponentName = null;
            if (_tabBar.ActiveTab != null)
            {
                _tabBar.ActiveTab.IsDirty = false;
            }

            // Update all other open tabs that use this component
            UpdateComponentInAllTabs(_circuitManager.Circuit.Name);
        }
    }

    private void HandleTabSelected(int tabIndex)
    {
        var tab = _tabBar.GetTab(tabIndex);
        if (tab == null)
        {
            return;
        }

        // Switch to the tab's circuit
        typeof(CircuitManager).GetProperty("Circuit")!.SetValue(_circuitManager, tab.Circuit);
        _selection = new SelectionManager(_circuitManager.Circuit);

        // Update game mode and level based on tab
        _levelService.SetMode(tab.GameMode);
        _mainMenu.SetCurrentMode(tab.GameMode);

        if (tab.GameMode == GameMode.Levels && tab.Level != null)
        {
            // Restore level state
            _truthTableService.SetCurrentLevel(tab.Level);
            _levelInfoWindow.SetLevel(tab.Level, _fontService.GetFont());
        }
        else
        {
            _truthTableService.SetCurrentLevel(null);
            _levelInfoWindow.SetLevel(null, null);
        }

        // Update editing state based on tab
        if (tab.IsMainCircuit)
        {
            _editingComponentName = null;
        }
        else
        {
            _editingComponentName = tab.Name;
        }

        // Update window title
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        string baseTitle = LocalizationManager.Get("app.title");
        var activeTab = _tabBar.ActiveTab;

        if (activeTab != null)
        {
            string tabTitle = activeTab.GetDisplayName();
            Window.Title = $"{baseTitle} - {tabTitle}";
        }
        else
        {
            Window.Title = baseTitle;
        }
    }

    private void HandleTabCloseRequested(int tabIndex)
    {
        var tab = _tabBar.GetTab(tabIndex);
        if (tab == null)
        {
            return;
        }

        // Don't allow closing the main circuit tab
        if (tab.IsMainCircuit)
        {
            return;
        }

        // If tab has unsaved changes, ask for confirmation
        if (tab.IsDirty)
        {
            _tabCloseRequestIndex = tabIndex;
            _confirmationDialog.Show(
                LocalizationManager.Get("confirm.save_title"),
                LocalizationManager.Get("confirm.save_before_close", tab.Name)
            );
        }
        else
        {
            // Close tab directly
            _tabBar.RemoveTab(tabIndex);
        }
    }

    private void UpdateComponentInAllTabs(string componentName)
    {
        // Reload the component data
        if (!_circuitManager.CustomComponents.TryGetValue(componentName, out var circuitData))
        {
            return;
        }

        // Update all tabs that are editing this component (except the current one)
        for (int i = 0; i < _tabBar.Tabs.Count; i++)
        {
            var tab = _tabBar.Tabs[i];
            if (!tab.IsMainCircuit && tab.Name == componentName && i != _tabBar.ActiveTabIndex)
            {
                // Reload the circuit for this tab
                var updatedCircuit = CircuitSerializer.DeserializeCircuit(circuitData, _circuitManager.CustomComponents);
                updatedCircuit.Name = componentName;
                tab.Circuit = updatedCircuit;
            }
        }
    }

    private void MarkActiveTabDirty()
    {
        if (_tabBar.ActiveTab != null && !_tabBar.ActiveTab.IsMainCircuit)
        {
            _tabBar.ActiveTab.IsDirty = true;
        }
    }

    private void UpdateViewportOffset()
    {
        int yOffset = _isMenuVisible ? _menuHeight + _tabBarHeight : _tabBarHeight;
        _camera.ViewportOffset = new Microsoft.Xna.Framework.Vector2(0, yOffset);
    }

    private int GetTabBarYOffset()
    {
        return _isMenuVisible ? _menuHeight : 0;
    }

    private void ToggleMenuVisibility()
    {
        _isMenuVisible = !_isMenuVisible;
        UpdateViewportOffset();
    }
}
