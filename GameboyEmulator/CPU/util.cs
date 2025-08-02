using System.Numerics;
using static GameboyEmulator.CPU.LR35902;

namespace GameboyEmulator;

public static class util
{
    public static byte ReadCode(byte opCode, byte mask)
    {
        var shift = BitOperations.TrailingZeroCount(mask);
        return (byte)((opCode & mask) >> shift);
    }
    
    public static byte ReadCode(byte opCode, Masks mask)
    {
        var m = (byte)mask;
        var shift = BitOperations.TrailingZeroCount(m);
        return (byte)((opCode & m) >> shift);
    }
}