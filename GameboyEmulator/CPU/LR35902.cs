namespace GameboyEmulator;

public class LR35902
{
    public Registers Registers;

    public Bus Bus;
    
    private byte opCode;
    private ushort fetched;
    private bool isCB;
    private int cycles;
    
    private Instruction[] instructions;
    private Instruction[] cbInstructions;

    public LR35902(Bus bus)
    {
        this.Bus = bus;
        Registers = new(this);
        instructions = new Instruction[0xFF];
        cbInstructions = new Instruction[0xFF];
    }

    public void Clock()
    {
        opCode = Bus.Read(Registers.PC);
    }

    public void fetch()
    {
        fetched = R8R8();
    }
    
    // Addressing Modes
    private ushort IMP()
    {
        return 0;
    }

    private ushort R8()
    {
        var code = isCB ? (byte)(opCode & 0b00000111) : (byte)(opCode & 0b00111000 >> 3);
        var val = Registers.GetR8(code);
        return val;
    }

    private ushort R8R8()
    {
        var code = (byte)(opCode & 0b00000111);
        var val = Registers.GetR8(code);
        return val;
    }

    private ushort R16MEM()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        return Registers.GetR16Mem(code);
    }
    
    private ushort IMM8()
    {
        var val = Bus.Read(Registers.PC);
        Registers.PC++;
        return val;
    }

    private ushort IMM16()
    {
        var hi = Bus.Read(Registers.PC);
        Registers.PC++;
        var lo = Bus.Read(Registers.PC);
        Registers.PC++;
        return (ushort)(hi << 8 | lo);
    }

    private ushort R16()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        return Registers.GetR16(code);
    }

    private ushort R16STK()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        return Registers.GetR16Stk(code);
    }
    
    {
    }

    {
        var code = (byte)((opCode & 0b00110000) >> 4);
    }

    {
    }

    {
    }

    {
    }

    {
    }
}