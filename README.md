# GameboyEmulator

## Tests

### Blargg's test ROMs

**cpu_instrs**
| Test | Passed? |
|----------------|---------|
| cpu_instrs | **Passed** |

### Mooneye's test ROMs

**mbc1**
| Test | Passed? |
| ----------- | ---------- |
| bits_bank1 | **Passed** |
| bits_bank2 | **Passed** |
| bits_mode | **Passed** |
| bits_ramg | **Passed** |
| multicart_rom_8Mb | Failed |
| ram_64kb | **Passed** |
| ram_256kb | **Passed** |
| rom_1Mb | **Passed** |
| rom_2Mb | **Passed** |
| rom_4Mb | **Passed** |
| rom_8Mb | **Passed** |
| rom_16Mb | **Passed** |
| rom_512kb | **Passed** |

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
