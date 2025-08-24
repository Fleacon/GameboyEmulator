namespace GameboyEmulator.MBCs;

public interface IMapper
{
    // Registers
    public bool RamEnabled { get; set; }
    public byte RomBankNum { get; set; }
    public byte RamBankNum { get; set; }
    public bool isRamBankingMode { get; set; } // Rom Banking Mode = false, Ram Banking Mode = true
    
    public abstract byte Read(Cartridge c, ushort addr);
    public abstract void Write(Cartridge c, ushort addr, byte value);
}