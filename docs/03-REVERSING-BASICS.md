# Reversing Basics

Finding the addresses and offsets you need to hack a game.

---

## The Reversing Mindset

You're a detective. The game knows where the player's health is. Your job is to find out.

**Two main approaches:**
1. **Dynamic Analysis** - Run the game, watch what happens (debuggers)
2. **Static Analysis** - Read the code without running it (disassemblers)

---

## Essential Tools

### For Native Games (C++, Unreal)

| Tool | Purpose | Cost |
|------|---------|------|
| **x64dbg** | Debugger, see memory live | Free |
| **IDA Pro/Free** | Disassembler, static analysis | Free/$$$ |
| **Ghidra** | Disassembler by NSA, free IDA alternative | Free |
| **Cheat Engine** | Memory scanner, find values | Free |
| **ReClass.NET** | Visualize structs in memory | Free |

### For .NET/Mono Games (Unity)

| Tool | Purpose |
|------|---------|
| **dnSpy** | Decompile C# to readable code |
| **ILSpy** | Another C# decompiler |
| **IL2CppDumper** | For IL2CPP Unity games |
| **Cpp2IL** | Alternative IL2CPP tool |

---

## Cheat Engine: Finding Values

The most important beginner tool. Find any value in memory.

### Basic Scan Workflow

1. Open Cheat Engine, attach to game
2. **First Scan**: Search for the value you want (health = 100)
3. Go back to game, change the value (take damage, health = 85)
4. **Next Scan**: Search for new value (85)
5. Repeat until you have 1-3 addresses
6. Add to list, modify, see if it works

### Value Types

- **4 Bytes** - Most common for integers (health, ammo, money)
- **Float** - Decimal numbers (position, speed)
- **Double** - Higher precision float
- **2 Bytes** - Older games, some counters
- **1 Byte** - Very small values (level, flags)
- **String** - Player names, etc.

### Unknown Initial Value

Don't know the exact value? Use comparisons:
1. First Scan: "Unknown initial value"
2. Take damage: "Decreased value"
3. Heal: "Increased value"
4. No change: "Unchanged value"

### Pointer Scanning

Values often move between game restarts. To find stable addresses:

1. Find the health address
2. Right-click → "Pointer scan for this address"
3. Restart game, find health again
4. Compare results, keep matching pointers

**Result looks like:**
```
"game.exe"+00123456 → +10 → +48 → +C0 → Health
```

This means:
1. Start at game.exe base
2. Add 0x123456
3. Read pointer, add 0x10
4. Read pointer, add 0x48
5. Read pointer, add 0xC0
6. This is your health address

---

## x64dbg: Live Debugging

Pause the game, step through code, see registers.

### Basic Controls

- **F9** - Run
- **F7** - Step into (enter function calls)
- **F8** - Step over (skip function calls)
- **F2** - Toggle breakpoint
- **Ctrl+G** - Go to address

### Breakpoints

**Software breakpoint (F2):**
- Replaces instruction with `INT 3`
- Game stops when it hits this address
- Good for finding what code accesses a value

**Hardware breakpoint:**
- Uses CPU debug registers (DR0-DR3, only 4 available)
- Doesn't modify code (stealthier)
- Can trigger on Read, Write, or Execute

### Finding What Writes to an Address

In Cheat Engine:
1. Find the health address
2. Right-click → "Find out what writes to this address"
3. Take damage in game
4. See the instruction that modified health

Example result:
```asm
0x7FF61234ABCD: mov [rcx+0x48], eax
```

This tells you:
- `rcx` is a pointer to some object (probably the player)
- Health is at offset `0x48` in that object
- `eax` contains the new health value

---

## Assembly Basics

You need to read assembly to understand what you find.

### Registers (x64)

```
General Purpose (64-bit):
RAX, RBX, RCX, RDX - General use, function args/returns
RSI, RDI - Source/Destination (string operations)
RBP - Base pointer (stack frame)
RSP - Stack pointer (top of stack)
R8-R15 - Additional general purpose

Lower portions:
RAX → EAX (32-bit) → AX (16-bit) → AL/AH (8-bit)
```

### Common Instructions

