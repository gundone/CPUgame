using CPUgame.Core;
using Microsoft.Xna.Framework.Input;

namespace CPUgame.Components;

/// <summary>
/// The fundamental NAND gate - all other gates can be built from this
/// </summary>
public class NandGate : Component
{
    public Pin InputA { get; }
    public Pin InputB { get; }
    public Pin Output { get; }

    public NandGate(int x, int y) : base(x, y)
    {
        Name = "NAND";
        Width = 40;
        Height = 60;

        InputA = AddInput("A", 0, 20);
        InputB = AddInput("B", 0, 40);
        Output = AddOutput("Out", 40, 40);
    }

    public override void Evaluate()
    {
        Output.Value = InputA.Value.Nand(InputB.Value);
    }

}
