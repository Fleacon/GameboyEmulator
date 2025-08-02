namespace GameboyEmulator;

public class Bus
{
    private byte[] ROMBank00;
    private byte[] ROMBankNN;
    private byte[] VRAM;
    private byte[] WRAM;
    private byte[] EchoRAM; // prohibited
    private IO IOregister;
    private byte[] HRAM;
    private byte IERegister;

    public Bus()
    {
        ROMBank00 = new byte[16384]; // 16 KiB
        ROMBankNN = new byte[16384]; // 16 KiB
        VRAM = new byte[8192]; // 8 KiB
        WRAM = new byte[8192]; // 8 KiB
        IOregister = new IO();
        HRAM = new byte[126];
        
        IERegister = 0x00;
    }

    public void LoadROM(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }
        
        byte[] rom = File.ReadAllBytes(filePath);

        if (rom.Length > ROMBank00.Length + ROMBankNN.Length)
        {
            throw new IndexOutOfRangeException("ROM size is too large");
        }
        
        int firstBankSize = Math.Min(rom.Length, ROMBank00.Length);
        Array.Copy(rom, 0, ROMBank00, 0, firstBankSize);
        
        if (rom.Length > ROMBank00.Length)
        {
            int bytesRemaining = rom.Length - ROMBank00.Length;
            
            int secondBankSize = Math.Min(bytesRemaining, ROMBankNN.Length);
            Array.Copy(rom, ROMBank00.Length, ROMBankNN, 0, secondBankSize);
        }
        Console.WriteLine("ROM loaded");
    }

    public void Write8(ushort addr, byte data)
    {
        if (addr < 0x7FFF) 
            return;
        if (addr is >= 0x8000 and <= 0x9FFF)
            VRAM[addr - 0x8000] = data;
        if (addr is >= 0xC000 and <= 0xDFFF)
            WRAM[addr - 0xC000] = data;
        if (addr is >= 0xFF00 and <= 0xFF7F)
            IOregister.Write((ushort)(addr - 0xFF00), data);
        if (addr is >= 0xFF80 and <= 0xFFFE)
            HRAM[addr - 0xFF80] = data;
        if (addr is 0xFFFF)
            IERegister = data;
    }
    
    public void Write16(ushort addr, ushort data)
    {
        // Write the low byte to the current address
        Write8(addr, (byte)(data & 0xFF));

        // Write the high byte to the next address
        Write8((ushort)(addr + 1), (byte)(data >> 8));
    }

    public byte Read(ushort addr)
    {
        if (addr <= 0x3FFF)
            return ROMBank00[addr % 16384];
        if (addr is >= 0x4000 and <= 0x7FFF)
            return ROMBankNN[addr - 0x4000];
        if (addr is >= 0x8000 and <= 0x9FFF)
            return VRAM[addr - 0x8000];
        if (addr is >= 0xC000 and <= 0xDFFF)
            return WRAM[addr - 0xC000];
        if (addr is >= 0xFF00 and <= 0xFF7F)
            return IOregister.Read((ushort)(addr - 0xFF00));
        if (addr is >= 0xFF80 and <= 0xFFFE)
            return HRAM[addr - 0xFF80];
        if (addr is 0xFFFF)
            return IERegister;
        return 0;
    }
}