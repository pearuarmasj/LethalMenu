# Windows Internals for Game Hacking

Understanding how Windows manages processes, memory, and DLLs is essential for game hacking.

---

## Process Memory Layout

Every Windows process has its own virtual address space (on x64, theoretically 256TB, practically much less).

```
High Addresses (0x7FFFFFFFFFFF)
┌─────────────────────────────┐
│    Kernel Space             │  (Not accessible from user mode)
├─────────────────────────────┤
│    Stack                    │  Grows downward, local variables
├─────────────────────────────┤
│    Memory-Mapped Files      │  Shared memory, file views
├─────────────────────────────┤
│    Heap                     │  Dynamic allocations (new, malloc)
├─────────────────────────────┤
│    DLLs                     │  Loaded libraries
├─────────────────────────────┤
│    Executable (.exe)        │  Game code and data
├─────────────────────────────┤
│    Reserved/Guard Pages     │
└─────────────────────────────┘
Low Addresses (0x000000000000)
```

### Key Regions

**Executable Sections (.exe or .dll):**
- `.text` - Code (executable, read-only)
- `.data` - Initialized global/static variables
- `.rdata` - Read-only data (strings, vtables)
- `.bss` - Uninitialized data

**Stack:**
- Local variables
- Return addresses
- Function parameters (some)
- Grows downward (high to low addresses)

**Heap:**
- Dynamic allocations
- Objects created with `new`
- Managed by the runtime

---

## Virtual Memory and Page Protection

Memory is divided into pages (4KB on x86/x64). Each page has protection flags:

```cpp
// From Windows headers
PAGE_NOACCESS          0x01  // Can't read/write/execute
PAGE_READONLY          0x02  // Read only
PAGE_READWRITE         0x04  // Read and write
PAGE_EXECUTE           0x10  // Execute only
PAGE_EXECUTE_READ      0x20  // Execute and read
PAGE_EXECUTE_READWRITE 0x40  // Execute, read, and write
```

### Querying Memory Protection

```cpp
#include <Windows.h>

MEMORY_BASIC_INFORMATION mbi;
VirtualQuery(address, &mbi, sizeof(mbi));

// mbi.State: MEM_COMMIT, MEM_FREE, MEM_RESERVE
// mbi.Protect: PAGE_READWRITE, etc.
// mbi.BaseAddress: Start of region
// mbi.RegionSize: Size of region
```

### Changing Memory Protection

To write to read-only memory (like patching code):

```cpp
DWORD oldProtect;
VirtualProtect(address, size, PAGE_EXECUTE_READWRITE, &oldProtect);
// Now you can write to the memory
memcpy(address, newBytes, size);
// Restore original protection
VirtualProtect(address, size, oldProtect, &oldProtect);
```

---

## DLLs (Dynamic Link Libraries)

DLLs are shared libraries loaded into a process. Game cheats are typically DLLs injected into the game process.

### DLL Structure

```cpp
// Every DLL has an entry point
BOOL APIENTRY DllMain(
    HMODULE hModule,    // Handle to this DLL
    DWORD reason,       // Why we're being called
    LPVOID reserved
) {
    switch (reason) {
    case DLL_PROCESS_ATTACH:
        // DLL just loaded - initialize your cheat here
        DisableThreadLibraryCalls(hModule);
        CreateThread(nullptr, 0, MainThread, hModule, 0, nullptr);
        break;
    case DLL_PROCESS_DETACH:
        // DLL unloading - cleanup
        break;
    }
    return TRUE;
}
```

### DLL_PROCESS_ATTACH vs Thread Creation

**Never do heavy work directly in DllMain!** The loader lock is held during DllMain, which can cause deadlocks.

```cpp
// BAD - can deadlock
BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID reserved) {
    if (reason == DLL_PROCESS_ATTACH) {
        MessageBoxA(nullptr, "Hello", "Test", MB_OK);  // Might deadlock!
    }
    return TRUE;
}

// GOOD - spawn a thread for initialization
BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID reserved) {
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
        CreateThread(nullptr, 0, [](LPVOID param) -> DWORD {
            // Safe to do anything here
            Initialize((HMODULE)param);
            return 0;
        }, hModule, 0, nullptr);
    }
    return TRUE;
}
```

---