```asm
; Data movement
mov rax, rbx        ; Copy rbx to rax
mov [rax], rbx      ; Write rbx to memory at address in rax
mov rax, [rbx]      ; Read memory at rbx into rax
mov rax, [rbx+0x48] ; Read memory at rbx+0x48 into rax
lea rax, [rbx+0x10] ; Load address (rbx+0x10) into rax (no dereference)

; Arithmetic
add rax, 10         ; rax = rax + 10
sub rax, rbx        ; rax = rax - rbx
inc rax             ; rax++
dec rax             ; rax--
imul rax, rbx       ; rax = rax * rbx

; Comparison and jumps
cmp rax, rbx        ; Compare rax and rbx (sets flags)
test rax, rax       ; AND rax with itself (check if zero)
jmp label           ; Unconditional jump
je label            ; Jump if equal (ZF=1)
jne label           ; Jump if not equal (ZF=0)
jg/jl               ; Jump if greater/less (signed)
ja/jb               ; Jump if above/below (unsigned)

; Function calls
call address        ; Call function, push return address
ret                 ; Return from function
push rax            ; Push rax onto stack
pop rax             ; Pop from stack into rax

; Floating point (SSE)
movss xmm0, [rax]   ; Load float
addss xmm0, xmm1    ; Add floats
mulss xmm0, xmm1    ; Multiply floats
```

### Calling Conventions (x64 Windows)

```
Arguments: RCX, RDX, R8, R9, then stack
Return: RAX (int) or XMM0 (float)
Preserved: RBX, RBP, RDI, RSI, R12-R15
```

Function call example:
```cpp
// C++ code
player->TakeDamage(50, attacker, true);

// Assembly
mov rcx, [playerPtr]  ; this pointer (first arg)
mov edx, 50           ; damage (second arg)
mov r8, [attackerPtr] ; attacker (third arg)
mov r9d, 1            ; true (fourth arg)
call TakeDamage
```

---

## IDA / Ghidra: Static Analysis

Read the game's code without running it.

### Finding Functions

**By string:**
1. View → Open subviews → Strings
2. Search for text you see in game
3. Cross-reference to find code that uses it

**By imports:**
1. View → Imports
2. Find interesting APIs (LoadLibrary, CreateThread)
3. Cross-reference to see what calls them

**By patterns:**
Look for common patterns:
- Constructor: allocates memory, sets vtable
- Destructor: frees memory
- Getter: returns a member variable
- Setter: writes to a member variable

### Identifying Classes

Vtables help identify classes:
```
.rdata:00007FF600123000 vtable_Player:
    dq offset Player_GetHealth
    dq offset Player_SetHealth
    dq offset Player_TakeDamage
    dq offset Player_Die
```

### Reading Decompiled Code

Ghidra and IDA can generate pseudo-C:

```c
void __fastcall Player::TakeDamage(Player *this, int damage, Entity *attacker) {
    if (this->godMode) return;
    
    int newHealth = this->health - damage;
    if (newHealth < 0) newHealth = 0;
    this->health = newHealth;
    
    if (newHealth == 0) {
        Player::Die(this, attacker);
    }
}
```

From this you learn:
- `godMode` flag exists (offset needed)
- `health` is a member variable
- `Die` function exists

---

## dnSpy: Unity/Mono Games

For games like Lethal Company, dnSpy is your primary tool.

### Opening Assemblies

1. Find `[GameName]_Data/Managed/` folder
2. Open `Assembly-CSharp.dll` in dnSpy
3. Browse namespaces and classes

### Finding What You Need

Lethal Company structure example:
```
Assembly-CSharp.dll
├── StartOfRound
├── GameNetworkManager  
├── PlayerControllerB   ← Player class
│   ├── health
│   ├── sprintMeter
│   ├── TakeDamage()
│   └── KillPlayer()
├── EnemyAI            ← Base enemy class
│   ├── currentBehaviourStateIndex
│   └── KillEnemy()
├── HUDManager
│   ├── DisplayTip()
│   └── AddNewScrapFoundToDisplay()
└── Terminal
    ├── groupCredits
    └── SetItemSales()
```

### Practical Example: Finding Health

