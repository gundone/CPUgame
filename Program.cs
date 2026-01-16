using CPUgame;
using CPUgame.Core;
using CPUgame.Core.Designer;
using CPUgame.Core.Input;
using CPUgame.Core.Services;
using CPUgame.Input;
using CPUgame.Platform;
using CPUgame.Rendering;

// Configure services
var services = ServiceContainer.Instance;

// Platform services
services.AddSingleton<IPlatformServices, DesktopPlatformServices>();
services.AddSingleton<IInputHandler, DesktopInputHandler>();

// Core services
services.AddSingleton<IStatusService, StatusService>();
services.AddSingleton<ICircuitManager, CircuitManager>();
services.AddSingleton<ICameraController, CameraController>();
services.AddSingleton<IComponentBuilder, ComponentBuilder>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<IWireManager, WireManager>();
services.AddSingleton<IManualWireService, ManualWireService>();
services.AddSingleton<ICommandHandler, CommandHandler>();
services.AddSingleton<IToolboxManager, ToolboxManager>();
services.AddSingleton<IPrimitiveDrawer, PrimitiveDrawer>();
services.AddSingleton<IFontService, FontService>();
services.AddSingleton<ICircuitRenderer, CircuitRenderer>();
services.AddSingleton<IGameRenderer, GameRenderer>();
services.AddSingleton<ITruthTableService, TruthTableService>();
services.AddSingleton<ILevelService, LevelService>();
services.AddSingleton<IProfileService, ProfileService>();
services.AddSingleton<IAppearanceService, AppearanceService>();
services.AddSingleton<IPreferencesService, PreferencesService>();

// Game
services.AddSingleton<IGameField, GameField>();

// Run the game
using var game = services.Get<IGameField>();
game.Run();
