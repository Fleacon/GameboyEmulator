namespace GameboyEmulator.CPU;

public partial class LR35902 
{
    private ushort IMP()
    {
        return 0;
    }

    private ushort fetchR8()
    {
        // if Instruction has CB prefix or is LD r8, r8
        byte code = isCB || ((opCode & 0xC0) == 0x0) ? util.ReadCode(opCode, Masks.R8Middle) : util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        return val;
    }

    private ushort fetchR16MEM()
    {
        byte code = util.ReadCode(opCode, Masks.R16);
        return Registers.GetR16Mem(code);
    }
    
    private ushort fetchIMM8()
    {
        byte val = Mmu.Read(Registers.PC);
        Registers.PC++;
        return val;
    }

    private ushort fetchIMM16()
    {
        byte lo = Mmu.Read(Registers.PC);
        Registers.PC++;
        byte hi = Mmu.Read(Registers.PC);
        Registers.PC++;
        return (ushort)(hi << 8 | lo);
    }

    private ushort fetchR16()
    {
        byte code = util.ReadCode(opCode, Masks.R16);
        return Registers.GetR16(code);
    }

    private ushort fetchR16STK()
    {
        byte code = util.ReadCode(opCode, Masks.R16);
        return Registers.GetR16Stk(code);
    }
}