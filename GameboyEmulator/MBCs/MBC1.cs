namespace GameboyEmulator.MBCs;

public class MBC1 : IMapper
{
    public bool RamEnabled { get; set; }

    private byte mode; // 0b0 or 0b1 Mode (ROM banking or RAM banking Mode)

    private byte bank1 = 1; // Lower 5 bits for ROM Banking
    private byte bank2; // Either 2 upper bits of ROM Bank or 2 bit Register for RAM Banking
    
    public byte Read(Cartridge c, ushort addr)
    {
        if (addr <= 0x3FFF)
        {
            if (mode == 0)
            {
                return readRomBank(c, 0, addr);
            }
            if (mode == 1)
            {
                byte bank = (byte)(bank2 << 5);
                int bankMask = (1 << c.RomBankNumBits) - 1;
                bank = (byte)(bank & bankMask);
                return readRomBank(c, bank, addr);
            }
            throw new Exception($"Mode undefined: {mode:X2}");
        }
        if (addr is >= 0x4000 and <= 0x7FFF)
        {
            byte bank = (byte)((bank2 << 5) | bank1);
            int bankMask = (1 << c.RomBankNumBits) - 1;
            bank = (byte)(bank & bankMask);
            return readRomBank(c, bank, addr - 0x4000);
        }
        if (addr is >= 0xA000 and <= 0xBFFF)
        {
            if (!c.RamExists || !RamEnabled)
                return 0xFF;
            if (mode == 0)
            {
                return c.RAMBankNN[0][addr - 0xA000];
            }
            if (mode == 1)
            {
                byte ramBank = (byte)(bank2 & ((1 << c.RamBankNumBits) - 1));
                return c.RAMBankNN[ramBank][addr - 0xA000];
            }
            throw new Exception($"Mode undefined: {mode:X2}");
        }
        throw new Exception($"Adress out of range. {addr:X4}");
    }

    public void Write(Cartridge c, ushort addr, byte value)
    {
        if (addr <= 0x1FFF)
        {
            RamEnabled = (value & 0xF) == 0b1010;
        }
        if (addr is >= 0x2000 and <= 0x3FFF)
        {
            int bank = value & 0x1F;
            if (bank == 0)
                bank = 1;
            bank1 = (byte)bank;
        }
        if (addr is >= 0x4000 and <= 0x5FFF)
        {
            bank2 = (byte)(value & 0x3);
        }
        if (addr is >= 0x6000 and <= 0x7FFF)
        {
            mode = (byte)(value & 1);
        }
        if (addr is >= 0xA000 and <= 0xBFFF)
        {
            if (!c.RamExists || !RamEnabled)
                return;
            if (mode == 0)
            {
                c.RAMBankNN[0][addr - 0xA000] = value;
            }
            else if (mode == 1)
            {
                byte ramBank = (byte)(bank2 & ((1 << c.RamBankNumBits) - 1));
                c.RAMBankNN[ramBank][addr - 0xA000] = value;
            }
            else
            {
                throw new Exception($"Mode undefined: {mode:X2}");
            }
        }
    }

    // Because RomBank0 is stored seperatly, some logic for handling bank 0 access
    private byte readRomBank(Cartridge c, byte bank, int addr)
    {
        if (bank == 0)
        {
            return c.ROMBank00[addr];
        }
        else
        {
            return c.ROMBankNN[bank - 1][addr];
        }
    }
}