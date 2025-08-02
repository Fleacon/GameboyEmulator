using static GameboyEmulator.Registers;

namespace GameboyEmulator;
namespace GameboyEmulator.CPU;

public class LR35902
{
    public Registers Registers;

    public Bus Bus;
    
    private byte opCode;
    private ushort fetched;
    private bool isCB;
    private int cycles;
    private bool withBranch;
    private bool IME;
    
    private Instruction[] instructions;
    private Instruction[] cbInstructions;
    
    // set of Masks to read out the values stored in the OpCode
    public enum Masks : byte
    {
        cond = 0b00011000,
        r16 = 0b00110000,
        // Single Registers are either stored in the middle of the byte or the left of the byte 
        r8Right = 0b00000111,
        r8Middle = 0b01110000,
        target = 0b00111000,
        bitIndex = 0b00111000,
        u3 = 0b01110000
    }

    public LR35902(Bus bus)
    {
        this.Bus = bus;
        Registers = new(this);
        instructions = new Instruction[0xFF];
        cbInstructions = new Instruction[0xFF];
    }

    public void Clock()
    {
        if (cycles == 0)
        {
            opCode = Bus.Read(Registers.PC);
            Registers.PC++;
            
            var ins = instructions[opCode];
            
            fetched = instructions[opCode].AddressingMode();

            ins.Typ();
            
            cycles = withBranch ? ins.CyclesBranch : ins.Cycles;
            withBranch = false;
        }
    }

    private void initInstructionArray()
    {
        instructions[0x00] = new("NOP", NOP, IMP, 1);
        instructions[0x01] = new("LD BC, u16", LD_r16, fetchIMM16, 3);
        instructions[0x02] = new("LD (BC), A", LD_r16mem, fetchR16MEM, 2);
        instructions[0x03] = new("INC BC", INC_R8, fetchR16, 2);
        instructions[0x04] = new("INC B", INC_R8, fetchR8, 1);
    }
    
    // Addressing Modes
    private ushort IMP()
    {
        return 0;
    }

    private ushort fetchR8()
    {
        // if Instruction has CB prefix or is LD r8, r8
        var code = isCB || ((opCode & 0b01000000) == 0x40) ? util.ReadCode(opCode, Masks.r8Right) : util.ReadCode(opCode, Masks.r8Middle);
        var val = Registers.GetR8(code);
        return val;
    }

    private ushort fetchR16MEM()
    {
        var code = util.ReadCode(opCode, Masks.r16);
        return Registers.GetR16Mem(code);
    }
    
    private ushort fetchIMM8()
    {
        var val = Bus.Read(Registers.PC);
        Registers.PC++;
        return val;
    }

    private ushort fetchIMM16()
    {
        var hi = Bus.Read(Registers.PC);
        Registers.PC++;
        var lo = Bus.Read(Registers.PC);
        Registers.PC++;
        return (ushort)(hi << 8 | lo);
    }

    private ushort fetchR16()
    {
        var code = util.ReadCode(opCode, Masks.r16);
        return Registers.GetR16(code);
    }

    private ushort fetchR16STK()
    {
        var code = util.ReadCode(opCode, Masks.r16);
        return Registers.GetR16Stk(code);
    }
    
    // Instructions

    private void NOP()
    {
        return;
    }

    private void LD_r8()
    {
        var dest = util.ReadCode(opCode,Masks.r8Middle);
        Registers.SetR8(dest, (byte)fetched);
    }

    private void LD_r16()
    {
        var dest = util.ReadCode(opCode,Masks.r16);
        Registers.SetR16(dest, (byte)fetched);
    }

    private void LD_r16mem()
    {
        Bus.Write8(fetched, Registers.A);
    }

    private void LD_a()
    {
        Registers.A = Bus.Read(fetched);
    }
    
    private void LD_imm16mem_SP()
    {
        Bus.Write16(fetched, Registers.SP);
    }

    private void LD_imm16mem_a()
    {
        Bus.Write8(fetched, Registers.A);
    }

    private void INC_R8()
    {
        var code = util.ReadCode(opCode, Masks.r8Middle);
        var regVal = Registers.GetR8(code);
        var inc = (byte)(regVal + 1);

        Registers.SetFlag(Flags.Z, inc == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, (regVal & 0x0F) == 0x0F);

        Registers.SetR8(code, inc);
    }

