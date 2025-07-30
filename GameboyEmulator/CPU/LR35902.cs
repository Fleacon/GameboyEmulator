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
        if (cycles == 0)
        {
            opCode = Bus.Read(Registers.PC);
            Registers.PC++;
            
            var ins = instructions[opCode];
            cycles = ins.Cycles;
            fetched = instructions[opCode].AddressingMode();
            ins.Typ();
        }
    }

    private void initInstructionArray()
    {
        instructions[0x00] = new("NOP", NOP, IMP, 1);
        instructions[0x01] = new("LD BC, u16", LD_r16, IMM16, 3);
        instructions[0x02] = new("LD (BC), A", LD_r16mem, R16MEM, 2);
        instructions[0x03] = new("INC BC", INC, R16, 2);
        instructions[0x04] = new("INC B", INC, R8, 1);
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
    
    // Instructions

    private void NOP()
    {
        return;
    }

    private void LD_r8()
    {
        var dest = (byte)((opCode & 0b00111000) >> 3);
        Registers.SetR8(dest, (byte)fetched);
    }

    private void LD_r16()
    {
        var dest = (byte)((opCode & 0b00110000) >> 4);
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
        var code = (byte)((opCode & 0b00111000) >> 3);
        var regVal = Registers.GetR8(code);
        var inc = (byte)(regVal + 1);

        Registers.SetFlag(Registers.Flags.Z, inc == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x0F);

        Registers.SetR8(code, inc);
    }

    private void INC_HLmem()
    {
        var regVal = Bus.Read(Registers.HL);
        var inc = (byte)(regVal + 1);

        Registers.SetFlag(Registers.Flags.Z, inc == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x0F);

        Bus.Write8(Registers.HL, inc);
    }

    private void INC_R16()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        Registers.SetR16(code, (ushort)(fetched + 1));
    }

    private void DEC_R8()
    {
        var code = (byte)((opCode & 0b00111000) >> 3);
        var regVal = Registers.GetR8(code);
        var dec = (byte)(regVal - 1);

        Registers.SetFlag(Registers.Flags.Z, dec == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x00);

        Registers.SetR8(code, dec);
    }
    
    private void DEC_HLmem()
    {
        var regVal = Bus.Read(Registers.HL);
        var dec = (byte)(regVal - 1);

        Registers.SetFlag(Registers.Flags.Z, dec == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x00);

        Bus.Write8(Registers.HL, dec);
    }

    private void DEC_R16()
    {
        var code = (byte)((opCode & 0b00110000) >> 4);
        Registers.SetR16(code, (ushort)(fetched - 1));
    }

    private void RLCA()
    {
        
    }

    private void RRCA()
    {
        
    }

    private void RLA()
    {
        
    }

    private void RRA()
    {
        
    }

    private void DAA()
    {
        
    }

    private void CPL()
    {
        
    }

    private void SCF()
    {
        
    }

    private void CCF()
    {
        
    }

    private void JR()
    {
        
    }

    private void JR_cond()
    {
        
    }

    private void STOP()
    {
        
    }

    private void HALT()
    {
        
    }

    private void ADD()
    {
        
    }

    private void ADC()
    {
        
    }

    private void SUB()
    {
        
    }

    private void SBC()
    {
        
    }

    private void AND()
    {
        
    }

    private void XOR()
    {
        
    }

    private void OR()
    {
        
    }

    private void CP()
    {
        
    }

    private void RET()
    {
        
    }

    private void RET_cond()
    {
        
    }

    private void RETI()
    {
        
    }

    private void JP_cond()
    {
        
    }

    private void JP_imm16()
    {
        
    }

    private void JP_hl()
    {
        
    }

    private void CALL_cond()
    {
        
    }

    private void CALL()
    {
        
    }

    private void RST()
    {
        
    }

    private void POP()
    {
        
    }

    private void PUSH()
    {
        
    }

    private void LDH()
    {
        
    }

    private void DI()
    {
        
    }

    private void EI()
    {
        
    }
}