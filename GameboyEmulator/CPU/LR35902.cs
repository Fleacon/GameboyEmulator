namespace GameboyEmulator;

public class LR35902
{
    public Registers Registers;

    public Bus Bus;

    public LR35902(Bus bus)
    {
        this.Bus = bus;
        Registers = new(this);
    }

    public void clock()
    {
        
    }
}