    private void INC_R16()
    {
        var code = util.ReadCode(opCode, Masks.r16);
        Registers.SetR16(code, (ushort)(fetched + 1));
    }

    private void DEC_R8()
    {
        var code = util.ReadCode(opCode, Masks.r8Middle);
        var regVal = Registers.GetR8(code);
        var dec = (byte)(regVal - 1);

        Registers.SetFlag(Flags.Z, dec == 0);
        Registers.SetFlag(Flags.N, true);
        Registers.SetFlag(Flags.H, (regVal & 0x0F) == 0x00);

        Registers.SetR8(code, dec);
    }
    
    private void DEC_HLmem()
    {
        var regVal = Bus.Read(Registers.HL);
        var dec = (byte)(regVal - 1);

        Registers.SetFlag(Flags.Z, dec == 0);
        Registers.SetFlag(Flags.N, true);
        Registers.SetFlag(Flags.H, (regVal & 0x0F) == 0x00);

        Bus.Write8(Registers.HL, dec);
    }

    private void DEC_R16()
    {
        var code = util.ReadCode(opCode, Masks.r16);
        Registers.SetR16(code, (ushort)(fetched - 1));
    }

    private void RLCA()
    {
        var regA = Registers.A;
        var bit7 = (regA & 0b10000000) == 0x80;
        Registers.A = (byte)((regA << 1) | (bit7 ? 1 : 0));
        
        Registers.SetFlag(Flags.Z, false);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit7);
    }

    private void RRCA()
    {
        var regA = Registers.A;
        var bit0 = (regA & 1) == 1;
        Registers.A = (byte)((regA >> 1) | (bit0 ? 0x80 : 0));
        
        Registers.SetFlag(Flags.Z, false);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit0);
    }

    private void RLA()
    {
        var regA = Registers.A;
        var bit7 = (regA & 0b10000000) == 0x80;
        Registers.A = (byte)((regA << 1) | (Registers.GetFlag(Flags.C) ? 1 : 0));
        
        Registers.SetFlag(Flags.Z, false);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit7);
    }

    private void RRA()
    {
        var regA = Registers.A;
        var bit0 = (regA & 1) == 1;
        Registers.A = (byte)((regA >> 1) | (Registers.GetFlag(Flags.C) ? 0x80 : 0));
        
        Registers.SetFlag(Flags.Z, false);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit0);
    }

    private void DAA()
    {
        byte adjustment = 0;
        if (Registers.GetFlag(Flags.N)) // Subtraction
        {
            if (Registers.GetFlag(Flags.H))
                adjustment += 0x6;
            if (Registers.GetFlag(Flags.C))
                adjustment += 0x60;
            Registers.A -= adjustment;
        }
        else // Addition
        {
            if (Registers.GetFlag(Flags.H) || (Registers.A & 0xF) > 0x9)
                adjustment += 0x6;
            if (Registers.GetFlag(Flags.C) || Registers.A > 0x99)
            {
                adjustment += 0x60;
                Registers.SetFlag(Flags.C, true);
            }
            Registers.A += adjustment;
        }
        
        Registers.SetFlag(Flags.Z, Registers.A == 0);
        Registers.SetFlag(Flags.H, false);
    }

    private void CPL()
    {
        Registers.A = (byte)~Registers.A;
        Registers.SetFlag(Flags.N, true);
        Registers.SetFlag(Flags.H, true);
    }

    private void SCF()
    {
        Registers.SetFlag(Flags.C, true);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
    }

    private void CCF()
    {
        Registers.SetFlag(Flags.C, !Registers.GetFlag(Flags.C));
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
    }

    private void JR()
    {
        var signedOffset = (sbyte)fetched;
        Registers.PC = (ushort)(Registers.PC + signedOffset);
    }

    private void JR_cond()
    {
        var code = util.ReadCode(opCode, Masks.cond);
        var cond = Registers.GetCond(code);

        if (cond)
        {
            var signedOffset = (sbyte)fetched;
            Registers.PC = (ushort)(Registers.PC + signedOffset);
            withBranch = true;
        }
    }

    private void STOP()
    {
        // TODO: Implement STOP
    }

    private void HALT()
    {
        // TODO: Implement HALT
    }

    private void ADD()
    {
        var regA = Registers.A;
        var result = regA + fetched;
        
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, ((regA & 0x0F) + (fetched & 0x0F)) > 0x0F);
        Registers.SetFlag(Flags.C, result > 0xFF);
    }

    private void ADD_HL()
    {
        var regHL = Registers.HL;
        var result = regHL + fetched;
        
        Registers.HL = (ushort)result;
        
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, ((regHL & 0x0FFF) + (fetched & 0x0FFF)) > 0x0FFF);
        Registers.SetFlag(Flags.C, result > 0xFFFF);
    }

    private void ADD_SP()
    {
        var regSP = Registers.SP;
        var signedOffset = (sbyte)fetched;
        var result = Registers.SP + fetched;
        
        Registers.SP = (ushort)result;
        
        Registers.SetFlag(Flags.Z, false);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, ((regSP & 0x0F) + (signedOffset & 0x0F)) > 0x0F);
        Registers.SetFlag(Flags.C, ((regSP & 0xFF) + (signedOffset & 0xFF)) > 0xFF);
    }

    private void ADC()
    {
        var regA = Registers.A;
        var cFlagValue = Registers.GetFlag(Flags.C) ? 1 : 0;
        var result = regA + fetched + cFlagValue;
        
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, ((regA & 0x0F) + (fetched & 0x0F) + cFlagValue) > 0x0F);
        Registers.SetFlag(Flags.C, result > 0xFF);
    }

    private void SUB()
    {
        var regA = Registers.A;
        var result = regA - fetched;
        
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, true);
        Registers.SetFlag(Flags.H, (regA & 0x0F) < (fetched & 0x0F));
        Registers.SetFlag(Flags.C, fetched > regA);
    }

    private void SBC()
    {
        var regA = Registers.A;
        var cFlagValue = Registers.GetFlag(Flags.C) ? 1 : 0;
        var result = regA - fetched - cFlagValue;
        
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, true);
        Registers.SetFlag(Flags.H, (regA & 0x0F) < (fetched & 0x0F) + cFlagValue);
        Registers.SetFlag(Flags.C, (fetched + cFlagValue) > regA);
    }

    private void AND()
    {
        var result = Registers.A & fetched;
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, true);
        Registers.SetFlag(Flags.C, false);
    }

    private void XOR()
    {
        var result = Registers.A ^ fetched;
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, false);
    }

    private void OR()
    {
        var result = Registers.A | fetched;
        Registers.A = (byte)result;
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, false);
    }

    private void CP()
    {
        Registers.SetFlag(Flags.Z, (Registers.A - fetched) == 0);
        Registers.SetFlag(Flags.N, true);
        Registers.SetFlag(Flags.H, (Registers.A & 0x0F) < (fetched & 0x0F));
        Registers.SetFlag(Flags.C, fetched > Registers.A);
    }

    private void RET()
    {
        var lo = Bus.Read(Registers.SP);
        Registers.SP++;
        var hi = Bus.Read(Registers.SP);
        Registers.SP++;

        var val = (ushort)((hi << 8) | lo);

        Registers.PC = val;
    }

    private void RET_cond()
    {
        var code = util.ReadCode(opCode, Masks.cond);
        var cond = Registers.GetCond(code);

        if (cond)
        {
            var lo = Bus.Read(Registers.SP);
            Registers.SP++;
            var hi = Bus.Read(Registers.SP);
            Registers.SP++;

            var val = (ushort)((hi << 8) | lo);

            Registers.PC = val;

            withBranch = true;
        }
    }

    private void RETI()
    {
        var lo = Bus.Read(Registers.SP);
        Registers.SP++;
        var hi = Bus.Read(Registers.SP);
        Registers.SP++;

        var val = (ushort)((hi << 8) | lo);

        Registers.PC = val;
        IME = true;
    }

    private void JP_imm16()
    {
        Registers.PC = fetched;
    }
    
    private void JP_cond()
    {
        var code = util.ReadCode(opCode, Masks.cond);
        var cond = Registers.GetCond(code);
        if (cond)
        {
            Registers.PC = fetched;
            withBranch = true;
        }
    }

    private void JP_hl()
    {
        Registers.PC = Registers.HL;
    }
    
    private void CALL()
    {
        Registers.SP -= 2;
        Bus.Write16(Registers.SP, Registers.PC);
        Registers.PC = fetched;
    }

    private void CALL_cond()
    {
        var code = util.ReadCode(opCode, Masks.cond);
        var cond = Registers.GetCond(code);
        if (cond)
        {
            Registers.SP -= 2;
            Bus.Write16(Registers.SP, Registers.PC);
            Registers.PC = fetched;
        }
    }

    private void RST()
    {
        Registers.SP -= 2;
        Bus.Write16(Registers.SP, Registers.PC);
        
        var targetAddress = util.ReadCode(opCode, Masks.target) * 8;
        Registers.PC = (ushort)targetAddress;
    }

    private void POP()
    {
        var code = util.ReadCode(opCode, Masks.r16);

        var lo = Bus.Read(Registers.SP);
        Registers.SP++;
        var hi = Bus.Read(Registers.SP);
        Registers.SP++;

        var val = (ushort)((hi << 8) | lo);
        
        Registers.SetR16Stk(code, val);
        
        /*
        // Register AF
        if (code == 3) {
            Registers.SetFlag(Flags.Z, (lo & 0x80) == 0x80);
            Registers.SetFlag(Flags.N, (lo & 0x40) == 0x40);
            Registers.SetFlag(Flags.H, (lo & 0x20) == 0x20);
            Registers.SetFlag(Flags.C, (lo & 0x10) == 0x10);
        }
        */
    }

    private void PUSH()
    {
        var code = util.ReadCode(opCode, Masks.r16);
        Registers.SP -= 2;
        Bus.Write16(Registers.SP, Registers.GetR16Mem(code));
    }

    private void LDH_imm8mem()
    {
        var highAddress = (ushort)(fetched + 0xFF00);
        Bus.Write8(highAddress, Registers.A);
    }

    private void LDH_cmem()
    {
        var highAddress = (ushort)(Registers.C + 0xFF00);
        Bus.Write8(highAddress, Registers.A);
    }

    private void LDH_a_cmem()
    {
        var highAddress = (ushort)(Registers.C + 0xFF00);
        Registers.A = Bus.Read(highAddress);
    }
    
    private void LDH_a_imm8mem()
    {
        var highAddress = (ushort)(fetched + 0xFF00);
        Registers.A = Bus.Read(highAddress);
    }

    private void DI()
    {
        IME = false;
    }

    private void EI()
    {
        IME = true;
    }
    
    // 0xCB prefix

    private void RLC()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit7 = (val & 0b10000000) == 0x80;

        var result = (byte)((val << 1) | (bit7 ? 1 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit7);
    }

    private void RRC()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit0 = (val & 1) == 1;
        
        var result = (byte)((val >> 1) | (bit0 ? 0x80 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit0);
    }

    private void RL()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit7 = (val & 0b10000000) == 0x80;
        
        var result = (byte)((val << 1) | (Registers.GetFlag(Flags.C) ? 1 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit7);
    }

    private void RR()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit0 = (val & 1) == 1;

        var result = (byte)((val >> 1) | (Registers.GetFlag(Flags.C) ? 0x80 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit0);
    }

    private void SLA()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit7 = (val & 0b10000000) == 0x80;

        var result = (byte)(val << 1);
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit7);
    }

    private void SRA()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit0 = (val & 1) == 1;
        var bit7 = (val & 0b10000000) == 0x80;

        var result = (byte)((val >> 1) | (bit7 ? 0x80 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit0);
    }

    private void SWAP()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);

        var hi = (byte)((val & 0xF0) >> 4);
        var lo = (byte)(val & 0x0F);
        var result = (byte)((lo << 4) | hi);
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, false);
    }

    private void SRL()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var bit0 = (val & 1) == 1;

        var result = (byte)(val >> 1);
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Flags.Z, result == 0);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, false);
        Registers.SetFlag(Flags.C, bit0);
    }

    private void BIT()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var index = util.ReadCode(opCode, Masks.u3);
        
        var isSet = ((val >> index) & 0x01) == 1;
        
        Registers.SetFlag(Flags.Z, !isSet);
        Registers.SetFlag(Flags.N, false);
        Registers.SetFlag(Flags.H, true);
    }

    private void RES()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var index = util.ReadCode(opCode, Masks.u3);

        var newVal = (byte)(~(0x01 << index) & val);
        
        Registers.SetR8(code, newVal);
    }

    private void SET()
    {
        var code = util.ReadCode(opCode, Masks.r8Right);
        var val = Registers.GetR8(code);
        var index = util.ReadCode(opCode, Masks.u3);

        var newVal = (byte)((0x01 << index) | val);
        
        Registers.SetR8(code, newVal);
    }
}