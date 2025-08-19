using System.Text;
using GameboyEmulator.MBCs;

namespace GameboyEmulator;

public class Cartridge
{
    public string Title { get; private set; }
    public string ManufacturerCode { get; private set; }
    public byte CGBFlag { get; private set; }
    public byte SGBFlag { get; private set; }
    public byte DestinationCode { get; private set; }
    
    private MBC? mbc;
    private bool isRomOnly;
    
    private byte[] ROMBank00;
    private byte[][] ROMBankNN;
    
    private byte[][] RAMBankNN;

    public Cartridge()
    {
        ROMBank00 = new byte[0x4000];
    }

    public void LoadGame(byte[] data)
    {
        ReadCartridgeHeader(data);
        Array.Copy(data, 0, ROMBank00, 0, ROMBank00.Length);
        for (int i = 0; i < ROMBankNN.Length; i++)
        {
            Array.Copy(data, (i + 1) * ROMBankNN[i].Length, ROMBankNN[i], 0, ROMBankNN[i].Length);
        }
    }

    private void ReadCartridgeHeader(byte[] data)
    {
        Title = Encoding.ASCII.GetString(data, 0x0134, 0x0143);
        ManufacturerCode = Encoding.ASCII.GetString(data, 0x0134, 0x0142);
        CGBFlag = data[0x0143];
        SGBFlag = data[0x0146];
        
        var romSize = 0x8000 * (1 << data[0x0148]);
        var additionalRomBanks = romSize / 0x4000 - 1;
        ROMBankNN = new byte[additionalRomBanks][];
        for (int i = 0; i < additionalRomBanks; i++)
        {
            ROMBankNN[i] = new byte[0x4000];
        }
        
        var ramCode = data[0x0149];
        int ramSize = 0;
        switch (ramCode)
        {
            case 0x00:
                isRomOnly = true;
                break;
            case 0x01: // unused
                break;
            case 0x02:
                ramSize = 0x400;
                break;
            case 0x03:
                ramSize = 0x1000;
                break;
            case 0x04:
                ramSize = 0x4000;
                break;
            case 0x05:
                ramSize = 0x2000;
                break;
        }
        RAMBankNN = new byte[ramSize / 0x400][];
        for (int i = 0; i < RAMBankNN.Length; i++)
        {
            RAMBankNN[i] = new byte[0x400];
        }
        
        DestinationCode = data[0x014A];
        
        // TODO: Maybe implement Checksum or Nintendo Logo?
    }

    public void Write(ushort addr, byte value)
    {
        if (mbc == null) return;
        
        // TODO: Implement possible MBC Writes
    }

    public byte Read(ushort addr)
    {
        if (mbc == null)
        {
            if (addr <= 0x3FFF)
                return ROMBank00[addr];
            if (addr is >= 0x4000 and <= 0x7FFF)
                return ROMBankNN[0][addr - 0x4000];
        }

        // TODO: Implement MBC Reads
        return 0;
    }
}