using System.Collections.ObjectModel;
using GameboyEmulator.CPU;

namespace GameboyEmulator;

public class Registers
{
    private LR35902 cpu;
    public byte A { get; set;}
    public byte B { get; set;}
    public byte C { get; set;}
    public byte D { get; set;}
    public byte E { get; set;}
    public byte H { get; set;}
    public byte L { get; set;}
    public byte F { get; set;}
    
    public ushort PC { get; set; } // Program Counter
    public ushort SP { get; set; } // Stack Pointer

    public ushort AF
    {
        get => (ushort)((A << 8) + F);
        set
        {
            A = (byte)(value >> 8);
            F = (byte)(value & 0x00F0);
        }
    }
    public ushort BC
    {
        get => (ushort)((B << 8) | C);
        set
        {
            B = (byte)(value >> 8);
            C = (byte)(value & 0x00FF);
        }
    }
    public ushort DE
    {
        get => (ushort)((D << 8) | E);
        set
        {
            D = (byte)(value >> 8);
            E = (byte)(value & 0x00FF);
        }
    }
    public ushort HL
    {
        get => (ushort)((H << 8) | L);
        set
        {
            H = (byte)(value >> 8);
            L = (byte)(value & 0x00FF);
        }
    }

    public Registers(LR35902 cpu)
    {
        this.cpu = cpu;
        initStartingValues();
    }

    public byte GetR8(byte code)
    {
        return code switch
        {
            0 => B,
            1 => C,
            2 => D,
            3 => E,
            4 => H,
            5 => L,
            6 => cpu.Bus.Read(HL),
            7 => A,
            _ => throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}")
        };
    }

    public void SetR8(byte code, byte value)
    {
        switch (code)
        {
            case 0: B = value; break;
            case 1: C = value; break;
            case 2: D = value; break;
            case 3: E = value; break;
            case 4: H = value; break;
            case 5: L = value; break;
            case 6: cpu.Bus.Write8(HL, value); break;
            case 7: A = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}");
        }
    }

    public ushort GetR16(byte code) => code switch
    {
        0 => BC,
        1 => DE,
        2 => HL,
        3 => SP,
        _ => throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}")
    };
    
    public void SetR16(byte code, ushort value)
    {
        switch (code)
        {
            case 0: BC = value; break;
            case 1: DE = value; break;
            case 2: HL = value; break;
            case 3: SP = value; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}");
        }
    }

    public ushort GetR16Stk(byte code) => code switch
    {
        0 => BC,
        1 => DE,
        2 => HL,
        3 => AF,
        _ => throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}")
    };

    public void SetR16Stk(byte code, ushort value)
    {
        switch (code)
        {
            case 0: BC = value; break;
            case 1: DE = value; break;
            case 2: HL = value; break;
            case 3: AF = value; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}");
        }
    }
    
    public ushort GetR16Mem(byte code)
    {
        switch (code)
        {
            case 0: return BC;
            case 1: return DE;
            case 2: return HL++;  // Post-increment
            case 3: return HL--;  // Post-decrement
            default:
                throw new ArgumentOutOfRangeException(nameof(code), $"Unknown register code: {code}");
        }
    }
    
    public bool GetCond(byte code) => code switch
    {
        0 => !GetFlag(Flags.Z),  // NZ
        1 => GetFlag(Flags.Z),   // Z
        2 => !GetFlag(Flags.C),  // NC
        3 => GetFlag(Flags.C),   // C
        _ => throw new ArgumentOutOfRangeException(nameof(code), $"Unknown condition code: {code}")
    };
    
    public bool GetFlag(Flags flag) => (F & (byte)flag) != 0;

    public void SetFlag(Flags flag, bool value)
    {
        if (value)
            F |= (byte)flag;
        else 
            F &= (byte)~flag;
    }
    
    public enum Flags
    {
        Z = 0x80,
        N = 0x40,
        H = 0x20,
        C = 0x10
    }

    private void initStartingValues()
    {
        A = 0x01;
        F = 0xB0;
        B = 0x00;
        C = 0x13;
        D = 0x00;
        E = 0xD8;
        H = 0x01;
        L = 0x4D;
        PC = 0x0100;
        SP = 0xFFFE;
    }
}