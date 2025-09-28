using System.Diagnostics.CodeAnalysis;
using static GameboyEmulator.Registers;

namespace GameboyEmulator.CPU;

public partial class LR35902
{
    public Registers Registers;

    public MMU Mmu;
    
    private byte opCode;
    private ushort fetched;
    private bool isCB;
    private bool isHalted;
    private bool isStopped;
    private int cycles;
    private bool withBranch;
    private bool ime;
    private ushort prevIF;
    
    private readonly Instruction[] instructions;
    private readonly Instruction[] cbInstructions;

    private Logger logger = new("export");
    
    // set of Masks to read out the values stored in the OpCode
    public enum Masks : byte
    {
        Cond = 0b00011000,
        R16 = 0b00110000,
        // Single Registers are either stored in the middle of the byte or the left of the byte 
        R8Right = 0b00000111,
        R8Middle = 0b00111000,
        Target = 0b00111000,
        U3 = 0b00111000
    }
    
    public LR35902(MMU mmu)
    {
        Mmu = mmu;
        Registers = new(this);
        instructions = new Instruction[0x100];
        initInstructionArray();
        cbInstructions = new Instruction[0x100];
        initCBInstructionArray();
        
        logger.RegisterShutdownHandler();
    }

    public int Execute()
    {
        if (checkInterrupt(out Interrupts interrupt))
        {
            handleInterrupt(interrupt);
            return cycles;
        }
        
        if (!(isHalted || isStopped))
        {   
            opCode = Mmu.Read(Registers.PC);
            Registers.PC++;

            Instruction ins;
            if (opCode == 0xCB)
            {
                opCode = Mmu.Read(Registers.PC);
                Registers.PC++;
                ins = cbInstructions[opCode];
            }
            else
            {
                ins = instructions[opCode];
            }
            fetched = ins.AddressingMode();
            ins.Typ();
            cycles = withBranch ? ins.CyclesBranch : ins.Cycles;
            withBranch = false;
            //createLogFile();
        }
        else
        {
            cycles = 2;
        }
        
        return cycles;
    }

    private void createLogFile()
    {
        string log = $"A:{Registers.A:X2} F:{Registers.F:X2} B:{Registers.B:X2} C:{Registers.C:X2} D:{Registers.D:X2} E:{Registers.E:X2} H:{Registers.H:X2} L:{Registers.L:X2} SP:{Registers.SP:X4} PC:{Registers.PC:X4} PCMEM:{Mmu.Read(Registers.PC):X2},{Mmu.Read((ushort)(Registers.PC + 1)):X2},{Mmu.Read((ushort)(Registers.PC + 2)):X2},{Mmu.Read((ushort)(Registers.PC + 3)):X2}";
        logger.Log(log);
    }

    private bool checkInterrupt(out Interrupts interrupt)
    {
        byte IE = Mmu.Read(0xFFFF);
        byte IF = Mmu.Read(0xFF0F);
        byte interrupts = (byte)((IE & 0x1F) & (IF & 0x1F));
        
        if (interrupts != 0 && ime)
        {
            byte mask = 0x01;
            byte temp = interrupts;
            int i;
            for (i = 0; i < 5; i++)
            {
                if ((temp & 0x01) == 0x01)
                    break;
                mask = (byte)(mask << 1);
                temp = (byte)(temp >> 1);
            }
            ime = false;
            Mmu.Write8(0xFF0F, (byte)(interrupts & ~mask));
            interrupt = (Interrupts)i;
            return true;
        }
        
        if (isHalted && ((IF & 0x1F) != (prevIF & 0x1F)))
        {
            isHalted = false;
        }

        prevIF = IF;
        interrupt = Interrupts.NONE;
        return false;
    }

    private void handleInterrupt(Interrupts interrupt)
    {
        isHalted = false;
        
        if (interrupt == Interrupts.JOYPAD) isStopped = false;
        
        Registers.SP -= 2;
        Mmu.Write16(Registers.SP, Registers.PC);

        Registers.PC = interrupt switch
        {
            Interrupts.VBLANK => 0x40,
            Interrupts.LCD => 0x48,
            Interrupts.TIMER => 0x50,
            Interrupts.SERIAL => 0x58,
            Interrupts.JOYPAD => 0x60,
            _ => throw new Exception("INVALID INTERRUPT"),
        };
        cycles = 5;
    }