1. Search (Ctrl+Shift+K) for "health"
2. Find `PlayerControllerB.health`
3. See it's an `int` field
4. Right-click → Analyze → Find where it's written to
5. Find `DamagePlayer` method

```csharp
public void DamagePlayer(int damageNumber, bool hasDamageSFX = true, 
    bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown)
{
    if (!AllowPlayerDeath()) return;
    
    health -= damageNumber;
    if (health < 0) health = 0;
    
    // Play damage effects...
    
    if (health <= 0 && !isPlayerDead) {
        KillPlayer(velocityToSet, spawnBody, causeOfDeath);
    }
}
```

Now you know:
- You can hook `DamagePlayer` to prevent damage
- Or hook `AllowPlayerDeath` to always return false
- Or just write to `health` field directly

---

## Pattern Scanning

Addresses change, but code patterns don't.

### What is a Pattern?

A sequence of bytes that uniquely identifies code:

```cpp
// This function always starts with these bytes
// 48 89 5C 24 08  mov [rsp+8], rbx
// 57              push rdi
// 48 83 EC 20     sub rsp, 0x20

const char* pattern = "48 89 5C 24 08 57 48 83 EC 20";
```

### Basic Pattern Scanner

```cpp
uintptr_t FindPattern(HMODULE module, const char* pattern) {
    uint8_t* base = reinterpret_cast<uint8_t*>(module);
    size_t size = GetModuleSize(module);
    
    std::vector<int> bytes = ParsePattern(pattern);
    
    for (size_t i = 0; i < size - bytes.size(); i++) {
        bool found = true;
        for (size_t j = 0; j < bytes.size(); j++) {
            if (bytes[j] != -1 && base[i + j] != bytes[j]) {
                found = false;
                break;
            }
        }
        if (found) return reinterpret_cast<uintptr_t>(&base[i]);
    }
    return 0;
}
```

### Wildcards

Use `??` for bytes that might change (like offsets):

```cpp
// Original: 48 8B 05 AB CD 12 00  mov rax, [rel 0x12CDAB]
// Pattern:  48 8B 05 ?? ?? ?? ??
// The offset changes, but the instruction format doesn't
```

### IDA Pattern Generation

In IDA, select bytes → Edit → Export data → C array, then replace varying bytes with `??`.

---

## ReClass.NET: Visualizing Structures

Drag-and-drop struct building.

### Workflow

1. Attach to game
2. Paste an address (player pointer)
3. See raw memory with type interpretation
4. Name fields as you discover them
5. Export to C++ struct

### Example Output

```cpp
class PlayerControllerB {
public:
    char pad_0000[0x48];      // 0x0000
    int32_t health;            // 0x0048
    float sprintMeter;         // 0x004C
    bool isPlayerDead;         // 0x0050
    char pad_0051[0x7];       // 0x0051
    Vector3 position;          // 0x0058
    // ... continue mapping
};
```

---

## Practical Workflow Example

**Goal:** Find player speed multiplier

1. **Cheat Engine:** Scan for float ~1.0, run fast, scan for >1.0, walk slow, scan for <1.0
2. **Found address.** Set breakpoint on write.
3. **x64dbg:** See what instruction writes to it:
   ```asm
   movss [rcx+0x234], xmm0
   ```
4. **Analyze:** `rcx` is the player, offset `0x234` is speed multiplier
5. **dnSpy:** Find `PlayerControllerB`, look for field at offset 0x234, confirm it's `movementSpeed`
6. **Implement:** Read player pointer from Mono, write to offset 0x234

---

## Tips

- **Start simple** - Find exact values first (health, ammo)
- **Document everything** - Write down offsets, addresses, what they do
- **Game updates break things** - After updates, re-verify your offsets
- **Learn one game deeply** - Better than shallow knowledge of many
- **Read write-ups** - UnKnoWnCheaTs, GuidedHacking have tutorials

---

## Next Steps

1. Download Cheat Engine and practice on simple games first
2. Open `Assembly-CSharp.dll` in dnSpy for Lethal Company
3. Find `PlayerControllerB` and explore its fields
4. Read `04-HOOKING-TECHNIQUES.md` - Intercepting game functions
