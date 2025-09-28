namespace GameboyEmulator;

public enum Interrupts : byte
{
    NONE = 0,
    VBLANK = 0b00001,
    LCD    = 0b00010,
    TIMER  = 0b00100,
    SERIAL = 0b01000,
    JOYPAD = 0b10000,
}