using System;

namespace CPUgame;

public interface IGameField : IDisposable
{
    void Run();
}