namespace GameboyEmulator;

public readonly record struct Instruction(string name, Action operate, Func<ushort> addressingMode, byte cycles, byte cyclesBranch = 0)
{
    public readonly string Name = name;
    public readonly Action Typ = operate;
    public readonly Func<ushort> AddressingMode = addressingMode;
    public readonly byte Cycles = cycles;
    public readonly byte CyclesBranch = cyclesBranch;
}