## Getting Module Handles and Addresses

### GetModuleHandle

Get base address of a loaded module:

```cpp
// Get handle to the main executable
HMODULE gameBase = GetModuleHandleA(nullptr);

// Get handle to a specific DLL
HMODULE d3d11 = GetModuleHandleA("d3d11.dll");
HMODULE unityPlayer = GetModuleHandleA("UnityPlayer.dll");
HMODULE mono = GetModuleHandleA("mono-2.0-bdwgc.dll");

// The handle IS the base address
uintptr_t baseAddr = reinterpret_cast<uintptr_t>(gameBase);
```

### GetProcAddress

Get address of an exported function:

```cpp
HMODULE mono = GetModuleHandleA("mono-2.0-bdwgc.dll");

// Get a specific function
using mono_get_root_domain_t = void* (*)();
auto mono_get_root_domain = reinterpret_cast<mono_get_root_domain_t>(
    GetProcAddress(mono, "mono_get_root_domain")
);

// Now you can call it
void* domain = mono_get_root_domain();
```

### Pattern for Loading Many Functions

```cpp
#define LOAD_FUNC(module, name) \
    fn_##name = reinterpret_cast<name##_t>(GetProcAddress(module, #name)); \
    if (!fn_##name) return false;

// Usage
LOAD_FUNC(mono, mono_get_root_domain);
LOAD_FUNC(mono, mono_class_from_name);
// etc.
```

---

## DLL Injection Methods

How do you get your DLL into the game process?

### 1. LoadLibrary Injection (Most Common)

```cpp
// Injector (separate program):
// 1. Get handle to target process
HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);

// 2. Allocate memory in target for DLL path
void* remotePath = VirtualAllocEx(hProcess, nullptr, pathLen, 
    MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

// 3. Write DLL path to target
WriteProcessMemory(hProcess, remotePath, dllPath, pathLen, nullptr);

// 4. Get address of LoadLibraryA
FARPROC loadLibAddr = GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryA");

// 5. Create remote thread that calls LoadLibraryA with our path
CreateRemoteThread(hProcess, nullptr, 0, 
    (LPTHREAD_START_ROUTINE)loadLibAddr, remotePath, 0, nullptr);
```

### 2. Manual Mapping

More stealthy - manually loads the DLL without using LoadLibrary:
- Parse PE headers
- Allocate memory for sections
- Copy sections
- Fix relocations
- Resolve imports
- Call DllMain manually

More complex but leaves fewer traces.

### 3. DLL Hijacking

Place your DLL where the game will load it naturally:
- Name it after a DLL the game imports
- Put it in the game directory
- Game loads your DLL thinking it's legitimate

---

## The PE (Portable Executable) Format

EXEs and DLLs are PE files. Understanding the format helps with manual mapping and finding vtables.

```
┌─────────────────────────────┐
│    DOS Header               │  Starts with "MZ"
├─────────────────────────────┤
│    DOS Stub                 │  "This program cannot be run in DOS mode"
├─────────────────────────────┤
│    PE Signature             │  "PE\0\0"
├─────────────────────────────┤
│    COFF File Header         │  Number of sections, characteristics
├─────────────────────────────┤
│    Optional Header          │  Entry point, image base, section alignment
├─────────────────────────────┤
│    Section Headers          │  .text, .data, .rdata, etc.
├─────────────────────────────┤
│    Section Data             │  Actual code and data
└─────────────────────────────┘
```

### Finding the Entry Point

```cpp
HMODULE base = GetModuleHandleA(nullptr);

// DOS header at base
auto* dosHeader = reinterpret_cast<IMAGE_DOS_HEADER*>(base);

// PE header at base + e_lfanew
auto* ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS*>(
    reinterpret_cast<uint8_t*>(base) + dosHeader->e_lfanew
);

// Entry point RVA (Relative Virtual Address)
DWORD entryPointRVA = ntHeaders->OptionalHeader.AddressOfEntryPoint;
void* entryPoint = reinterpret_cast<uint8_t*>(base) + entryPointRVA;
```

---

## Threads

### Creating Threads

