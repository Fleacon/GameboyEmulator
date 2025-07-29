namespace GameboyEmulator;

public class LR35902
{
    public Registers Registers;

    public Bus Bus;
    
    private byte opCode;
    private ushort fetched;
    private ushort memDest;
    private bool isCB;

    public LR35902(Bus bus)
    {
        this.Bus = bus;
        Registers = new(this);
    }

    public void clock()
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

    private byte R8()
    {
        var code = isCB ? (byte)(opCode & 0b00000111) : (byte)(opCode & 0b00111000 >> 3);
        var val = Registers.GetR8(code, out var isAddress);
        if (isAddress) memDest = Registers.HL;
        return val;
    }

    private byte R8R8()
    {
        var code = (byte)(opCode & 0b00000111);
        return Registers.GetR8(code, out _);
    }

    private byte R8IMM8()
    {
        var val = Bus.Read(Registers.PC);
        Registers.PC++;
        return val;
    }

    private byte R16MEMA()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        memDest = Registers.GetR16Mem(code);
        return Registers.A;
    }

    private byte AR16MEM()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        return Bus.Read(Registers.GetR16Mem(code));
    }

    private byte IMM8()
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

    private byte AR8()
    {
        var code = (byte)(opCode & 0b00000111);
        return Registers.GetR8(code, out _);
    }

    private ushort R16STK()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        return Registers.GetR16Stk(code);
    }

    private byte IMM16MEM()
    {
        var hi = Bus.Read(Registers.PC);
        Registers.PC++;
        var lo = Bus.Read(Registers.PC);
        Registers.PC++;
        var val = (ushort)(hi << 8 | lo);
        return Bus.Read(val);
    }

    private ushort a8()
    {
        var val = Bus.Read(Registers.PC);
        Registers.PC++;
        return (ushort)(val + 0xFF00);
    }

    private byte AC()
    {
        var val = (ushort)(0xFF00 + Registers.C);
        return Bus.Read(val);
    }

    private ushort SPE8()
    {
        var val = (sbyte)Bus.Read(Registers.PC);
        Registers.PC++;
        return (ushort)(Registers.SP + val);
    }
}