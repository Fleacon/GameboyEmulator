namespace GameboyEmulator.IO;

public class Timer
{
    private IO io;
    
    private ushort dividerRegister;
    private byte timerCounter;
    private byte timerModulo;
    private byte timerControl;
    
    private int tCounter;
    private int dCounter;

    public Timer(IO io)
    {
        this.io = io;
    }

    public void UpdateTimers(int cycles)
    {
        int tCycles = cycles * 4; // Machine Cycles to Tick Cycles
        updateDivider(tCycles);

        if (isClockEnabled())
        {
            tCounter -= cycles;

            if (tCounter <= 0)
            {
                setClockFrequency();

                if (timerCounter == 255) // about to overflow
                {
                    timerCounter = timerModulo;
                    io.RequestInterrupt(IO.Interrupts.Timer);
                }
                else
                {
                    timerCounter++;
                }
            }
        }
    }
    
    public void Write(int target, byte data)
    {
        switch (target)
        {
            case 0: // DIV
                dividerRegister = 0;
                break;
            case 1:
                timerCounter = data;
                break;
            case 2:
                timerModulo = data;
                break;
            case 3:
                timerControl = data;
                break;
        }
    }

    public byte Read(int target)
    {
        switch (target)
        {
            case 0: return (byte)(dividerRegister >> 8);
            case 1: return timerCounter;
            case 2: return timerModulo;
            case 3: return timerControl;
            default: throw new Exception("Invalid Timer target");
        }
    }

    private void updateDivider(int cycles)
    {
        dCounter += cycles;
        if (dCounter >= 256)
        {
            dCounter -= 256;
            dividerRegister++;
        }
    }

    private bool isClockEnabled()
    {
        return (timerControl & 0x4) == 0x4;
    }

    private void setClockFrequency()
    {
        byte clock = (byte)(timerControl & 0x3);
        tCounter = clock switch
        {
            0 => 1024, // 4096Hz
            1 => 16, // 262144Hz
            2 => 64, // 65536Hz
            3 => 256, // 16382Hz
        };
    }
}