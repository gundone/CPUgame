using CPUgame;
using CPUgame.Core;
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
services.AddSingleton<IWireManager, WireManager>();
services.AddSingleton<ICommandHandler, CommandHandler>();
services.AddSingleton<IToolboxManager, ToolboxManager>();
services.AddSingleton<IGameRenderer, GameRenderer>();
services.AddSingleton<ITruthTableService, TruthTableService>();

// Game
services.AddSingleton<IGameField, GameField>();

// Run the game
using var game = services.Get<IGameField>();
game.Run();
