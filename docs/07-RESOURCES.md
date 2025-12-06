# Resources & Learning Path

Curated resources for learning game hacking.

---

## Recommended Learning Order

1. **C++ Fundamentals** - Can't hack if you can't code
2. **Windows Programming** - DLLs, WinAPI, processes
3. **Assembly Basics** - Read what the debugger shows you
4. **Cheat Engine** - Find values, learn memory layout
5. **Reversing Tools** - x64dbg, IDA/Ghidra, dnSpy
6. **Simple Cheats** - Trainers for single-player games
7. **Injection** - Get your code into other processes
8. **Hooking** - Intercept functions
9. **Unity Specifics** - Mono runtime, common patterns
10. **Advanced Topics** - Anti-cheat bypass, kernel drivers

---

## Free Online Resources

### C++ Learning

| Resource | Description |
|----------|-------------|
| [learncpp.com](https://www.learncpp.com/) | Comprehensive C++ tutorial |
| [cppreference.com](https://en.cppreference.com/) | C++ reference documentation |
| [Compiler Explorer](https://godbolt.org/) | See what your code compiles to |

### Windows Internals

| Resource | Description |
|----------|-------------|
| [Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/) | Official Win32 documentation |
| [Windows Internals Book](https://docs.microsoft.com/en-us/sysinternals/resources/windows-internals) | Deep dive (not free, but essential) |
| [ReactOS Source](https://github.com/nicola-lunghi/reactos) | Open-source Windows reimplementation |

### Reversing & Game Hacking

| Resource | Description |
|----------|-------------|
| [GuidedHacking](https://guidedhacking.com/) | Game hacking tutorials, forums |
| [UnKnoWnCheaTs](https://unknowncheats.me/) | Massive cheat development community |
| [x64dbg](https://x64dbg.com/) | Free debugger |
| [Ghidra](https://ghidra-sre.org/) | Free disassembler from NSA |
| [Cheat Engine](https://cheatengine.org/) | Memory scanner |

### Unity Specific

| Resource | Description |
|----------|-------------|
| [dnSpy](https://github.com/dnSpy/dnSpy) | .NET decompiler (archived but works) |
| [Unity Docs](https://docs.unity3d.com/) | Official Unity documentation |
| [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) | For IL2CPP games |

---

## YouTube Channels

| Channel | Focus |
|---------|-------|
| **Guided Hacking** | Game hacking tutorials, beginner friendly |
| **Stephen Chapman** | C++ game hacking |
| **Cazz** | Unity hacking, BepInEx |
| **LiveOverflow** | Security research, CTF |
| **Low Level Learning** | Systems programming |
| **The Cherno** | C++ game development |

---

## Books

### Must-Read

1. **"Windows Internals" by Mark Russinovich** - How Windows actually works
2. **"Practical Malware Analysis"** - Reversing techniques (applicable to games)
3. **"The IDA Pro Book"** - Master disassembly

### Helpful

4. **"Reverse Engineering for Beginners"** by Dennis Yurichev (FREE online)
5. **"Game Hacking" by Nick Cano** - Specifically about game cheats
6. **"Effective Modern C++"** - Write better C++

---

## Tools to Download

### Essential (Free)

- **Visual Studio Community** - IDE and compiler
- **x64dbg** - Debugger
- **Cheat Engine** - Memory scanner
- **Ghidra** - Disassembler
- **dnSpy** - .NET decompiler
- **Process Hacker** - Better task manager
- **ReClass.NET** - Struct visualization

### Nice to Have

- **IDA Pro** - Industry standard disassembler (expensive, free version limited)
- **HxD** - Hex editor
- **Wireshark** - Network analysis
- **API Monitor** - Watch API calls

---

## Practice Targets

### Beginner (No Anti-Cheat)

1. **Notepad** - Just write to its memory
2. **Minesweeper** - Find mine locations
3. **Assault Cube** - Classic practice FPS
4. **Pwn Adventure 3** - CTF game made for hacking
5. **Terraria** - Simple, no anti-cheat

### Intermediate

6. **Single-player Unity games** - Practice Mono
7. **Source Engine games** (offline) - Well documented
8. **Dark Souls** (offline) - Popular target

### Do NOT Target (as a beginner)

- Any game with kernel anti-cheat (EAC, BattlEye, Vanguard)
- Online competitive games
- Games where cheating = ban

---

## Specific Skills to Develop

### Memory

- [ ] Understand virtual memory and paging
- [ ] Read/write process memory
- [ ] Find static addresses vs dynamic
- [ ] Follow pointer chains
- [ ] Pattern scan for signatures

### Assembly

- [ ] Read basic x86/x64 assembly
- [ ] Understand calling conventions
- [ ] Recognize common patterns (loops, if/else, function calls)
- [ ] Find what writes to an address
- [ ] Patch instructions

### Reversing

- [ ] Navigate a disassembler
- [ ] Find functions by strings
- [ ] Identify function purposes
- [ ] Map out class structures
- [ ] Find vtables

### Hooking

- [ ] Write a basic DLL
- [ ] Inject into a process
- [ ] Hook an API function
- [ ] Hook a game function
- [ ] Handle calling conventions

### Unity

- [ ] Navigate decompiled C#
- [ ] Find MonoBehaviour classes
- [ ] Access fields via Mono API
- [ ] Hook C# methods
- [ ] Understand Unity's object model

---

## Community & Forums

| Forum | Focus |
|-------|-------|
| [UnKnoWnCheaTs](https://unknowncheats.me/) | General game hacking |
| [GuidedHacking](https://guidedhacking.com/forums/) | Tutorials, beginner help |
| [r/REGames](https://reddit.com/r/REGames) | Game reversing |
| [r/ReverseEngineering](https://reddit.com/r/ReverseEngineering) | General reversing |

---

## Project Ideas (Progressive Difficulty)

### Level 1: External
Write a separate program that reads/writes game memory:
- [ ] Health editor for a simple game
- [ ] Position teleporter
- [ ] Speed modifier

### Level 2: Internal DLL
Inject a DLL that runs inside the game:
- [ ] Basic trainer (god mode, infinite ammo)
- [ ] Simple ESP (draw boxes)
- [ ] Console-based menu

### Level 3: Overlay
Add graphical UI:
- [ ] ImGui menu
- [ ] D3D/OpenGL hooking
- [ ] In-game visuals (ESP boxes, lines)

### Level 4: Advanced
- [ ] Prediction/aimbot (learn game math)
- [ ] Full SDK dump (map all classes)
- [ ] Network manipulation
- [ ] Signature-based updates (survive patches)

---

## Common Mistakes to Avoid

1. **Skipping fundamentals** - Learn C++ properly first
2. **Copy-pasting without understanding** - Won't help you learn
3. **Targeting anti-cheat games early** - Get banned, learn nothing
4. **Not using SEH** - Crashes = game closes = frustration
5. **Hardcoding addresses** - Updates break everything
6. **Not documenting** - You'll forget what offsets do
7. **Over-engineering** - Start simple, add complexity as needed

---

## Final Advice

1. **Be patient** - This takes months/years to master
2. **Build projects** - Reading tutorials isn't enough
3. **Read other people's code** - UnKnoWnCheaTs has tons of open source
4. **Ask questions** - Forums exist for this
5. **Document your findings** - Future you will thank present you
6. **Respect anti-cheat** - Don't cheat in competitive games
7. **Have fun** - This should be interesting, not a chore

---

## Quick Reference Cards

### Cheat Engine Scan Types

| Type | Use When |
|------|----------|
| Exact Value | You know the exact number |
| Unknown Initial | Don't know the value |
| Increased/Decreased | Value changed but don't know to what |
| Changed/Unchanged | Binary: did it change or not |

### Common Offsets to Look For

| Field | Typical Type | Common Names |
|-------|--------------|--------------|
| Health | int/float | health, hp, hitpoints |
| Ammo | int | ammo, bullets, rounds |
| Position | Vector3 (3 floats) | position, pos, transform |
| Speed | float | speed, movementSpeed, velocity |
| Money | int | money, gold, credits, cash |

### MinHook Return Codes

| Code | Meaning |
|------|---------|
| MH_OK | Success |
| MH_ERROR_NOT_INITIALIZED | Call MH_Initialize first |
| MH_ERROR_ALREADY_INITIALIZED | Already initialized |
| MH_ERROR_NOT_EXECUTABLE | Target isn't executable memory |
| MH_ERROR_UNSUPPORTED_FUNCTION | Can't hook this function |
| MH_ERROR_MEMORY_ALLOC | Out of memory |
| MH_ERROR_ENABLED | Hook already enabled |

---

Good luck. Start small, build up.
