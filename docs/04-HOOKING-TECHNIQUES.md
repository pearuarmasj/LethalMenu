# Hooking Techniques

Intercepting game functions to modify behavior.

---

## What is Hooking?

Hooking = redirecting execution flow. When the game calls function X, your code runs instead (or first).

```
Normal:          Game Code → OriginalFunction → Result
Hooked:          Game Code → YourFunction → (maybe) OriginalFunction → Result
```

---

## Types of Hooks

### 1. Inline/Detour Hooks

Modify the first bytes of a function to jump to your code.

**Before:**
```asm
OriginalFunction:
    push rbp              ; 55
    mov rbp, rsp          ; 48 89 E5
    sub rsp, 0x20         ; 48 83 EC 20
    ; ... rest of function
```

**After:**
```asm
OriginalFunction:
    jmp YourFunction      ; E9 XX XX XX XX (5 bytes)
    nop                   ; Padding if needed
    ; ... rest overwritten

YourFunction:
    ; Your code here
    ; Optionally call trampoline (original function)
```

### 2. VTable Hooks

Replace function pointers in virtual tables.

```cpp
// Every class with virtual functions has a vtable
class IUnknown {
public:
    virtual HRESULT QueryInterface(...) = 0;  // vtable[0]
    virtual ULONG AddRef() = 0;               // vtable[1]
    virtual ULONG Release() = 0;              // vtable[2]
};

// D3D11 device swap chain vtable:
// [0] QueryInterface
// [1] AddRef  
// [2] Release
// ...
// [8] Present  ← We hook this
// ...
// [13] ResizeBuffers ← And this
```

### 3. IAT (Import Address Table) Hooks

Modify the import table to redirect API calls.

### 4. Hardware Breakpoint Hooks

Use CPU debug registers to intercept execution.

---

## MinHook Library

We use MinHook for inline hooks. It handles the complexity.

### Basic Usage

```cpp
#include <MinHook.h>

// Original function pointer (to call the real function)
using Present_t = HRESULT(WINAPI*)(IDXGISwapChain*, UINT, UINT);
Present_t OriginalPresent = nullptr;

// Your hook function
HRESULT WINAPI HookedPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags) {
    // Your code runs here (draw ImGui, etc.)
    RenderOverlay(swapChain);
    
    // Call original function
    return OriginalPresent(swapChain, syncInterval, flags);
}

// Setup
bool SetupHook() {
    // Initialize MinHook
    if (MH_Initialize() != MH_OK) return false;
    
    // Get the address to hook
    void* presentAddr = GetPresentAddress();  // You need to find this
    
    // Create the hook
    if (MH_CreateHook(presentAddr, &HookedPresent, 
        reinterpret_cast<void**>(&OriginalPresent)) != MH_OK) {
        return false;
    }
    
    // Enable it
    if (MH_EnableHook(presentAddr) != MH_OK) return false;
    
    return true;
}

// Cleanup
void RemoveHook() {
    MH_DisableHook(MH_ALL_HOOKS);
    MH_Uninitialize();
}
```

### How MinHook Works

1. **Allocates a trampoline** - A small code cave near the target
2. **Copies original bytes** - The bytes that will be overwritten
3. **Writes a jump** - At the start of the original function
4. **Trampoline contains:**
   - Original bytes (so the function can still work)
   - Jump back to original + offset (continue original function)

```
┌─────────────────┐     ┌──────────────────┐     ┌────────────────────┐
│ Original Func   │     │ Your Hook        │     │ Trampoline         │
├─────────────────┤     ├──────────────────┤     ├────────────────────┤
│ jmp HookedFunc ─┼────►│ // Your code     │     │ (original bytes)   │
│ (overwritten)   │     │ ...              │     │ push rbp           │
│                 │     │ // Call original │     │ mov rbp, rsp       │
│ (rest of func)◄─┼─────┼─OriginalPresent()├────►│ jmp Original+5  ───┼─┐
│    ...          │  ┌──┼─ return result   │     └────────────────────┘ │
│    ...          │◄─┘  └──────────────────┘                            │
│    ret          │◄────────────────────────────────────────────────────┘
└─────────────────┘
```

---

## D3D11 VTable Hook (What We Use)

Finding and hooking DirectX functions.

### Method 1: Dummy Device

Create a throwaway D3D11 device to get vtable addresses:

