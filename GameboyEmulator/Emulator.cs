using GameboyEmulator.CPU;

namespace GameboyEmulator;

public class Emulator
{
    private const int MAXCYCLES = 69905; // cylesPerSec / Frames | 4194304 / 60
    public LR35902 Cpu;
    public Bus Bus;
    public IO.IO Io;

    public Emulator()
    {
        Bus = new ();
        Cpu = new (Bus);
    }
    
    public void Update()
    {
        int cycles = 0;

        while (cycles < MAXCYCLES)
        {
            cycles += Cpu.Execute();
            Bus.IOregister.UpdateTimers(cycles);
        }
    }

    public void LoadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }
        
        byte[] rom = File.ReadAllBytes(filePath);
        
        var cartridge = new Cartridge();
        cartridge.LoadGame(rom);
        Bus.InsertCartridge(cartridge);
        cartridge.PrintInfo();
    }
}