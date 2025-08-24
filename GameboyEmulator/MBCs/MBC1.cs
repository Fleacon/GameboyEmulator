namespace GameboyEmulator.MBCs;

public class MBC1 : IMapper
{
    public bool RamEnabled { get; set; }

    public byte RomBankNum { get; set; }

    public byte RamBankNum { get; set; }
    public bool isRamBankingMode { get; set; }

    public byte Read(Cartridge c, ushort addr)
    {
        if (addr <= 0x3FFF)
        {
            if (isRamBankingMode)
            {
                int bank = RamBankNum << 5;
                int bankMask = (1 << c.RomBankNumBits) - 1;
                bank = bank & bankMask;
                if (bank == 0)
                {
                    return c.ROMBank00[addr];
                }
                else
                {
                    return c.ROMBankNN[bank - 1][addr];
                }
            }
            else
            {
                return c.ROMBank00[addr];
            }
        }
        if (addr is >= 0x4000 and <= 0x7FFF)
        {
            int bank = RamBankNum << 5 | RomBankNum;
            int bankMask = (1 << c.RomBankNumBits) - 1;
            bank = bank & bankMask;
            // Cannot Access Bank 0x00, 0x20, 0x40, 0x60 -> instead read 0x01, 0x21 and so on.
            // Because Bank 0 is stored seperatly the index of the Rombanks Array is one lower than the actual bank number.
            // So we decrement if it is not zero to access the proper Banks and let the value be if it is zero because it is already one higher
            if (bank != 0 && bank != 0x20 && bank != 0x40 && bank != 0x60) 
            {
                bank--;
            }
            return c.ROMBankNN[bank][addr - 0x4000];
        }
        if (addr is >= 0xA000 and <= 0xBFFF)
        {
            if (!c.RamExists || !RamEnabled)
                return 0xFF;
            
            int bank = isRamBankingMode ? RamBankNum : 0;
            return c.RAMBankNN[bank][addr - 0xA000];
        }
        throw new Exception($"Adress out of range. {addr:X4}");
    }

    public void Write(Cartridge c, ushort addr, byte value)
    {
        if (addr <= 0x1FFF)
            RamEnabled = (value & 0xF) == 0xA;
        if (addr is >= 0x2000 and <= 0x3FFF)
        {
            byte lower5 = (byte)(value & 0x1F);
            RomBankNum = (byte)((RomBankNum & 0xE0) | lower5);
            if (lower5 == 0)
                RomBankNum++;
        }
        if (addr is >= 0x4000 and <= 0x5FFF)
        {
            byte lower2 = (byte)(value & 0x03);
            RamBankNum = lower2;
        }
        if (addr is >= 0x6000 and <= 0x7FFF)
        {
            if (value == 0x00)
                isRamBankingMode = false;
            if (value == 0x01)
                isRamBankingMode = true;
        }
        if (addr is >= 0xA000 and <= 0xBFFF)
        {
            if (RamEnabled)
            {
                if (!isRamBankingMode)
                {
                    c.RAMBankNN[0][addr - 0xA000] = value;
                }
                else
                {
                    c.RAMBankNN[RamBankNum][addr - 0xA000] = value;
                }
            }
        }
    }
}