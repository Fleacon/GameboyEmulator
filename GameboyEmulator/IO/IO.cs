namespace GameboyEmulator.IO;

public class IO
{
    private byte joypad;
    private byte serialData;
    private byte serialControl;
    private Timer timer;
    private byte interrupts;
    private byte[] audio;
    private byte[] wavePattern;
    private byte[] screen;
    private byte vramBankSelector;
    private byte bootRomMappingController;
    private byte[] vramDMA;
    private byte[] bgoObjPalettes;
    private byte wramBankSelector;

    public IO()
    {
        this.timer = new ();
        audio = new byte[23];
        wavePattern = new byte[15];
        screen = new byte[12];
        vramDMA = new byte[5];
        bgoObjPalettes = new byte[3];
        
        initStartingValues();
    }
    
    public void Write(ushort addr, byte data)
    {
        if (addr == 0x00)
            joypad = data;
        if (addr == 0x01)
            serialData = data;
        if (addr == 0x02)
        {
            if ((data & 0x81) == 0x81) // TODO: Properly Implement
            {
                char c = (char)serialData;
                Console.Write(c);
                
                serialData = 0xFF;
                serialControl = 0;
            }
        }
        if (addr is >= 0x04 and <= 0x07)
            timer.Write(addr - 0x04, data);
        if (addr == 0x0F)
            interrupts = data;
        if (addr is >= 0x10 and <= 0x26)
            audio[addr - 0x10] = data;
        if (addr is >= 0x30 and <= 0x3F)
            wavePattern[addr - 0x30] = data;
        if (addr is >= 0x40 and <= 0x4B)
            screen[addr - 0x40] = data;
        if (addr == 0x4F)
            vramBankSelector = data;
        if (addr == 0x50)
            bootRomMappingController = data;
        if (addr is >= 0x51 and <= 0x55)
            vramDMA[addr - 0x55] = data;
        if (addr is >= 0x68 and <= 0x6B)
            bgoObjPalettes[addr - 0x68] = data;
        if (addr == 0x70)
            wramBankSelector = data;
        if (addr > 0x70)
            throw new Exception($"{addr} is out of range.");
    }

    public byte Read(ushort addr)
    {
        if (addr == 0x00)
            return joypad;
        if (addr == 0x01)
            return serialData;
        if (addr == 0x02)
            return serialControl;
        if (addr is >= 0x04 and <= 0x07)
            return timer.Read(addr - 0x04);
        if (addr == 0x0F)
            return interrupts;
        if (addr is >= 0x10 and <= 0x26)
            return audio[addr - 0x10];
        if (addr is >= 0x30 and <= 0x3F)
            return wavePattern[addr - 0x30];
        if (addr is >= 0x40 and <= 0x4B)
            return screen[addr - 0x40];
        if (addr == 0x4F)
            return vramBankSelector;
        if (addr == 0x50)
            return bootRomMappingController;
        if (addr is >= 0x51 and <= 0x55)
            return vramDMA[addr - 0x55];
        if (addr is >= 0x68 and <= 0x6B)
            return bgoObjPalettes[addr - 0x68];
        if (addr == 0x70)
            return wramBankSelector;
        throw new Exception($"{addr} is out of range.");
    }

    public void UpdateTimers(int cycles)
    {
        timer.UpdateTimers(cycles);
    }

    private void initStartingValues()
    {
        Write(0x00, 0xCF);
        Write(0x01, 0x00);
        Write(0x02, 0x7E);
        Write(0x04, 0xAB);
        Write(0x05, 0x00);
        Write(0x06, 0x00);
        Write(0x07, 0xF8);
        Write(0x0F, 0xE1);
        Write(0x10, 0x80);
        Write(0x11, 0xBF);
        Write(0x12, 0xF3);
        Write(0x13, 0xFF);
        Write(0x14, 0xBF);
        Write(0x16, 0x3F);
        Write(0x17, 0x00);
        Write(0x18, 0xFF);
        Write(0x19, 0xBF);
        Write(0x1A, 0x7F);
        Write(0x1B, 0xFF);
        Write(0x1C, 0x9F);
        Write(0x1D, 0xFF);
        Write(0x1E, 0xBF);
        Write(0x20, 0xFF);
        Write(0x21, 0x00);
        Write(0x22, 0x00);
        Write(0x23, 0xBF);
        Write(0x24, 0x77);
        Write(0x25, 0xF3);
        Write(0x26, 0xF1);
        Write(0x40, 0x91);
        Write(0x41, 0x85);
        Write(0x42, 0x00);
        Write(0x43, 0x00);
        Write(0x44, 0x90); // TODO: SET BACK TO 0x00
        Write(0x45, 0x00);
        Write(0x46, 0xFF);
        Write(0x47, 0xFC);
        Write(0x4A, 0x00);
        Write(0x4B, 0x00);
    }
}