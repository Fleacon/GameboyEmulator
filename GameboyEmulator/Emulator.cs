using GameboyEmulator.CPU;

namespace GameboyEmulator;

public class Emulator
{
    private const int MAXCYCLES = 69905; // cylesPerSec / Frames | 4194304 / 60
    private LR35902 cpu;
    private Bus bus;
    private IO.IO io;

    public Emulator()
    {
        bus = new ();
        cpu = new (bus);
    }
    
    public void Update()
    {
        int cycles = 0;

        while (cycles < MAXCYCLES)
        {
            cycles += cpu.Execute();
            bus.IOregister.UpdateTimers(cycles);
        }
    }
}