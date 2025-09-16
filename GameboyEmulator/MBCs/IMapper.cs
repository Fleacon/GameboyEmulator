namespace GameboyEmulator.MBCs;

public interface IMapper
{
    public abstract byte Read(Cartridge c, ushort addr);
    public abstract void Write(Cartridge c, ushort addr, byte value);
}