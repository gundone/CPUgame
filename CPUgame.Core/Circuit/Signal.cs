namespace CPUgame.Core.Circuit;

/// <summary>
/// Represents a digital signal state
/// </summary>
public enum Signal
{
    Low = 0,
    High = 1,
    // Undefined = -1
}

public static class SignalExtensions
{
    public static Signal Nand(this Signal a, Signal b)
    {
        //if (a == Signal.Undefined || b == Signal.Undefined)
        //    return Signal.Undefined;

        return (a == Signal.High && b == Signal.High) ? Signal.Low : Signal.High;
    }

    public static Signal Not(this Signal a)
    {
        return a switch
        {
            Signal.High => Signal.Low,
            Signal.Low => Signal.High,
            _ => Signal.Low
        };
    }

    public static Signal And(this Signal a, Signal b) => a.Nand(b).Not();

    public static Signal Or(this Signal a, Signal b) => a.Not().Nand(b.Not());

    public static Signal Xor(this Signal a, Signal b)
    {
        var nandAB = a.Nand(b);
        return a.Nand(nandAB).Nand(b.Nand(nandAB));
    }
}
