# GameboyEmulator

## Tests

### Blargg's test ROMs

**cpu_instrs**
| Test | Passed? |
|----------------|---------|
| 01-special | **Passed** |
| 02-interrupts | **Passed** |
| 03-op sp,hl | **Passed** |
| 04-op r,imm | **Passed** |
| 05-op rp.gb | **Passed** |
| 06-ld r,r | **Passed** |
| 07-jr,jp,call,ret,rst | **Passed** |
| 08-mis instrs | **Passed** |
| 09-op r,r | **Passed** |
| 10-bit ops | Failed |
| 11-op a,(hl) | Failed |

## Resourcess

- https://gbdev.io/pandocs/About.html | General overview
- https://gekkio.fi/files/gb-docs/gbctr.pdf | Detailed overview
- https://github.com/OneLoneCoder/olcNES | Used as guide for Emulation
- https://github.com/rockytriton/LLD_gbemu | Reference for Gameboy Emulator specifically
- https://izik1.github.io/gbops | Instruction Sets
- https://rgbds.gbdev.io/docs/master/gbz80.7 | Opcode Reference
- https://github.com/retrio/gb-test-roms/tree/master/cpu_instrs | Blarrg's Test Roms for CPU Instruction Behavior
- http://www.codeslinger.co.uk/pages/projects/gameboy/beginning.html | Main Loop, Clock, Timers
- https://www.reddit.com/r/EmuDev | Specific Questions

### Software used

- https://bgb.bircd.org | Emulator with debugger to compare with
- https://github.com/Rodrigodd/gameroy | Other Emulator with debugger
- https://hexed.it | Hex Editor
- https://github.com/robert/gameboy-doctor/tree/master | Gameboy Doctor for diagnosing Issues

### AI

- ChatGPT | General Questions
- Gemini | Check if Instructions implemented correctly (sometimes worked)
- Perplexity | Questions about the Gameboy Architecture
- Claude | Generating both Instruction Tables