    private void initInstructionArray()
    {
        instructions[0x00] = new("NOP", NOP, IMP, 1);
        instructions[0x01] = new("LD BC, u16", LD_r16, fetchIMM16, 3);
        instructions[0x02] = new("LD (BC), A", LD_r16mem, fetchR16MEM, 2);
        instructions[0x03] = new("INC BC", INC_R16, fetchR16, 2);
        instructions[0x04] = new("INC B", INC_R8, fetchR8, 1);
        instructions[0x05] = new("DEC B", DEC_R8, fetchR8, 1);
        instructions[0x06] = new("LD B, u8", LD_r8, fetchIMM8, 2);
        instructions[0x07] = new("RLCA", RLCA, IMP, 1);
        instructions[0x08] = new("LD (u16), SP", LD_imm16mem_SP, fetchIMM16, 5);
        instructions[0x09] = new("ADD HL, BC", ADD_HL, fetchR16, 2);
        instructions[0x0A] = new("LD A, (BC)", LD_a, fetchR16MEM, 2);
        instructions[0x0B] = new("DEC BC", DEC_R16, fetchR16, 2);
        instructions[0x0C] = new("INC C", INC_R8, fetchR8, 1);
        instructions[0x0D] = new("DEC C", DEC_R8, fetchR8, 1);
        instructions[0x0E] = new("LD C, u8", LD_r8, fetchIMM8, 2);
        instructions[0x0F] = new("RRCA", RRCA, IMP, 1);

        // 0x10-0x1F
        instructions[0x10] = new("STOP", STOP, fetchIMM8, 1);
        instructions[0x11] = new("LD DE, u16", LD_r16, fetchIMM16, 3);
        instructions[0x12] = new("LD (DE), A", LD_r16mem, fetchR16MEM, 2);
        instructions[0x13] = new("INC DE", INC_R16, fetchR16, 2);
        instructions[0x14] = new("INC D", INC_R8, fetchR8, 1);
        instructions[0x15] = new("DEC D", DEC_R8, fetchR8, 1);
        instructions[0x16] = new("LD D, u8", LD_r8, fetchIMM8, 2);
        instructions[0x17] = new("RLA", RLA, IMP, 1);
        instructions[0x18] = new("JR i8", JR, fetchIMM8, 3);
        instructions[0x19] = new("ADD HL, DE", ADD_HL, fetchR16, 2);
        instructions[0x1A] = new("LD A, (DE)", LD_a, fetchR16MEM, 2);
        instructions[0x1B] = new("DEC DE", DEC_R16, fetchR16, 2);
        instructions[0x1C] = new("INC E", INC_R8, fetchR8, 1);
        instructions[0x1D] = new("DEC E", DEC_R8, fetchR8, 1);
        instructions[0x1E] = new("LD E, u8", LD_r8, fetchIMM8, 2);
        instructions[0x1F] = new("RRA", RRA, IMP, 1);

        // 0x20-0x2F
        instructions[0x20] = new("JR NZ, i8", JR_cond, fetchIMM8, 2, 3);
        instructions[0x21] = new("LD HL, u16", LD_r16, fetchIMM16, 3);
        instructions[0x22] = new("LD (HL+), A", LD_r16mem, fetchR16MEM, 2);
        instructions[0x23] = new("INC HL", INC_R16, fetchR16, 2);
        instructions[0x24] = new("INC H", INC_R8, fetchR8, 1);
        instructions[0x25] = new("DEC H", DEC_R8, fetchR8, 1);
        instructions[0x26] = new("LD H, u8", LD_r8, fetchIMM8, 2);
        instructions[0x27] = new("DAA", DAA, IMP, 1);
        instructions[0x28] = new("JR Z, i8", JR_cond, fetchIMM8, 2, 3);
        instructions[0x29] = new("ADD HL, HL", ADD_HL, fetchR16, 2);
        instructions[0x2A] = new("LD A, (HL+)", LD_a, fetchR16MEM, 2);
        instructions[0x2B] = new("DEC HL", DEC_R16, fetchR16, 2);
        instructions[0x2C] = new("INC L", INC_R8, fetchR8, 1);
        instructions[0x2D] = new("DEC L", DEC_R8, fetchR8, 1);
        instructions[0x2E] = new("LD L, u8", LD_r8, fetchIMM8, 2);
        instructions[0x2F] = new("CPL", CPL, IMP, 1);

        // 0x30-0x3F
        instructions[0x30] = new("JR NC, i8", JR_cond, fetchIMM8, 2, 3);
        instructions[0x31] = new("LD SP, u16", LD_r16, fetchIMM16, 3);
        instructions[0x32] = new("LD (HL-), A", LD_r16mem, fetchR16MEM, 2);
        instructions[0x33] = new("INC SP", INC_R16, fetchR16, 2);
        instructions[0x34] = new("INC (HL)", INC_R8, fetchR8, 3);
        instructions[0x35] = new("DEC (HL)", DEC_HLmem, IMP, 3);
        instructions[0x36] = new("LD (HL), u8", LD_r8, fetchIMM8, 3);
        instructions[0x37] = new("SCF", SCF, IMP, 1);
        instructions[0x38] = new("JR C, i8", JR_cond, fetchIMM8, 2, 3);
        instructions[0x39] = new("ADD HL, SP", ADD_HL, fetchR16, 2);
        instructions[0x3A] = new("LD A, (HL-)", LD_a, fetchR16MEM, 2);
        instructions[0x3B] = new("DEC SP", DEC_R16, fetchR16, 2);
        instructions[0x3C] = new("INC A", INC_R8, fetchR8, 1);
        instructions[0x3D] = new("DEC A", DEC_R8, fetchR8, 1);
        instructions[0x3E] = new("LD A, u8", LD_r8, fetchIMM8, 2);
        instructions[0x3F] = new("CCF", CCF, IMP, 1);

        // 0x40-0x4F (LD r8, r8 instructions)
        instructions[0x40] = new("LD B, B", LD_r8, fetchR8, 1);
        instructions[0x41] = new("LD B, C", LD_r8, fetchR8, 1);
        instructions[0x42] = new("LD B, D", LD_r8, fetchR8, 1);
        instructions[0x43] = new("LD B, E", LD_r8, fetchR8, 1);
        instructions[0x44] = new("LD B, H", LD_r8, fetchR8, 1);
        instructions[0x45] = new("LD B, L", LD_r8, fetchR8, 1);
        instructions[0x46] = new("LD B, (HL)", LD_r8, fetchR8, 2);
        instructions[0x47] = new("LD B, A", LD_r8, fetchR8, 1);
        instructions[0x48] = new("LD C, B", LD_r8, fetchR8, 1);
        instructions[0x49] = new("LD C, C", LD_r8, fetchR8, 1);
        instructions[0x4A] = new("LD C, D", LD_r8, fetchR8, 1);
        instructions[0x4B] = new("LD C, E", LD_r8, fetchR8, 1);
        instructions[0x4C] = new("LD C, H", LD_r8, fetchR8, 1);
        instructions[0x4D] = new("LD C, L", LD_r8, fetchR8, 1);
        instructions[0x4E] = new("LD C, (HL)", LD_r8, fetchR8, 2);
        instructions[0x4F] = new("LD C, A", LD_r8, fetchR8, 1);

        // 0x50-0x5F
        instructions[0x50] = new("LD D, B", LD_r8, fetchR8, 1);
        instructions[0x51] = new("LD D, C", LD_r8, fetchR8, 1);
        instructions[0x52] = new("LD D, D", LD_r8, fetchR8, 1);
        instructions[0x53] = new("LD D, E", LD_r8, fetchR8, 1);
        instructions[0x54] = new("LD D, H", LD_r8, fetchR8, 1);
        instructions[0x55] = new("LD D, L", LD_r8, fetchR8, 1);
        instructions[0x56] = new("LD D, (HL)", LD_r8, fetchR8, 2);
        instructions[0x57] = new("LD D, A", LD_r8, fetchR8, 1);
        instructions[0x58] = new("LD E, B", LD_r8, fetchR8, 1);
        instructions[0x59] = new("LD E, C", LD_r8, fetchR8, 1);
        instructions[0x5A] = new("LD E, D", LD_r8, fetchR8, 1);
        instructions[0x5B] = new("LD E, E", LD_r8, fetchR8, 1);
        instructions[0x5C] = new("LD E, H", LD_r8, fetchR8, 1);
        instructions[0x5D] = new("LD E, L", LD_r8, fetchR8, 1);
        instructions[0x5E] = new("LD E, (HL)", LD_r8, fetchR8, 2);
        instructions[0x5F] = new("LD E, A", LD_r8, fetchR8, 1);

        // 0x60-0x6F
        instructions[0x60] = new("LD H, B", LD_r8, fetchR8, 1);
        instructions[0x61] = new("LD H, C", LD_r8, fetchR8, 1);
        instructions[0x62] = new("LD H, D", LD_r8, fetchR8, 1);
        instructions[0x63] = new("LD H, E", LD_r8, fetchR8, 1);
        instructions[0x64] = new("LD H, H", LD_r8, fetchR8, 1);
        instructions[0x65] = new("LD H, L", LD_r8, fetchR8, 1);
        instructions[0x66] = new("LD H, (HL)", LD_r8, fetchR8, 2);
        instructions[0x67] = new("LD H, A", LD_r8, fetchR8, 1);
        instructions[0x68] = new("LD L, B", LD_r8, fetchR8, 1);
        instructions[0x69] = new("LD L, C", LD_r8, fetchR8, 1);
        instructions[0x6A] = new("LD L, D", LD_r8, fetchR8, 1);
        instructions[0x6B] = new("LD L, E", LD_r8, fetchR8, 1);
        instructions[0x6C] = new("LD L, H", LD_r8, fetchR8, 1);
        instructions[0x6D] = new("LD L, L", LD_r8, fetchR8, 1);
        instructions[0x6E] = new("LD L, (HL)", LD_r8, fetchR8, 2);
        instructions[0x6F] = new("LD L, A", LD_r8, fetchR8, 1);

        // 0x70-0x7F
        instructions[0x70] = new("LD (HL), B", LD_r8, fetchR8, 2);
        instructions[0x71] = new("LD (HL), C", LD_r8, fetchR8, 2);
        instructions[0x72] = new("LD (HL), D", LD_r8, fetchR8, 2);
        instructions[0x73] = new("LD (HL), E", LD_r8, fetchR8, 2);
        instructions[0x74] = new("LD (HL), H", LD_r8, fetchR8, 2);
        instructions[0x75] = new("LD (HL), L", LD_r8, fetchR8, 2);
        instructions[0x76] = new("HALT", HALT, IMP, 1);
        instructions[0x77] = new("LD (HL), A", LD_r8, fetchR8, 2);
        instructions[0x78] = new("LD A, B", LD_r8, fetchR8, 1);
        instructions[0x79] = new("LD A, C", LD_r8, fetchR8, 1);
        instructions[0x7A] = new("LD A, D", LD_r8, fetchR8, 1);
        instructions[0x7B] = new("LD A, E", LD_r8, fetchR8, 1);
        instructions[0x7C] = new("LD A, H", LD_r8, fetchR8, 1);
        instructions[0x7D] = new("LD A, L", LD_r8, fetchR8, 1);
        instructions[0x7E] = new("LD A, (HL)", LD_r8, fetchR8, 2);
        instructions[0x7F] = new("LD A, A", LD_r8, fetchR8, 1);

        // 0x80-0x8F (ADD, ADC)
        instructions[0x80] = new("ADD A, B", ADD, fetchR8, 1);
        instructions[0x81] = new("ADD A, C", ADD, fetchR8, 1);
        instructions[0x82] = new("ADD A, D", ADD, fetchR8, 1);
        instructions[0x83] = new("ADD A, E", ADD, fetchR8, 1);
        instructions[0x84] = new("ADD A, H", ADD, fetchR8, 1);
        instructions[0x85] = new("ADD A, L", ADD, fetchR8, 1);
        instructions[0x86] = new("ADD A, (HL)", ADD, fetchR8, 2);
        instructions[0x87] = new("ADD A, A", ADD, fetchR8, 1);
        instructions[0x88] = new("ADC A, B", ADC, fetchR8, 1);
        instructions[0x89] = new("ADC A, C", ADC, fetchR8, 1);
        instructions[0x8A] = new("ADC A, D", ADC, fetchR8, 1);
        instructions[0x8B] = new("ADC A, E", ADC, fetchR8, 1);
        instructions[0x8C] = new("ADC A, H", ADC, fetchR8, 1);
        instructions[0x8D] = new("ADC A, L", ADC, fetchR8, 1);
        instructions[0x8E] = new("ADC A, (HL)", ADC, fetchR8, 2);
        instructions[0x8F] = new("ADC A, A", ADC, fetchR8, 1);

        // 0x90-0x9F (SUB, SBC)
        instructions[0x90] = new("SUB A, B", SUB, fetchR8, 1);
        instructions[0x91] = new("SUB A, C", SUB, fetchR8, 1);
        instructions[0x92] = new("SUB A, D", SUB, fetchR8, 1);
        instructions[0x93] = new("SUB A, E", SUB, fetchR8, 1);
        instructions[0x94] = new("SUB A, H", SUB, fetchR8, 1);
        instructions[0x95] = new("SUB A, L", SUB, fetchR8, 1);
        instructions[0x96] = new("SUB A, (HL)", SUB, fetchR8, 2);
        instructions[0x97] = new("SUB A, A", SUB, fetchR8, 1);
        instructions[0x98] = new("SBC A, B", SBC, fetchR8, 1);
        instructions[0x99] = new("SBC A, C", SBC, fetchR8, 1);
        instructions[0x9A] = new("SBC A, D", SBC, fetchR8, 1);
        instructions[0x9B] = new("SBC A, E", SBC, fetchR8, 1);
        instructions[0x9C] = new("SBC A, H", SBC, fetchR8, 1);
        instructions[0x9D] = new("SBC A, L", SBC, fetchR8, 1);
        instructions[0x9E] = new("SBC A, (HL)", SBC, fetchR8, 2);
        instructions[0x9F] = new("SBC A, A", SBC, fetchR8, 1);

        // 0xA0-0xAF (AND, XOR)
        instructions[0xA0] = new("AND A, B", AND, fetchR8, 1);
        instructions[0xA1] = new("AND A, C", AND, fetchR8, 1);
        instructions[0xA2] = new("AND A, D", AND, fetchR8, 1);
        instructions[0xA3] = new("AND A, E", AND, fetchR8, 1);
        instructions[0xA4] = new("AND A, H", AND, fetchR8, 1);
        instructions[0xA5] = new("AND A, L", AND, fetchR8, 1);
        instructions[0xA6] = new("AND A, (HL)", AND, fetchR8, 2);
        instructions[0xA7] = new("AND A, A", AND, fetchR8, 1);
        instructions[0xA8] = new("XOR A, B", XOR, fetchR8, 1);
        instructions[0xA9] = new("XOR A, C", XOR, fetchR8, 1);
        instructions[0xAA] = new("XOR A, D", XOR, fetchR8, 1);
        instructions[0xAB] = new("XOR A, E", XOR, fetchR8, 1);
        instructions[0xAC] = new("XOR A, H", XOR, fetchR8, 1);
        instructions[0xAD] = new("XOR A, L", XOR, fetchR8, 1);
        instructions[0xAE] = new("XOR A, (HL)", XOR, fetchR8, 2);
        instructions[0xAF] = new("XOR A, A", XOR, fetchR8, 1);

        // 0xB0-0xBF (OR, CP)
        instructions[0xB0] = new("OR A, B", OR, fetchR8, 1);
        instructions[0xB1] = new("OR A, C", OR, fetchR8, 1);
        instructions[0xB2] = new("OR A, D", OR, fetchR8, 1);
        instructions[0xB3] = new("OR A, E", OR, fetchR8, 1);
        instructions[0xB4] = new("OR A, H", OR, fetchR8, 1);
        instructions[0xB5] = new("OR A, L", OR, fetchR8, 1);
        instructions[0xB6] = new("OR A, (HL)", OR, fetchR8, 2);
        instructions[0xB7] = new("OR A, A", OR, fetchR8, 1);
        instructions[0xB8] = new("CP A, B", CP, fetchR8, 1);
        instructions[0xB9] = new("CP A, C", CP, fetchR8, 1);
        instructions[0xBA] = new("CP A, D", CP, fetchR8, 1);
        instructions[0xBB] = new("CP A, E", CP, fetchR8, 1);
        instructions[0xBC] = new("CP A, H", CP, fetchR8, 1);
        instructions[0xBD] = new("CP A, L", CP, fetchR8, 1);
        instructions[0xBE] = new("CP A, (HL)", CP, fetchR8, 2);
        instructions[0xBF] = new("CP A, A", CP, fetchR8, 1);

        // 0xC0-0xCF
        instructions[0xC0] = new("RET NZ", RET_cond, IMP, 2, 5);
        instructions[0xC1] = new("POP BC", POP, IMP, 3);
        instructions[0xC2] = new("JP NZ, u16", JP_cond, fetchIMM16, 3, 4);
        instructions[0xC3] = new("JP u16", JP_imm16, fetchIMM16, 4);
        instructions[0xC4] = new("CALL NZ, u16", CALL_cond, fetchIMM16, 3, 6);
        instructions[0xC5] = new("PUSH BC", PUSH, fetchR16STK, 4);
        instructions[0xC6] = new("ADD A, u8", ADD, fetchIMM8, 2);
        instructions[0xC7] = new("RST 00h", RST, IMP, 4);
        instructions[0xC8] = new("RET Z", RET_cond, IMP, 2, 5);
        instructions[0xC9] = new("RET", RET, IMP, 4);
        instructions[0xCA] = new("JP Z, u16", JP_cond, fetchIMM16, 3, 4);
        instructions[0xCB] = new("PREFIX CB", PREFIX_CB, IMP, 1);
        instructions[0xCC] = new("CALL Z, u16", CALL_cond, fetchIMM16, 3, 6);
        instructions[0xCD] = new("CALL u16", CALL, fetchIMM16, 6);
        instructions[0xCE] = new("ADC A, u8", ADC, fetchIMM8, 2);
        instructions[0xCF] = new("RST 08h", RST, IMP, 4);

        // 0xD0-0xDF
        instructions[0xD0] = new("RET NC", RET_cond, IMP, 2, 5);
        instructions[0xD1] = new("POP DE", POP, IMP, 3);
        instructions[0xD2] = new("JP NC, u16", JP_cond, fetchIMM16, 3, 4);
        instructions[0xD3] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xD4] = new("CALL NC, u16", CALL_cond, fetchIMM16, 3, 6);
        instructions[0xD5] = new("PUSH DE", PUSH, fetchR16STK, 4);
        instructions[0xD6] = new("SUB A, u8", SUB, fetchIMM8, 2);
        instructions[0xD7] = new("RST 10h", RST, IMP, 4);
        instructions[0xD8] = new("RET C", RET_cond, IMP, 2, 5);
        instructions[0xD9] = new("RETI", RETI, IMP, 4);
        instructions[0xDA] = new("JP C, u16", JP_cond, fetchIMM16, 3, 4);
        instructions[0xDB] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xDC] = new("CALL C, u16", CALL_cond, fetchIMM16, 3, 6);
        instructions[0xDD] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xDE] = new("SBC A, u8", SBC, fetchIMM8, 2);
        instructions[0xDF] = new("RST 18h", RST, IMP, 4);

        // 0xE0-0xEF
        instructions[0xE0] = new("LDH (u8), A", LDH_imm8mem, fetchIMM8, 3);
        instructions[0xE1] = new("POP HL", POP, IMP, 3);
        instructions[0xE2] = new("LDH (C), A", LDH_cmem, IMP, 2);
        instructions[0xE3] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xE4] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xE5] = new("PUSH HL", PUSH, fetchR16STK, 4);
        instructions[0xE6] = new("AND A, u8", AND, fetchIMM8, 2);
        instructions[0xE7] = new("RST 20h", RST, IMP, 4);
        instructions[0xE8] = new("ADD SP, i8", ADD_SP, fetchIMM8, 4);
        instructions[0xE9] = new("JP HL", JP_hl, IMP, 1);
        instructions[0xEA] = new("LD (u16), A", LD_imm16mem_a, fetchIMM16, 4);
        instructions[0xEB] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xEC] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xED] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xEE] = new("XOR A, u8", XOR, fetchIMM8, 2);
        instructions[0xEF] = new("RST 28h", RST, IMP, 4);

        // 0xF0-0xFF
        instructions[0xF0] = new("LDH A, (u8)", LDH_a_imm8mem, fetchIMM8, 3);
        instructions[0xF1] = new("POP AF", POP, IMP, 3);
        instructions[0xF2] = new("LDH A, (C)", LDH_a_cmem, IMP, 2);
        instructions[0xF3] = new("DI", DI, IMP, 1);
        instructions[0xF4] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xF5] = new("PUSH AF", PUSH, fetchR16STK, 4);
        instructions[0xF6] = new("OR A, u8", OR, fetchIMM8, 2);
        instructions[0xF7] = new("RST 30h", RST, IMP, 4);
        instructions[0xF8] = new("LD HL, SP+i8", LD_HL_SP_imm8, fetchIMM8, 3);
        instructions[0xF9] = new("LD SP, HL", LD_SP_HL, IMP, 2);
        instructions[0xFA] = new("LD A, (u16)", LD_a, fetchIMM16, 4);
        instructions[0xFB] = new("EI", EI, IMP, 1);
        instructions[0xFC] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xFD] = new("INVALID", NOP, IMP, 1); // Invalid opcode
        instructions[0xFE] = new("CP A, u8", CP, fetchIMM8, 2);
        instructions[0xFF] = new("RST 38h", RST, IMP, 4);
    }
    
    private void initCBInstructionArray()
    {
        // 0x00-0x0F: RLC r8
        cbInstructions[0x00] = new("RLC B", RLC, fetchR8, 2);
        cbInstructions[0x01] = new("RLC C", RLC, fetchR8, 2);
        cbInstructions[0x02] = new("RLC D", RLC, fetchR8, 2);
        cbInstructions[0x03] = new("RLC E", RLC, fetchR8, 2);
        cbInstructions[0x04] = new("RLC H", RLC, fetchR8, 2);
        cbInstructions[0x05] = new("RLC L", RLC, fetchR8, 2);
        cbInstructions[0x06] = new("RLC (HL)", RLC, fetchR8, 4);
        cbInstructions[0x07] = new("RLC A", RLC, fetchR8, 2);
        cbInstructions[0x08] = new("RRC B", RRC, fetchR8, 2);
        cbInstructions[0x09] = new("RRC C", RRC, fetchR8, 2);
        cbInstructions[0x0A] = new("RRC D", RRC, fetchR8, 2);
        cbInstructions[0x0B] = new("RRC E", RRC, fetchR8, 2);
        cbInstructions[0x0C] = new("RRC H", RRC, fetchR8, 2);
        cbInstructions[0x0D] = new("RRC L", RRC, fetchR8, 2);
        cbInstructions[0x0E] = new("RRC (HL)", RRC, fetchR8, 4);
        cbInstructions[0x0F] = new("RRC A", RRC, fetchR8, 2);

        // 0x10-0x1F: RL r8
        cbInstructions[0x10] = new("RL B", RL, fetchR8, 2);
        cbInstructions[0x11] = new("RL C", RL, fetchR8, 2);
        cbInstructions[0x12] = new("RL D", RL, fetchR8, 2);
        cbInstructions[0x13] = new("RL E", RL, fetchR8, 2);
        cbInstructions[0x14] = new("RL H", RL, fetchR8, 2);
        cbInstructions[0x15] = new("RL L", RL, fetchR8, 2);
        cbInstructions[0x16] = new("RL (HL)", RL, fetchR8, 4);
        cbInstructions[0x17] = new("RL A", RL, fetchR8, 2);
        cbInstructions[0x18] = new("RR B", RR, fetchR8, 2);
        cbInstructions[0x19] = new("RR C", RR, fetchR8, 2);
        cbInstructions[0x1A] = new("RR D", RR, fetchR8, 2);
        cbInstructions[0x1B] = new("RR E", RR, fetchR8, 2);
        cbInstructions[0x1C] = new("RR H", RR, fetchR8, 2);
        cbInstructions[0x1D] = new("RR L", RR, fetchR8, 2);
        cbInstructions[0x1E] = new("RR (HL)", RR, fetchR8, 4);
        cbInstructions[0x1F] = new("RR A", RR, fetchR8, 2);

        // 0x20-0x2F: SLA r8
        cbInstructions[0x20] = new("SLA B", SLA, fetchR8, 2);
        cbInstructions[0x21] = new("SLA C", SLA, fetchR8, 2);
        cbInstructions[0x22] = new("SLA D", SLA, fetchR8, 2);
        cbInstructions[0x23] = new("SLA E", SLA, fetchR8, 2);
        cbInstructions[0x24] = new("SLA H", SLA, fetchR8, 2);
        cbInstructions[0x25] = new("SLA L", SLA, fetchR8, 2);
        cbInstructions[0x26] = new("SLA (HL)", SLA, fetchR8, 4);
        cbInstructions[0x27] = new("SLA A", SLA, fetchR8, 2);
        cbInstructions[0x28] = new("SRA B", SRA, fetchR8, 2);
        cbInstructions[0x29] = new("SRA C", SRA, fetchR8, 2);
        cbInstructions[0x2A] = new("SRA D", SRA, fetchR8, 2);
        cbInstructions[0x2B] = new("SRA E", SRA, fetchR8, 2);
        cbInstructions[0x2C] = new("SRA H", SRA, fetchR8, 2);
        cbInstructions[0x2D] = new("SRA L", SRA, fetchR8, 2);
        cbInstructions[0x2E] = new("SRA (HL)", SRA, fetchR8, 4);
        cbInstructions[0x2F] = new("SRA A", SRA, fetchR8, 2);

        // 0x30-0x3F: SWAP r8 and SRL r8
        cbInstructions[0x30] = new("SWAP B", SWAP, fetchR8, 2);
        cbInstructions[0x31] = new("SWAP C", SWAP, fetchR8, 2);
        cbInstructions[0x32] = new("SWAP D", SWAP, fetchR8, 2);
        cbInstructions[0x33] = new("SWAP E", SWAP, fetchR8, 2);
        cbInstructions[0x34] = new("SWAP H", SWAP, fetchR8, 2);
        cbInstructions[0x35] = new("SWAP L", SWAP, fetchR8, 2);
        cbInstructions[0x36] = new("SWAP (HL)", SWAP, fetchR8, 4);
        cbInstructions[0x37] = new("SWAP A", SWAP, fetchR8, 2);
        cbInstructions[0x38] = new("SRL B", SRL, fetchR8, 2);
        cbInstructions[0x39] = new("SRL C", SRL, fetchR8, 2);
        cbInstructions[0x3A] = new("SRL D", SRL, fetchR8, 2);
        cbInstructions[0x3B] = new("SRL E", SRL, fetchR8, 2);
        cbInstructions[0x3C] = new("SRL H", SRL, fetchR8, 2);
        cbInstructions[0x3D] = new("SRL L", SRL, fetchR8, 2);
        cbInstructions[0x3E] = new("SRL (HL)", SRL, fetchR8, 4);
        cbInstructions[0x3F] = new("SRL A", SRL, fetchR8, 2);

        // 0x40-0x4F: BIT 0, r8
        cbInstructions[0x40] = new("BIT 0, B", BIT, fetchR8, 2);
        cbInstructions[0x41] = new("BIT 0, C", BIT, fetchR8, 2);
        cbInstructions[0x42] = new("BIT 0, D", BIT, fetchR8, 2);
        cbInstructions[0x43] = new("BIT 0, E", BIT, fetchR8, 2);
        cbInstructions[0x44] = new("BIT 0, H", BIT, fetchR8, 2);
        cbInstructions[0x45] = new("BIT 0, L", BIT, fetchR8, 2);
        cbInstructions[0x46] = new("BIT 0, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x47] = new("BIT 0, A", BIT, fetchR8, 2);
        cbInstructions[0x48] = new("BIT 1, B", BIT, fetchR8, 2);
        cbInstructions[0x49] = new("BIT 1, C", BIT, fetchR8, 2);
        cbInstructions[0x4A] = new("BIT 1, D", BIT, fetchR8, 2);
        cbInstructions[0x4B] = new("BIT 1, E", BIT, fetchR8, 2);
        cbInstructions[0x4C] = new("BIT 1, H", BIT, fetchR8, 2);
        cbInstructions[0x4D] = new("BIT 1, L", BIT, fetchR8, 2);
        cbInstructions[0x4E] = new("BIT 1, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x4F] = new("BIT 1, A", BIT, fetchR8, 2);

        // 0x50-0x5F: BIT 2, r8
        cbInstructions[0x50] = new("BIT 2, B", BIT, fetchR8, 2);
        cbInstructions[0x51] = new("BIT 2, C", BIT, fetchR8, 2);
        cbInstructions[0x52] = new("BIT 2, D", BIT, fetchR8, 2);
        cbInstructions[0x53] = new("BIT 2, E", BIT, fetchR8, 2);
        cbInstructions[0x54] = new("BIT 2, H", BIT, fetchR8, 2);
        cbInstructions[0x55] = new("BIT 2, L", BIT, fetchR8, 2);
        cbInstructions[0x56] = new("BIT 2, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x57] = new("BIT 2, A", BIT, fetchR8, 2);
        cbInstructions[0x58] = new("BIT 3, B", BIT, fetchR8, 2);
        cbInstructions[0x59] = new("BIT 3, C", BIT, fetchR8, 2);
        cbInstructions[0x5A] = new("BIT 3, D", BIT, fetchR8, 2);
        cbInstructions[0x5B] = new("BIT 3, E", BIT, fetchR8, 2);
        cbInstructions[0x5C] = new("BIT 3, H", BIT, fetchR8, 2);
        cbInstructions[0x5D] = new("BIT 3, L", BIT, fetchR8, 2);
        cbInstructions[0x5E] = new("BIT 3, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x5F] = new("BIT 3, A", BIT, fetchR8, 2);

        // 0x60-0x6F: BIT 4, r8
        cbInstructions[0x60] = new("BIT 4, B", BIT, fetchR8, 2);
        cbInstructions[0x61] = new("BIT 4, C", BIT, fetchR8, 2);
        cbInstructions[0x62] = new("BIT 4, D", BIT, fetchR8, 2);
        cbInstructions[0x63] = new("BIT 4, E", BIT, fetchR8, 2);
        cbInstructions[0x64] = new("BIT 4, H", BIT, fetchR8, 2);
        cbInstructions[0x65] = new("BIT 4, L", BIT, fetchR8, 2);
        cbInstructions[0x66] = new("BIT 4, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x67] = new("BIT 4, A", BIT, fetchR8, 2);
        cbInstructions[0x68] = new("BIT 5, B", BIT, fetchR8, 2);
        cbInstructions[0x69] = new("BIT 5, C", BIT, fetchR8, 2);
        cbInstructions[0x6A] = new("BIT 5, D", BIT, fetchR8, 2);
        cbInstructions[0x6B] = new("BIT 5, E", BIT, fetchR8, 2);
        cbInstructions[0x6C] = new("BIT 5, H", BIT, fetchR8, 2);
        cbInstructions[0x6D] = new("BIT 5, L", BIT, fetchR8, 2);
        cbInstructions[0x6E] = new("BIT 5, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x6F] = new("BIT 5, A", BIT, fetchR8, 2);

        // 0x70-0x7F: BIT 6, r8 and BIT 7, r8
        cbInstructions[0x70] = new("BIT 6, B", BIT, fetchR8, 2);
        cbInstructions[0x71] = new("BIT 6, C", BIT, fetchR8, 2);
        cbInstructions[0x72] = new("BIT 6, D", BIT, fetchR8, 2);
        cbInstructions[0x73] = new("BIT 6, E", BIT, fetchR8, 2);
        cbInstructions[0x74] = new("BIT 6, H", BIT, fetchR8, 2);
        cbInstructions[0x75] = new("BIT 6, L", BIT, fetchR8, 2);
        cbInstructions[0x76] = new("BIT 6, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x77] = new("BIT 6, A", BIT, fetchR8, 2);
        cbInstructions[0x78] = new("BIT 7, B", BIT, fetchR8, 2);
        cbInstructions[0x79] = new("BIT 7, C", BIT, fetchR8, 2);
        cbInstructions[0x7A] = new("BIT 7, D", BIT, fetchR8, 2);
        cbInstructions[0x7B] = new("BIT 7, E", BIT, fetchR8, 2);
        cbInstructions[0x7C] = new("BIT 7, H", BIT, fetchR8, 2);
        cbInstructions[0x7D] = new("BIT 7, L", BIT, fetchR8, 2);
        cbInstructions[0x7E] = new("BIT 7, (HL)", BIT, fetchR8, 3);
        cbInstructions[0x7F] = new("BIT 7, A", BIT, fetchR8, 2);

        // 0x80-0x8F: RES 0, r8
        cbInstructions[0x80] = new("RES 0, B", RES, fetchR8, 2);
        cbInstructions[0x81] = new("RES 0, C", RES, fetchR8, 2);
        cbInstructions[0x82] = new("RES 0, D", RES, fetchR8, 2);
        cbInstructions[0x83] = new("RES 0, E", RES, fetchR8, 2);
        cbInstructions[0x84] = new("RES 0, H", RES, fetchR8, 2);
        cbInstructions[0x85] = new("RES 0, L", RES, fetchR8, 2);
        cbInstructions[0x86] = new("RES 0, (HL)", RES, fetchR8, 4);
        cbInstructions[0x87] = new("RES 0, A", RES, fetchR8, 2);
        cbInstructions[0x88] = new("RES 1, B", RES, fetchR8, 2);
        cbInstructions[0x89] = new("RES 1, C", RES, fetchR8, 2);
        cbInstructions[0x8A] = new("RES 1, D", RES, fetchR8, 2);
        cbInstructions[0x8B] = new("RES 1, E", RES, fetchR8, 2);
        cbInstructions[0x8C] = new("RES 1, H", RES, fetchR8, 2);
        cbInstructions[0x8D] = new("RES 1, L", RES, fetchR8, 2);
        cbInstructions[0x8E] = new("RES 1, (HL)", RES, fetchR8, 4);
        cbInstructions[0x8F] = new("RES 1, A", RES, fetchR8, 2);

        // 0x90-0x9F: RES 2, r8
        cbInstructions[0x90] = new("RES 2, B", RES, fetchR8, 2);
        cbInstructions[0x91] = new("RES 2, C", RES, fetchR8, 2);
        cbInstructions[0x92] = new("RES 2, D", RES, fetchR8, 2);
        cbInstructions[0x93] = new("RES 2, E", RES, fetchR8, 2);
        cbInstructions[0x94] = new("RES 2, H", RES, fetchR8, 2);
        cbInstructions[0x95] = new("RES 2, L", RES, fetchR8, 2);
        cbInstructions[0x96] = new("RES 2, (HL)", RES, fetchR8, 4);
        cbInstructions[0x97] = new("RES 2, A", RES, fetchR8, 2);
        cbInstructions[0x98] = new("RES 3, B", RES, fetchR8, 2);
        cbInstructions[0x99] = new("RES 3, C", RES, fetchR8, 2);
        cbInstructions[0x9A] = new("RES 3, D", RES, fetchR8, 2);
        cbInstructions[0x9B] = new("RES 3, E", RES, fetchR8, 2);
        cbInstructions[0x9C] = new("RES 3, H", RES, fetchR8, 2);
        cbInstructions[0x9D] = new("RES 3, L", RES, fetchR8, 2);
        cbInstructions[0x9E] = new("RES 3, (HL)", RES, fetchR8, 4);
        cbInstructions[0x9F] = new("RES 3, A", RES, fetchR8, 2);

        // 0xA0-0xAF: RES 4, r8
        cbInstructions[0xA0] = new("RES 4, B", RES, fetchR8, 2);
        cbInstructions[0xA1] = new("RES 4, C", RES, fetchR8, 2);
        cbInstructions[0xA2] = new("RES 4, D", RES, fetchR8, 2);
        cbInstructions[0xA3] = new("RES 4, E", RES, fetchR8, 2);
        cbInstructions[0xA4] = new("RES 4, H", RES, fetchR8, 2);
        cbInstructions[0xA5] = new("RES 4, L", RES, fetchR8, 2);
        cbInstructions[0xA6] = new("RES 4, (HL)", RES, fetchR8, 4);
        cbInstructions[0xA7] = new("RES 4, A", RES, fetchR8, 2);
        cbInstructions[0xA8] = new("RES 5, B", RES, fetchR8, 2);
        cbInstructions[0xA9] = new("RES 5, C", RES, fetchR8, 2);
        cbInstructions[0xAA] = new("RES 5, D", RES, fetchR8, 2);
        cbInstructions[0xAB] = new("RES 5, E", RES, fetchR8, 2);
        cbInstructions[0xAC] = new("RES 5, H", RES, fetchR8, 2);
        cbInstructions[0xAD] = new("RES 5, L", RES, fetchR8, 2);
        cbInstructions[0xAE] = new("RES 5, (HL)", RES, fetchR8, 4);
        cbInstructions[0xAF] = new("RES 5, A", RES, fetchR8, 2);

        // 0xB0-0xBF: RES 6, r8 and RES 7, r8
        cbInstructions[0xB0] = new("RES 6, B", RES, fetchR8, 2);
        cbInstructions[0xB1] = new("RES 6, C", RES, fetchR8, 2);
        cbInstructions[0xB2] = new("RES 6, D", RES, fetchR8, 2);
        cbInstructions[0xB3] = new("RES 6, E", RES, fetchR8, 2);
        cbInstructions[0xB4] = new("RES 6, H", RES, fetchR8, 2);
        cbInstructions[0xB5] = new("RES 6, L", RES, fetchR8, 2);
        cbInstructions[0xB6] = new("RES 6, (HL)", RES, fetchR8, 4);
        cbInstructions[0xB7] = new("RES 6, A", RES, fetchR8, 2);
        cbInstructions[0xB8] = new("RES 7, B", RES, fetchR8, 2);
        cbInstructions[0xB9] = new("RES 7, C", RES, fetchR8, 2);
        cbInstructions[0xBA] = new("RES 7, D", RES, fetchR8, 2);
        cbInstructions[0xBB] = new("RES 7, E", RES, fetchR8, 2);
        cbInstructions[0xBC] = new("RES 7, H", RES, fetchR8, 2);
        cbInstructions[0xBD] = new("RES 7, L", RES, fetchR8, 2);
        cbInstructions[0xBE] = new("RES 7, (HL)", RES, fetchR8, 4);
        cbInstructions[0xBF] = new("RES 7, A", RES, fetchR8, 2);

        // 0xC0-0xCF: SET 0, r8
        cbInstructions[0xC0] = new("SET 0, B", SET, fetchR8, 2);
        cbInstructions[0xC1] = new("SET 0, C", SET, fetchR8, 2);
        cbInstructions[0xC2] = new("SET 0, D", SET, fetchR8, 2);
        cbInstructions[0xC3] = new("SET 0, E", SET, fetchR8, 2);
        cbInstructions[0xC4] = new("SET 0, H", SET, fetchR8, 2);
        cbInstructions[0xC5] = new("SET 0, L", SET, fetchR8, 2);
        cbInstructions[0xC6] = new("SET 0, (HL)", SET, fetchR8, 4);
        cbInstructions[0xC7] = new("SET 0, A", SET, fetchR8, 2);
        cbInstructions[0xC8] = new("SET 1, B", SET, fetchR8, 2);
        cbInstructions[0xC9] = new("SET 1, C", SET, fetchR8, 2);
        cbInstructions[0xCA] = new("SET 1, D", SET, fetchR8, 2);
        cbInstructions[0xCB] = new("SET 1, E", SET, fetchR8, 2);
        cbInstructions[0xCC] = new("SET 1, H", SET, fetchR8, 2);
        cbInstructions[0xCD] = new("SET 1, L", SET, fetchR8, 2);
        cbInstructions[0xCE] = new("SET 1, (HL)", SET, fetchR8, 4);
        cbInstructions[0xCF] = new("SET 1, A", SET, fetchR8, 2);

        // 0xD0-0xDF: SET 2, r8
        cbInstructions[0xD0] = new("SET 2, B", SET, fetchR8, 2);
        cbInstructions[0xD1] = new("SET 2, C", SET, fetchR8, 2);
        cbInstructions[0xD2] = new("SET 2, D", SET, fetchR8, 2);
        cbInstructions[0xD3] = new("SET 2, E", SET, fetchR8, 2);
        cbInstructions[0xD4] = new("SET 2, H", SET, fetchR8, 2);
        cbInstructions[0xD5] = new("SET 2, L", SET, fetchR8, 2);
        cbInstructions[0xD6] = new("SET 2, (HL)", SET, fetchR8, 4);
        cbInstructions[0xD7] = new("SET 2, A", SET, fetchR8, 2);
        cbInstructions[0xD8] = new("SET 3, B", SET, fetchR8, 2);
        cbInstructions[0xD9] = new("SET 3, C", SET, fetchR8, 2);
        cbInstructions[0xDA] = new("SET 3, D", SET, fetchR8, 2);
        cbInstructions[0xDB] = new("SET 3, E", SET, fetchR8, 2);
        cbInstructions[0xDC] = new("SET 3, H", SET, fetchR8, 2);
        cbInstructions[0xDD] = new("SET 3, L", SET, fetchR8, 2);
        cbInstructions[0xDE] = new("SET 3, (HL)", SET, fetchR8, 4);
        cbInstructions[0xDF] = new("SET 3, A", SET, fetchR8, 2);

        // 0xE0-0xEF: SET 4, r8
        cbInstructions[0xE0] = new("SET 4, B", SET, fetchR8, 2);
        cbInstructions[0xE1] = new("SET 4, C", SET, fetchR8, 2);
        cbInstructions[0xE2] = new("SET 4, D", SET, fetchR8, 2);
        cbInstructions[0xE3] = new("SET 4, E", SET, fetchR8, 2);
        cbInstructions[0xE4] = new("SET 4, H", SET, fetchR8, 2);
        cbInstructions[0xE5] = new("SET 4, L", SET, fetchR8, 2);
        cbInstructions[0xE6] = new("SET 4, (HL)", SET, fetchR8, 4);
        cbInstructions[0xE7] = new("SET 4, A", SET, fetchR8, 2);
        cbInstructions[0xE8] = new("SET 5, B", SET, fetchR8, 2);
        cbInstructions[0xE9] = new("SET 5, C", SET, fetchR8, 2);
        cbInstructions[0xEA] = new("SET 5, D", SET, fetchR8, 2);
        cbInstructions[0xEB] = new("SET 5, E", SET, fetchR8, 2);
        cbInstructions[0xEC] = new("SET 5, H", SET, fetchR8, 2);
        cbInstructions[0xED] = new("SET 5, L", SET, fetchR8, 2);
        cbInstructions[0xEE] = new("SET 5, (HL)", SET, fetchR8, 4);
        cbInstructions[0xEF] = new("SET 5, A", SET, fetchR8, 2);

        // 0xF0-0xFF: SET 6, r8 and SET 7, r8
        cbInstructions[0xF0] = new("SET 6, B", SET, fetchR8, 2);
        cbInstructions[0xF1] = new("SET 6, C", SET, fetchR8, 2);
        cbInstructions[0xF2] = new("SET 6, D", SET, fetchR8, 2);
        cbInstructions[0xF3] = new("SET 6, E", SET, fetchR8, 2);
        cbInstructions[0xF4] = new("SET 6, H", SET, fetchR8, 2);
        cbInstructions[0xF5] = new("SET 6, L", SET, fetchR8, 2);
        cbInstructions[0xF6] = new("SET 6, (HL)", SET, fetchR8, 4);
        cbInstructions[0xF7] = new("SET 6, A", SET, fetchR8, 2);
        cbInstructions[0xF8] = new("SET 7, B", SET, fetchR8, 2);
        cbInstructions[0xF9] = new("SET 7, C", SET, fetchR8, 2);
        cbInstructions[0xFA] = new("SET 7, D", SET, fetchR8, 2);
        cbInstructions[0xFB] = new("SET 7, E", SET, fetchR8, 2);
        cbInstructions[0xFC] = new("SET 7, H", SET, fetchR8, 2);
        cbInstructions[0xFD] = new("SET 7, L", SET, fetchR8, 2);
        cbInstructions[0xFE] = new("SET 7, (HL)", SET, fetchR8, 4);
        cbInstructions[0xFF] = new("SET 7, A", SET, fetchR8, 2);
    }
}