namespace GameboyEmulator.MBCs;

public abstract class MBC
{
    // Hardware
    public bool RamExists { get; init; }
    public bool BatteryExists { get; init; }
    public bool TimerExists { get; init; }
    public bool RumbleExists { get; init; }
    public bool SensorExists { get; init; }
    
    // Registers
    public bool RamEnabled { get; set; }
    public byte RomBankNum { get; set; }
    public byte RamBankNum { get; set; }
    public bool isModeAdvanced { get; set; } // Mode0/simple = false, Mode1/advanced = true

    public MBC(bool ramExists, bool batteryExists, bool timerExists, bool rumbleExists, bool sensorExists)
    {
        RamExists = ramExists;
        BatteryExists = batteryExists;
        TimerExists = timerExists;
        RumbleExists = rumbleExists;
        SensorExists = sensorExists;
    }
    
    public abstract byte Read(ushort addr);
    public abstract void Write(ushort addr, byte value);
}