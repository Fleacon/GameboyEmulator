namespace GameboyEmulator.CPU;

public partial class LR35902
{
        private void NOP()
    {
        return;
    }

    private void LD_r8()
    {
        byte dest = util.ReadCode(opCode,Masks.R8Middle);
        Registers.SetR8(dest, (byte)fetched);
    }

    private void LD_r16()
    {
        byte dest = util.ReadCode(opCode,Masks.R16);
        Registers.SetR16(dest, fetched);
    }

    private void LD_r16mem()
    {
        Mmu.Write8(fetched, Registers.A);
    }

    private void LD_a()
    {
        Registers.A = Mmu.Read(fetched);
    }
    
    private void LD_imm16mem_SP()
    {
        Mmu.Write16(fetched, Registers.SP);
    }

    private void LD_imm16mem_a()
    {
        Mmu.Write8(fetched, Registers.A);
    }

    private void INC_R8()
    {
        byte code = util.ReadCode(opCode, Masks.R8Middle);
        byte regVal = Registers.GetR8(code);
        byte inc = (byte)(regVal + 1);

        Registers.SetFlag(Registers.Flags.Z, inc == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x0F);

        Registers.SetR8(code, inc);
    }

    private void INC_R16()
    {
        byte code = util.ReadCode(opCode, Masks.R16);
        Registers.SetR16(code, (ushort)(fetched + 1));
    }

    private void DEC_R8()
    {
        byte code = util.ReadCode(opCode, Masks.R8Middle);
        byte regVal = Registers.GetR8(code);
        byte dec = (byte)(regVal - 1);

        Registers.SetFlag(Registers.Flags.Z, dec == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x00);

        Registers.SetR8(code, dec);
    }
    
    private void DEC_HLmem()
    {
        byte regVal = Mmu.Read(Registers.HL);
        byte dec = (byte)(regVal - 1);

        Registers.SetFlag(Registers.Flags.Z, dec == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (regVal & 0x0F) == 0x00);

        Mmu.Write8(Registers.HL, dec);
    }

    private void DEC_R16()
    {
        byte code = util.ReadCode(opCode, Masks.R16);
        Registers.SetR16(code, (ushort)(fetched - 1));
    }

