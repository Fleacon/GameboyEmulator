namespace GameboyEmulator;

public class Bus
{
    private byte[] ROMBank00;
    private byte[] ROMBankNN;
    private byte[] VRAM;
    private byte[] WRAM;
    private byte[] EchoRAM; // prohibited
    private byte[] HRAM;
    private byte IERegister;

    public Bus()
    {
        ROMBank00 = new byte[16384]; // 16 KiB
        ROMBankNN = new byte[16384]; // 16 KiB
        VRAM = new byte[8192]; // 8 KiB
        WRAM = new byte[8192]; // 8 KiB
        HRAM = new byte[126];
    }

    public void Write(ushort addr, byte data)
    {
        if (addr < 0x7FFF) 
            return;
        if (addr is >= 0x8000 and <= 0x9FFF)
            VRAM[addr % 8192] = data;
        if (addr is >= 0xC000 and <= 0xDFFF)
            WRAM[addr % 8192] = data;
        if (addr is >= 0xFF80 and <= 0xFFFE)
            HRAM[addr % 126] = data;
        if (addr is 0xFFFF)
            IERegister = data;
    }

    public byte Read(ushort addr)
    {
        if (addr <= 0x3FFF)
            return ROMBank00[addr % 16384];
        if (addr is >= 0x4000 and <= 0x7FFF)
            return ROMBankNN[addr % 16384];
        if (addr is >= 0x8000 and <= 0x9FFF)
            return VRAM[addr % 8192];
        if (addr is >= 0xC000 and <= 0xDFFF)
            return WRAM[addr % 8192];
        if (addr is >= 0xFF80 and <= 0xFFFE)
            return HRAM[addr % 126];
        if (addr is 0xFFFF)
            return IERegister;
        return 0x0000;
    }
}