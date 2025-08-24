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
    
    // Hardware
    public bool RamExists { get; private set; }
    public bool BatteryExists { get; private set; }
    public bool TimerExists { get; private set; }
    public bool RumbleExists { get; private set; }
    public bool SensorExists { get; private set; }
    
    private IMapper? mbc;
    private bool isRomOnly;
    
    public readonly byte[] ROMBank00;
    public byte[][] ROMBankNN; // Does not contain Bank 0
    public ushort NumOfRomBanks { get; private set; }
    public ushort RomBankNumBits { get; private set; }
    
    public byte[][] RAMBankNN;
    public ushort NumOfRamBanks { get; private set; }

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

    public void Write(ushort addr, byte data)
    {
        if (mbc == null)
        {
            if (addr is >= 0xA000 and <= 0xBFFF && RamExists)
            {
                RAMBankNN[0][addr] = data;
            }
        }
        else
        {
            mbc.Write(this, addr, data);
        }
    }

    public byte Read(ushort addr)
    {
        if (mbc == null)
        {
            if(addr <= 0x3FFF)
                return ROMBank00[addr];
            if (addr is >= 0x4000 and <= 0x7FFF)
                return ROMBankNN[0][addr - 0x4000];
            if (addr is >= 0xA000 and <= 0xBFFF && RamExists)
                return RAMBankNN[0][addr - 0xA000];
            throw new Exception($"Invalid cartridge address {addr}");
        }
        else
        {
            return mbc.Read(this, addr);
        }
    }

    private void ReadCartridgeHeader(byte[] data)
    {
        Title = Encoding.ASCII.GetString(data, 0x0134, 0x0143);
        ManufacturerCode = Encoding.ASCII.GetString(data, 0x0134, 0x0142);
        CGBFlag = data[0x0143];
        SGBFlag = data[0x0146];
        DetermineCartridgeType(data[0x0147]);
        InitRomBanks(data);
        InitRamBanks(data);
        DestinationCode = data[0x014A];
        
        // TODO: Maybe implement Checksum or Nintendo Logo?
    }

    private void InitRamBanks(byte[] data)
    {
        var ramCode = data[0x0149];
        int ramSize = 0;
        switch (ramCode)
        {
            case 0x00:
                ramSize = 0;
                break;
            case 0x01: // unused
                break;
            case 0x02:
                ramSize = 0x2000;
                break;
            case 0x03:
                ramSize = 0x8000;
                break;
            case 0x04:
                ramSize = 0x20000;
                break;
            case 0x05:
                ramSize = 0x10000;
                break;
        }
        NumOfRamBanks = (byte)(ramSize / 0x2000);
        RAMBankNN = new byte[NumOfRamBanks][];
        for (int i = 0; i < NumOfRamBanks; i++)
        {
            RAMBankNN[i] = new byte[0x2000];
            Array.Fill(RAMBankNN[i], (byte)0xFF);
        }
    }

    private void InitRomBanks(byte[] data)
    {
        var romSize = 0x8000 * (1 << data[0x0148]);
        NumOfRomBanks = (byte)(romSize / 0x4000);
        RomBankNumBits = (byte)MathF.Ceiling(MathF.Log2(NumOfRomBanks));
        ROMBankNN = new byte[NumOfRomBanks - 1][];
        for (int i = 0; i < NumOfRomBanks - 1; i++)
        {
            ROMBankNN[i] = new byte[0x4000];
        }
    }

    private void DetermineCartridgeType(byte code)
    {
        switch (code)
        {
            case 0x00:
                isRomOnly = true;
                mbc = null;
                break;
            case 0x01:
                mbc = new MBC1();
                break;
            case 0x02:
                mbc = new MBC1();
                RamExists = true;
                break;
            case 0x03:
                mbc = new MBC1();
                RamExists = true;
                BatteryExists = true;
                break;
            /*
            case 0x05:
                mbc = new MBC2();
                break;
            case 0x06:
                mbc = new MBC2();
                BatteryExists = true;
                break;
            case 0x08:
                mbc = null;
                RamExists = true;
                break;
            case 0x09:
                mbc = null;
                RamExists = true;
                BatteryExists = true;
                break;
            case 0x0B:
                mbc = new MMM01();
                break;
            case 0x0C:
                mbc = new MMM01();
                RamExists = true;
                break;
            case 0x0D:
                mbc = new MMM01();
                RamExists = true;
                BatteryExists = true;
                break;
            case 0x0F:
                mbc = new MBC3();
                TimerExists = true;
                BatteryExists = true;
                break;
            case 0x10:
                mbc = new MBC3();
                RamExists = true;
                TimerExists = true;
                BatteryExists = true;
                break;
            case 0x11:
                mbc = new MBC3();
                break;
            case 0x12:
                mbc = new MBC3();
                RamExists = true;
                break;
            case 0x13:
                mbc = new MBC3();
                RamExists = true;
                BatteryExists = true;
                break;
            case 0x19:
                mbc = new MBC5();
                break;
            case 0x1A:
                mbc = new MBC5();
                RamExists = true;
                break;
            case 0x1B:
                mbc = new MBC5();
                RamExists = true;
                BatteryExists = true;
                break;
            case 0x1C:
                mbc = new MBC5();
                RumbleExists = true;
                break;
            case 0x1D:
                mbc = new MBC5();
                RumbleExists = true;
                RamExists = true;
                break;
            case 0x1E:
                mbc = new MBC5();
                RumbleExists = true;
                RamExists = true;
                BatteryExists = true;
                break;
            case 0x29:
                mbc = new MBC6();
                break;
            case 0x22:
                mbc = new MBC7();
                SensorExists = true;
                RumbleExists = true;
                RamExists = true;
                BatteryExists = true;
                break;
            case 0xFC:
                // Pocket Camera
                break;
            case 0xFD:
                // Bandai Tama5
                break;
            case 0xFE:
                mbc = new HuC3();
                break;
            case 0xFF:
                mbc = new HuC1();
                RamExists = true;
                BatteryExists = true;
                break;
                */
            default:
                throw new Exception("Unknown Cartridge Type");
        }
    }
}