```cpp
// Simple thread creation
DWORD WINAPI MyThread(LPVOID param) {
    // Thread code here
    return 0;
}

HANDLE hThread = CreateThread(
    nullptr,    // Default security
    0,          // Default stack size
    MyThread,   // Function to run
    nullptr,    // Parameter to pass
    0,          // Start immediately
    nullptr     // Don't need thread ID
);

// Wait for thread to complete
WaitForSingleObject(hThread, INFINITE);
CloseHandle(hThread);
```

### Thread Safety

Multiple threads accessing same data = potential bugs.

```cpp
// BAD - race condition
int g_counter = 0;
void Increment() {
    g_counter++;  // Not atomic!
}

// GOOD - use a critical section
CRITICAL_SECTION g_cs;
int g_counter = 0;

void Init() {
    InitializeCriticalSection(&g_cs);
}

void Increment() {
    EnterCriticalSection(&g_cs);
    g_counter++;
    LeaveCriticalSection(&g_cs);
}

// Or use std::mutex
#include <mutex>
std::mutex g_mutex;

void Increment() {
    std::lock_guard<std::mutex> lock(g_mutex);
    g_counter++;
}
```

---

## Structured Exception Handling (SEH)

Windows mechanism for handling crashes.

```cpp
__try {
    // Dangerous code
    int* ptr = nullptr;
    *ptr = 42;  // Access violation!
}
__except (EXCEPTION_EXECUTE_HANDLER) {
    // Handle the crash
    printf("Caught exception!\n");
}

// Get exception info
__try {
    // ...
}
__except (MyFilter(GetExceptionInformation())) {
    // ...
}

LONG MyFilter(EXCEPTION_POINTERS* ep) {
    DWORD code = ep->ExceptionRecord->ExceptionCode;
    void* addr = ep->ExceptionRecord->ExceptionAddress;
    
    if (code == EXCEPTION_ACCESS_VIOLATION) {
        // Log it, maybe recover
        return EXCEPTION_EXECUTE_HANDLER;
    }
    return EXCEPTION_CONTINUE_SEARCH;
}
```

**Important:** SEH and C++ exceptions don't mix well. Functions with `__try/__except` can't have objects with destructors.

---

## Common Windows API Functions for Hacking

```cpp
// Process enumeration
CreateToolhelp32Snapshot()
Process32First() / Process32Next()

// Module enumeration
Module32First() / Module32Next()
EnumProcessModules()

// Memory operations
ReadProcessMemory()    // Read from another process
WriteProcessMemory()   // Write to another process
VirtualAlloc()         // Allocate memory
VirtualFree()          // Free memory
VirtualProtect()       // Change memory protection
VirtualQuery()         // Query memory information

// Module operations
GetModuleHandleA()     // Get base address of loaded module
GetProcAddress()       // Get address of exported function
LoadLibraryA()         // Load a DLL

// Thread operations
CreateThread()
CreateRemoteThread()   // Create thread in another process
SuspendThread()
ResumeThread()

// Handle operations
OpenProcess()
CloseHandle()
DuplicateHandle()
```

---

## Base Address and ASLR

ASLR (Address Space Layout Randomization) randomizes where modules load each time.

```cpp
// Offsets from the base are constant
// Base addresses change every launch

HMODULE base = GetModuleHandleA("game.exe");
// base might be 0x7FF600000000 one launch, 0x7FF700000000 another

// But relative offset to player pointer is always the same
constexpr uintptr_t PLAYER_OFFSET = 0x123456;
uintptr_t playerPtr = reinterpret_cast<uintptr_t>(base) + PLAYER_OFFSET;
```

This is why cheats use **base + offset** rather than hardcoded addresses.

---

## Practical Example: Finding a Module's Size

```cpp
size_t GetModuleSize(HMODULE module) {
    if (!module) return 0;
    
    auto* dosHeader = reinterpret_cast<IMAGE_DOS_HEADER*>(module);
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE) return 0;
    
    auto* ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS*>(
        reinterpret_cast<uint8_t*>(module) + dosHeader->e_lfanew
    );
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE) return 0;
    
    return ntHeaders->OptionalHeader.SizeOfImage;
}
```

---

## Next Steps

1. Read `03-REVERSING-BASICS.md` - How to find the offsets you need
2. Read `04-HOOKING-TECHNIQUES.md` - Intercepting game functions
3. Practice: Write a simple injector that loads your DLL into notepad
