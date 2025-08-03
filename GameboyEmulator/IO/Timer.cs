namespace GameboyEmulator.IO;

public class Timer
{
    private byte dividerRegister;
    private byte timerCounter;
    private byte timerModulo;
    private byte timerControl;
    
    private int tCounter;
    private int dCounter = 0;

    public void UpdateTimers(int cycles)
    {
        updateDivider(cycles);

        if (isClockEnabled())
        {
            tCounter -= cycles;

            if (tCounter <= 0)
            {
                setClockFrequency();

                if (timerCounter == 255) // about to overflow
                {
                    timerCounter = timerModulo;
                    // TODO: Interrupt
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
            case 0: return dividerRegister;
            case 1: return timerCounter;
            case 2: return timerModulo;
            case 3: return timerControl;
            default: throw new Exception("Invalid Timer target");
        }
    }

    private void updateDivider(int cycles)
    {
        dCounter += (byte)cycles;
        if (dCounter >= 255)
        {
            dCounter = 0;
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