```cpp
void* GetPresent() {
    // Create a dummy window
    HWND hwnd = CreateWindowExA(0, "STATIC", "", WS_OVERLAPPEDWINDOW,
        0, 0, 100, 100, nullptr, nullptr, nullptr, nullptr);
    
    // Setup swap chain description
    DXGI_SWAP_CHAIN_DESC sd = {};
    sd.BufferCount = 1;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = hwnd;
    sd.SampleDesc.Count = 1;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
    
    // Create device and swap chain
    IDXGISwapChain* swapChain;
    ID3D11Device* device;
    ID3D11DeviceContext* context;
    
    D3D11CreateDeviceAndSwapChain(
        nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
        nullptr, 0, D3D11_SDK_VERSION,
        &sd, &swapChain, &device, nullptr, &context
    );
    
    // Get vtable from swap chain
    void** vtable = *reinterpret_cast<void***>(swapChain);
    void* present = vtable[8];  // Present is index 8
    
    // Cleanup
    swapChain->Release();
    device->Release();
    context->Release();
    DestroyWindow(hwnd);
    
    return present;
}
```

### Method 2: Pattern Scan

Scan for known byte patterns in d3d11.dll:

```cpp
// Pattern for Present might be something like:
const char* presentPattern = "48 89 5C 24 ?? 48 89 74 24 ?? 55 57 41 56";
void* present = FindPattern(GetModuleHandleA("dxgi.dll"), presentPattern);
```

### The VTable Indices (D3D11)

```cpp
// IDXGISwapChain vtable indices:
// 0  QueryInterface
// 1  AddRef
// 2  Release
// 3  SetPrivateData
// 4  SetPrivateDataInterface
// 5  GetPrivateData
// 6  GetParent
// 7  GetDevice
// 8  Present              ← Hook this for rendering
// 9  GetBuffer
// 10 SetFullscreenState
// 11 GetFullscreenState
// 12 GetDesc
// 13 ResizeBuffers        ← Hook this for window resize
// 14 ResizeTarget
// ...
```

---

## WndProc Hook

Intercept window messages for input handling.

```cpp
WNDPROC OriginalWndProc = nullptr;

LRESULT CALLBACK HookedWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    // Let ImGui handle input first
    if (ImGui_ImplWin32_WndProcHandler(hwnd, msg, wParam, lParam)) {
        return true;  // ImGui consumed the input
    }
    
    // Handle your own input
    if (msg == WM_KEYDOWN && wParam == VK_INSERT) {
        g_ShowMenu = !g_ShowMenu;
        return 0;
    }
    
    // If menu is open, block game input
    if (g_ShowMenu) {
        switch (msg) {
        case WM_KEYDOWN:
        case WM_KEYUP:
        case WM_MOUSEMOVE:
        case WM_LBUTTONDOWN:
        case WM_LBUTTONUP:
            return 0;  // Block these from reaching the game
        }
    }
    
    // Pass to original
    return CallWindowProcA(OriginalWndProc, hwnd, msg, wParam, lParam);
}

// Hook it
void HookWndProc(HWND hwnd) {
    OriginalWndProc = reinterpret_cast<WNDPROC>(
        SetWindowLongPtrA(hwnd, GWLP_WNDPROC, 
            reinterpret_cast<LONG_PTR>(HookedWndProc))
    );
}
```

---

## Mono/Unity Specific Hooks

For Unity Mono games, you can hook C# methods.

### Getting Method Pointers

```cpp
// Get the method info
MonoMethod* method = mono_class_get_method_from_name(
    playerClass, "TakeDamage", 4  // Method name, arg count
);

// Compile it to get native pointer
void* methodPtr = mono_compile_method(method);

// Now hook it like any other function
MH_CreateHook(methodPtr, &HookedTakeDamage, 
    reinterpret_cast<void**>(&OriginalTakeDamage));
```

### Hook Function Signature

Match the C# method signature:
```csharp
// C# method
public void TakeDamage(int damage, bool hasSFX, bool callRPC, CauseOfDeath cause)
```

```cpp
// C++ hook (note: 'this' is first parameter for instance methods)
using TakeDamage_t = void(*)(void* thisPtr, int damage, bool hasSFX, 
    bool callRPC, int causeOfDeath);
TakeDamage_t OriginalTakeDamage = nullptr;

void HookedTakeDamage(void* thisPtr, int damage, bool hasSFX, 
    bool callRPC, int causeOfDeath) 
{
    // God mode: just don't take damage
    if (g_GodMode) return;
    
    // Or reduce damage
    damage = damage / 2;
    
    OriginalTakeDamage(thisPtr, damage, hasSFX, callRPC, causeOfDeath);
}
```

---

## Inline Assembly Hooks (Manual)

If you don't want to use MinHook, here's manual hooking:

### 32-bit (x86) - 5 byte jump

```cpp
void Hook32(void* target, void* hook, void** original) {
    DWORD oldProtect;
    VirtualProtect(target, 5, PAGE_EXECUTE_READWRITE, &oldProtect);
    
    // Save original bytes for trampoline
    uint8_t* trampoline = (uint8_t*)VirtualAlloc(nullptr, 10, 
        MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    memcpy(trampoline, target, 5);
    trampoline[5] = 0xE9;  // jmp
    *(int32_t*)(trampoline + 6) = (int32_t)((uint8_t*)target + 5 - (trampoline + 10));
    *original = trampoline;
    
    // Write jump to hook
    uint8_t* ptr = (uint8_t*)target;
    ptr[0] = 0xE9;  // jmp rel32
    *(int32_t*)(ptr + 1) = (int32_t)((uint8_t*)hook - ptr - 5);
    
    VirtualProtect(target, 5, oldProtect, &oldProtect);
}
```

