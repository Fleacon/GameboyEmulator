namespace GameboyEmulator;

public class LR35902
{
    // Registers
    // Can be accessed as one 16 Bit or two 8 Bit Value
    private ushort AF; // Lower 8 Bits acts as Flag Register
    private ushort BC;
    private ushort DE;
    private ushort HL;
    private ushort SP;
    private ushort PC;

    private Action[] instructionTable = new Action[256];
}