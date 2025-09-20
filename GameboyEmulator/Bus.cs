using Timer = GameboyEmulator.IO.Timer;

namespace GameboyEmulator;

public class Bus
{
    private Cartridge cartridge;
    private byte[] VRAM;
    private byte[] WRAM;
    private byte[] EchoRAM; // prohibited
    public IO.IO IOregister;
    private byte[] HRAM;
    private byte IERegister;

    private byte lastByte; // TODO: When the CPU tries to read from unmapped addresses, it should return the last Byte stored in the bus 

    public Bus()
    {
        VRAM = new byte[8192]; // 8 KiB
        WRAM = new byte[8192]; // 8 KiB
        IOregister = new IO.IO();
        HRAM = new byte[126];
        IERegister = 0x00;
    }

    public void Write8(ushort addr, byte data)
    {
        if (addr <= 0x7FFF) 
            cartridge.Write(addr, data);
        if (addr is >= 0x8000 and <= 0x9FFF)
            VRAM[addr - 0x8000] = data;
        if (addr is >= 0xA000 and <= 0xBFFF)
            cartridge.Write(addr, data);
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
        if (addr <= 0x7FFF)
            return cartridge.Read(addr);
        if (addr is >= 0x8000 and <= 0x9FFF)
            return VRAM[addr - 0x8000];
        if (addr is >= 0xA000 and <= 0xBFFF)
            return cartridge.Read(addr);
        if (addr is >= 0xC000 and <= 0xDFFF)
            return WRAM[addr - 0xC000];
        if (addr is >= 0xFF00 and <= 0xFF7F)
            return IOregister.Read((ushort)(addr - 0xFF00));
        if (addr is >= 0xFF80 and <= 0xFFFE)
            return HRAM[addr - 0xFF80];
        if (addr is 0xFFFF)
            return IERegister;
        return 0xFF;
    }

    public void InsertCartridge(Cartridge cartridge)
    {
        this.cartridge = cartridge;
    }
}