### 64-bit (x64) - 14 byte jump

64-bit needs absolute addressing (addresses don't fit in 32-bit offset):

```cpp
void Hook64(void* target, void* hook, void** original) {
    DWORD oldProtect;
    VirtualProtect(target, 14, PAGE_EXECUTE_READWRITE, &oldProtect);
    
    // 14-byte absolute jump
    // FF 25 00 00 00 00 - jmp qword ptr [rip+0]
    // XX XX XX XX XX XX XX XX - absolute address
    
    uint8_t jumpBytes[14] = {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,  // jmp qword ptr [rip]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // Address placeholder
    };
    *(void**)(jumpBytes + 6) = hook;
    
    // Create trampoline (more complex for x64, skipped here)
    // ...
    
    memcpy(target, jumpBytes, 14);
    VirtualProtect(target, 14, oldProtect, &oldProtect);
}
```

This is why MinHook exists - it handles all the edge cases.

---

## VMT (Virtual Method Table) Hook Class

A cleaner approach to vtable hooking:

```cpp
class VMTHook {
    void** m_vtable = nullptr;
    void** m_original = nullptr;
    size_t m_size = 0;
    
public:
    bool Setup(void* instance) {
        m_vtable = *reinterpret_cast<void***>(instance);
        
        // Count vtable entries
        while (m_vtable[m_size]) m_size++;
        
        // Copy original vtable
        m_original = new void*[m_size];
        memcpy(m_original, m_vtable, m_size * sizeof(void*));
        
        return true;
    }
    
    void Hook(size_t index, void* hook) {
        DWORD oldProtect;
        VirtualProtect(&m_vtable[index], sizeof(void*), 
            PAGE_READWRITE, &oldProtect);
        m_vtable[index] = hook;
        VirtualProtect(&m_vtable[index], sizeof(void*), 
            oldProtect, &oldProtect);
    }
    
    template<typename T>
    T GetOriginal(size_t index) {
        return reinterpret_cast<T>(m_original[index]);
    }
    
    void Unhook(size_t index) {
        DWORD oldProtect;
        VirtualProtect(&m_vtable[index], sizeof(void*), 
            PAGE_READWRITE, &oldProtect);
        m_vtable[index] = m_original[index];
        VirtualProtect(&m_vtable[index], sizeof(void*), 
            oldProtect, &oldProtect);
    }
    
    ~VMTHook() {
        delete[] m_original;
    }
};

// Usage
VMTHook swapChainHook;
swapChainHook.Setup(swapChain);
swapChainHook.Hook(8, HookedPresent);
auto original = swapChainHook.GetOriginal<Present_t>(8);
```

---

## Common Pitfalls

### 1. Thread Safety
Hooks can be called from any thread. Don't assume single-threaded.

```cpp
std::mutex g_hookMutex;

void HookedFunction() {
    std::lock_guard<std::mutex> lock(g_hookMutex);
    // Safe to access shared data
}
```

### 2. Recursion
If your hook calls functions that trigger the same hook:

```cpp
thread_local bool g_inHook = false;

HRESULT HookedPresent(...) {
    if (g_inHook) return OriginalPresent(...);
    g_inHook = true;
    // Your code
    g_inHook = false;
    return OriginalPresent(...);
}
```

### 3. Calling Conventions
Make sure your hook matches the original:
- `__stdcall` - Callee cleans stack (Windows API)
- `__cdecl` - Caller cleans stack (C default)
- `__fastcall` - First args in registers
- `__thiscall` - `this` in ECX (x86), RCX (x64)

### 4. Stack Alignment
x64 requires 16-byte stack alignment before `call`. MinHook handles this.

### 5. Anti-Cheat Detection
- Integrity checks on code
- Hook detection by comparing bytes
- Return address checks
- Your hook might get detected

---

## Summary

| Hook Type | Pros | Cons |
|-----------|------|------|
| Inline (MinHook) | Universal, any function | Requires protection change |
| VTable | Clean, no code modification | Only virtual functions |
| IAT | Easy for imports | Only imported functions |
| Hardware BP | Invisible to memory scans | Limited to 4 breakpoints |

For our project:
- **D3D11 Present** - VTable hook (index 8)
- **Mono methods** - Inline hook via MinHook
- **WndProc** - SetWindowLongPtr (Windows API)

---

## Next Steps

1. Read `05-UNITY-MONO-HACKING.md` - Unity/Mono specifics
2. Experiment: Hook MessageBoxA to change its text
3. Look at our `renderer.cpp` - See real D3D11 hooking code
