namespace GameboyEmulator;

public class Registers
{
    private LR35902 cpu;
    public byte A { get; set; } // Accumulator
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public byte F { get; set; }
    
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
    }

}