    private void RLCA()
    {
        byte regA = Registers.A;
        bool bit7 = (regA & 0b10000000) == 0x80;
        Registers.A = (byte)((regA << 1) | (bit7 ? 1 : 0));
        
        Registers.SetFlag(Registers.Flags.Z, false);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit7);
    }

    private void RRCA()
    {
        byte regA = Registers.A;
        bool bit0 = (regA & 1) == 1;
        Registers.A = (byte)((regA >> 1) | (bit0 ? 0x80 : 0));
        
        Registers.SetFlag(Registers.Flags.Z, false);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit0);
    }

    private void RLA()
    {
        byte regA = Registers.A;
        bool bit7 = (regA & 0b10000000) == 0x80;
        Registers.A = (byte)((regA << 1) | (Registers.GetFlag(Registers.Flags.C) ? 1 : 0));
        
        Registers.SetFlag(Registers.Flags.Z, false);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit7);
    }

    private void RRA()
    {
        byte regA = Registers.A;
        bool bit0 = (regA & 1) == 1;
        Registers.A = (byte)((regA >> 1) | (Registers.GetFlag(Registers.Flags.C) ? 0x80 : 0));
        
        Registers.SetFlag(Registers.Flags.Z, false);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit0);
    }

    private void DAA()
    {
        byte adjustment = 0;
        if (Registers.GetFlag(Registers.Flags.N)) // Subtraction
        {
            if (Registers.GetFlag(Registers.Flags.H))
                adjustment += 0x6;
            if (Registers.GetFlag(Registers.Flags.C))
                adjustment += 0x60;
            Registers.A -= adjustment;
        }
        else // Addition
        {
            if (Registers.GetFlag(Registers.Flags.H) || (Registers.A & 0xF) > 0x9)
                adjustment += 0x6;
            if (Registers.GetFlag(Registers.Flags.C) || Registers.A > 0x99)
            {
                adjustment += 0x60;
                Registers.SetFlag(Registers.Flags.C, true);
            }
            Registers.A += adjustment;
        }
        
        Registers.SetFlag(Registers.Flags.Z, Registers.A == 0);
        Registers.SetFlag(Registers.Flags.H, false);
    }

    private void CPL()
    {
        Registers.A = (byte)~Registers.A;
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, true);
    }

    private void SCF()
    {
        Registers.SetFlag(Registers.Flags.C, true);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
    }

    private void CCF()
    {
        Registers.SetFlag(Registers.Flags.C, !Registers.GetFlag(Registers.Flags.C));
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
    }

    private void JR()
    {
        sbyte signedOffset = (sbyte)fetched;
        Registers.PC = (ushort)(Registers.PC + signedOffset);
    }

    private void JR_cond()
    {
        byte code = util.ReadCode(opCode, Masks.Cond);
        bool cond = Registers.GetCond(code);

        if (cond)
        {
            sbyte signedOffset = (sbyte)fetched;
            Registers.PC = (ushort)(Registers.PC + signedOffset);
            withBranch = true;
        }
    }

    private void STOP()
    {
        isStopped = true;
        Mmu.Write8(0xFF04, 0); // Stop DIV Timer
    }

    private void HALT()
    {
        isHalted = true;
        
        byte IE = Mmu.Read(0xFFFF);
        byte IF = Mmu.Read(0xFF0F);
        if (!ime && (IE & IF & 0x1F) != 0) // Hardware Bug
        {
            isHalted = false;
            Registers.PC--;
        }
    }

    private void ADD()
    {
        byte regA = Registers.A;
        byte result = (byte)(regA + fetched);
        
        Registers.A = result;
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, ((regA & 0x0F) + (fetched & 0x0F)) > 0x0F);
        Registers.SetFlag(Registers.Flags.C, regA > result);
    }

    private void ADD_HL()
    {
        ushort regHL = Registers.HL;
        int result = regHL + fetched;
        
        Registers.HL = (ushort)result;
        
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, ((regHL & 0x0FFF) + (fetched & 0x0FFF)) > 0x0FFF);
        Registers.SetFlag(Registers.Flags.C, result > 0xFFFF);
    }

    private void ADD_SP()
    {
        ushort regSP = Registers.SP;
        sbyte signedOffset = (sbyte)fetched;
        ushort result = (ushort)(Registers.SP + (sbyte)fetched);
        
        Registers.SP = result;
        
        Registers.SetFlag(Registers.Flags.Z, false);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, ((regSP & 0x0F) + (signedOffset & 0x0F)) > 0x0F);
        Registers.SetFlag(Registers.Flags.C, ((regSP & 0xFF) + (signedOffset & 0xFF)) > 0xFF);
    }

    private void ADC()
    {
        byte regA = Registers.A;
        int cFlagValue = Registers.GetFlag(Registers.Flags.C) ? 1 : 0;
        int result = regA + fetched + cFlagValue;
        
        Registers.A = (byte)result;
        
        Registers.SetFlag(Registers.Flags.Z, Registers.A == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, ((regA & 0x0F) + (fetched & 0x0F) + cFlagValue) > 0x0F);
        Registers.SetFlag(Registers.Flags.C, result > 0xFF);
    }

    private void SUB()
    {
        byte regA = Registers.A;
        byte result = (byte)(regA - fetched);
        
        Registers.A = result;
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (regA & 0x0F) < (fetched & 0x0F));
        Registers.SetFlag(Registers.Flags.C, fetched > regA);
    }

    private void SBC()
    {
        byte regA = Registers.A;
        int cFlagValue = Registers.GetFlag(Registers.Flags.C) ? 1 : 0;
        byte result = (byte)(regA - fetched - cFlagValue);
        
        Registers.A = result;
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (regA & 0x0F) < (fetched & 0x0F) + cFlagValue);
        Registers.SetFlag(Registers.Flags.C, (fetched + cFlagValue) > regA);
    }

    private void AND()
    {
        int result = Registers.A & fetched;
        Registers.A = (byte)result;
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, true);
        Registers.SetFlag(Registers.Flags.C, false);
    }

    private void XOR()
    {
        int result = Registers.A ^ fetched;
        Registers.A = (byte)result;
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, false);
    }

    private void OR()
    {
        int result = Registers.A | fetched;
        Registers.A = (byte)result;
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, false);
    }

    private void CP()
    {
        Registers.SetFlag(Registers.Flags.Z, (Registers.A - fetched) == 0);
        Registers.SetFlag(Registers.Flags.N, true);
        Registers.SetFlag(Registers.Flags.H, (Registers.A & 0x0F) < (fetched & 0x0F));
        Registers.SetFlag(Registers.Flags.C, fetched > Registers.A);
    }

    private void RET()
    {
        byte lo = Mmu.Read(Registers.SP);
        Registers.SP++;
        byte hi = Mmu.Read(Registers.SP);
        Registers.SP++;

        var val = (ushort)((hi << 8) | lo);

        Registers.PC = val;
    }

    private void RET_cond()
    {
        byte code = util.ReadCode(opCode, Masks.Cond);
        bool cond = Registers.GetCond(code);

        if (cond)
        {
            byte lo = Mmu.Read(Registers.SP);
            Registers.SP++;
            byte hi = Mmu.Read(Registers.SP);
            Registers.SP++;

            ushort val = (ushort)((hi << 8) | lo);

            Registers.PC = val;

            withBranch = true;
        }
    }

    private void RETI()
    {
        byte lo = Mmu.Read(Registers.SP);
        Registers.SP++;
        byte hi = Mmu.Read(Registers.SP);
        Registers.SP++;

        ushort val = (ushort)((hi << 8) | lo);

        Registers.PC = val;
        ime = true;
    }

    private void JP_imm16()
    {
        Registers.PC = fetched;
    }
    
    private void JP_cond()
    {
        byte code = util.ReadCode(opCode, Masks.Cond);
        bool cond = Registers.GetCond(code);
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
        Mmu.Write16(Registers.SP, Registers.PC);
        Registers.PC = fetched;
    }

    private void CALL_cond()
    {
        byte code = util.ReadCode(opCode, Masks.Cond);
        bool cond = Registers.GetCond(code);
        if (cond)
        {
            Registers.SP -= 2;
            Mmu.Write16(Registers.SP, Registers.PC);
            Registers.PC = fetched;
        }
    }

    private void RST()
    {
        Registers.SP -= 2;
        Mmu.Write16(Registers.SP, Registers.PC);
        
        int targetAddress = util.ReadCode(opCode, Masks.Target) * 8;
        Registers.PC = (ushort)targetAddress;
    }

    private void POP()
    {
        byte code = util.ReadCode(opCode, Masks.R16);

        byte lo = Mmu.Read(Registers.SP);
        Registers.SP++;
        byte hi = Mmu.Read(Registers.SP);
        Registers.SP++;

        ushort val = (ushort)((hi << 8) | lo);
        
        Registers.SetR16Stk(code, val);
    }

    private void PUSH()
    {
        Registers.SP -= 2;
        Mmu.Write16(Registers.SP, fetched);
    }

    private void LDH_imm8mem()
    {
        ushort highAddress = (ushort)(fetched + 0xFF00);
        Mmu.Write8(highAddress, Registers.A);
    }

    private void LDH_cmem()
    {
        ushort highAddress = (ushort)(Registers.C + 0xFF00);
        Mmu.Write8(highAddress, Registers.A);
    }

    private void LDH_a_cmem()
    {
        ushort highAddress = (ushort)(Registers.C + 0xFF00);
        Registers.A = Mmu.Read(highAddress);
    }
    
    private void LDH_a_imm8mem()
    {
        ushort highAddress = (ushort)(fetched + 0xFF00);
        Registers.A = Mmu.Read(highAddress);
    }

    private void LD_HL_SP_imm8()
    {
        ushort regSP = Registers.SP;
        sbyte signed = (sbyte)fetched;
        ushort result = (ushort)(Registers.SP + signed);
        
        Registers.HL = result;
        
        Registers.SetFlag(Registers.Flags.Z, false);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, ((regSP & 0x0F) + (signed & 0x0F)) > 0x0F);
        Registers.SetFlag(Registers.Flags.C, ((regSP & 0xFF) + (signed & 0xFF)) > 0xFF);
    }

    private void LD_SP_HL()
    {
        Registers.SP = Registers.HL;
    }

    private void DI()
    {
        ime = false;
    }

    private void EI()
    {
        ime = true;
    }
    
    private void PREFIX_CB()
    {
        isCB = true;
    }
    
    // 0xCB prefix

    private void RLC()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit7 = (val & 0b10000000) == 0x80;

        byte result = (byte)((val << 1) | (bit7 ? 1 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit7);
        
        isCB = false;
    }

    private void RRC()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit0 = (val & 1) == 1;
        
        byte result = (byte)((val >> 1) | (bit0 ? 0x80 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit0);
        
        isCB = false;
    }

    private void RL()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit7 = (val & 0b10000000) == 0x80;
        
        byte result = (byte)((val << 1) | (Registers.GetFlag(Registers.Flags.C) ? 1 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit7);
        
        isCB = false;
    }

    private void RR()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit0 = (val & 1) == 1;

        byte result = (byte)((val >> 1) | (Registers.GetFlag(Registers.Flags.C) ? 0x80 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit0);
        
        isCB = false;
    }

    private void SLA()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit7 = (val & 0b10000000) == 0x80;

        byte result = (byte)(val << 1);
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit7);
        
        isCB = false;
    }

    private void SRA()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit0 = (val & 1) == 1;
        bool bit7 = (val & 0b10000000) == 0x80;

        byte result = (byte)((val >> 1) | (bit7 ? 0x80 : 0));
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit0);
        
        isCB = false;
    }

    private void SWAP()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);

        byte hi = (byte)((val & 0xF0) >> 4);
        byte lo = (byte)(val & 0x0F);
        byte result = (byte)((lo << 4) | hi);
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, false);
        
        isCB = false;
    }

    private void SRL()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        bool bit0 = (val & 1) == 1;

        byte result = (byte)(val >> 1);
        Registers.SetR8(code, result);
        
        Registers.SetFlag(Registers.Flags.Z, result == 0);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, false);
        Registers.SetFlag(Registers.Flags.C, bit0);
        
        isCB = false;
    }

    private void BIT()
    {
        byte index = util.ReadCode(opCode, Masks.U3);
        bool isSet = ((fetched >> index) & 0x01) == 1;
        
        Registers.SetFlag(Registers.Flags.Z, !isSet);
        Registers.SetFlag(Registers.Flags.N, false);
        Registers.SetFlag(Registers.Flags.H, true);
        
        isCB = false;
    }

    private void RES()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        byte index = util.ReadCode(opCode, Masks.U3);

        byte newVal = (byte)(~(0x01 << index) & val);
        
        Registers.SetR8(code, newVal);
        
        isCB = false;
    }

    private void SET()
    {
        byte code = util.ReadCode(opCode, Masks.R8Right);
        byte val = Registers.GetR8(code);
        byte index = util.ReadCode(opCode, Masks.U3);

        byte newVal = (byte)((0x01 << index) | val);
        
        Registers.SetR8(code, newVal);
        
        isCB = false;
    }
}