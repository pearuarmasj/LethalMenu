# Project Structure & Architecture

How this cheat is organized and why.

---

## Directory Layout

```
LethalMenu/
├── deps/                    # Third-party dependencies
│   ├── imgui/              # Dear ImGui for UI rendering
│   ├── MinHook-src/        # Hooking library (compiled from source)
│   └── nlohmann/           # JSON library for config
│
├── src/                    # Our source code
│   ├── core/               # Core systems
│   │   ├── cheat.cpp/h     # Main entry, initialization orchestration
│   │   ├── mono.cpp/h      # Mono runtime interface
│   │   ├── game.cpp/h      # Game-specific bindings (classes, fields)
│   │   ├── renderer.cpp/h  # D3D11 hooks and ImGui setup
│   │   ├── hooks.cpp/h     # MinHook wrapper
│   │   └── exception.cpp/h # SEH crash protection
│   │
│   ├── features/           # Individual cheat features
│   │   └── (esp.cpp, godmode.cpp, etc.)
│   │
│   ├── ui/                 # Menu and UI code
│   │   └── menu.cpp/h      # ImGui menu rendering
│   │
│   ├── utils/              # Utility code
│   │   ├── logger.cpp/h    # Logging system
│   │   ├── console.cpp/h   # Debug console window
│   │   └── memory.h        # Memory read/write helpers
│   │
│   ├── dllmain.cpp         # DLL entry point
│   ├── pch.h               # Precompiled header
│   └── framework.h         # Windows includes
│
├── docs/                   # Documentation (you're reading this)
│
└── Additional sources for help/  # Reference material
    └── Project-Apparatus-main/   # C# cheat for reference
```

---

## Initialization Flow

When the DLL is injected:

```
1. DllMain (DLL_PROCESS_ATTACH)
   └── Creates main thread (to avoid loader lock)

2. Main Thread
   ├── Cheat::Initialize()
   │   ├── Console::Initialize()      # Debug output window
   │   ├── Exception::Initialize()    # SEH framework
   │   ├── Hooks::Initialize()        # MinHook MH_Initialize
   │   ├── Mono::Initialize()         # Load mono functions
   │   │   └── Wait for Assembly-CSharp
   │   ├── Game::Initialize()         # Cache classes, offsets
   │   ├── Renderer::Initialize()     # D3D11 hooks
   │   │   ├── Find Present address
   │   │   ├── Hook Present
   │   │   ├── Hook ResizeBuffers
   │   │   └── Setup ImGui
   │   └── Menu::Initialize()         # Load config, setup UI
   │
   └── Main loop (or just return)

3. Hooks run continuously
   ├── Present hook → Render ImGui
   ├── WndProc hook → Handle input
   └── (Future: Game function hooks)
```

---

## Core Systems Explained

### cheat.cpp - The Orchestrator

```cpp
namespace Cheat {
    bool Initialize() {
        // Init everything in order, with error handling
        SEH_TRY("Console", {
            if (!Console::Initialize()) return false;
        });
        
        SEH_TRY("Hooks", {
            if (!Hooks::Initialize()) return false;
        });
        
        // ... etc
        return true;
    }
    
    void Shutdown() {
        // Cleanup in reverse order
        Renderer::Shutdown();
        Hooks::Shutdown();
        // ...
    }
}
```

### mono.cpp - Mono Runtime Bridge

Loads function pointers from mono-2.0-bdwgc.dll and provides wrappers:

```cpp
namespace Mono {
    // Function pointers
    mono_get_root_domain_t fn_mono_get_root_domain;
    mono_class_from_name_t fn_mono_class_from_name;
    // ...
    
    bool Initialize() {
        HMODULE mono = GetModuleHandleA("mono-2.0-bdwgc.dll");
        if (!mono) return false;
        
        LOAD_FUNC(mono, mono_get_root_domain);
        LOAD_FUNC(mono, mono_class_from_name);
        // ...
        
        // Find Assembly-CSharp
        g_assemblyImage = FindAssemblyCSharp();
        return g_assemblyImage != nullptr;
    }
    
    // Helper wrappers
    MonoClass* FindClass(const char* ns, const char* name) {
        return fn_mono_class_from_name(g_assemblyImage, ns, name);
    }
}
```

### game.cpp - Game Bindings

Caches all game classes, fields, and offsets:

```cpp
namespace Game {
    // Cached data
    struct {
        MonoClass* playerClass;
        MonoClass* enemyClass;
        int healthOffset;
        int sprintOffset;
        // ...
    } Cache;
    
    bool Initialize() {
        Cache.playerClass = Mono::FindClass("", "PlayerControllerB");
        if (!Cache.playerClass) return false;
        
        Cache.healthOffset = Mono::GetFieldOffset(Cache.playerClass, "health");
        Cache.sprintOffset = Mono::GetFieldOffset(Cache.playerClass, "sprintMeter");
        // ...
        return true;
    }
    
    // Accessors
    void* GetLocalPlayer() {
        // Get GameNetworkManager.Instance.localPlayerController
    }
    
    int GetHealth(void* player) {
        return *reinterpret_cast<int*>((uint8_t*)player + Cache.healthOffset);
    }
    
    void SetHealth(void* player, int value) {
        *reinterpret_cast<int*>((uint8_t*)player + Cache.healthOffset) = value;
    }
}
```

### renderer.cpp - D3D11 and ImGui

Handles graphics hooking and UI rendering:

```cpp
namespace Renderer {
    IDXGISwapChain* g_swapChain;
    ID3D11Device* g_device;
    ID3D11DeviceContext* g_context;
    ID3D11RenderTargetView* g_rtv;
    
    // Original functions
    Present_t OriginalPresent;
    ResizeBuffers_t OriginalResizeBuffers;
    WNDPROC OriginalWndProc;
    
    HRESULT WINAPI HookedPresent(IDXGISwapChain* swapChain, UINT sync, UINT flags) {
        static bool initialized = false;
        if (!initialized) {
            InitImGui(swapChain);
            initialized = true;
        }
        
        // Start ImGui frame
        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();
        
        // Render our menu
        Menu::Render();
        
        // Finish ImGui
        ImGui::Render();
        g_context->OMSetRenderTargets(1, &g_rtv, nullptr);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
        
        return OriginalPresent(swapChain, sync, flags);
    }
}
```

### hooks.cpp - MinHook Wrapper

Simple wrapper around MinHook:

```cpp
namespace Hooks {
    bool Initialize() {
        return MH_Initialize() == MH_OK;
    }
    
    bool Create(void* target, void* detour, void** original) {
        if (MH_CreateHook(target, detour, original) != MH_OK) return false;
        return MH_EnableHook(target) == MH_OK;
    }
    
    bool Remove(void* target) {
        MH_DisableHook(target);
        return MH_RemoveHook(target) == MH_OK;
    }
    
    void Shutdown() {
        MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();
    }
}
```

---

## Adding a New Feature

Example: Adding "Infinite Sprint"

### 1. Add feature file

`src/features/infinite_sprint.cpp`:

```cpp
#include "pch.h"
#include "core/game.h"

namespace Features {
    bool g_infiniteSprint = false;
    
    void UpdateInfiniteSprint() {
        if (!g_infiniteSprint) return;
        
        void* player = Game::GetLocalPlayer();
        if (!player) return;
        
        Game::SetSprintMeter(player, 1.0f);
    }
}
```

### 2. Add to menu

In `src/ui/menu.cpp`:

```cpp
void Menu::Render() {
    // ...
    if (ImGui::CollapsingHeader("Player")) {
        ImGui::Checkbox("God Mode", &Features::g_godMode);
        ImGui::Checkbox("Infinite Sprint", &Features::g_infiniteSprint);
    }
    // ...
}
```

### 3. Call update in main loop

Either in Present hook or a dedicated update thread:

```cpp
// In HookedPresent or an update function
Features::UpdateGodMode();
Features::UpdateInfiniteSprint();
Features::UpdateESP();
// ...
```

---

## Error Handling Pattern

All risky code goes through SEH:

```cpp
SEH_TRY("Feature Name", {
    // Dangerous code here
    void* player = Game::GetLocalPlayer();
    if (player) {
        Game::SetHealth(player, 100);
    }
});
// Continues even if above crashes
```

For functions that return values:

```cpp
auto result = SafeResult<void*>::Execute([]() {
    return Game::GetLocalPlayer();
});

if (result.success && result.value) {
    // Use result.value
}
```

---

## Configuration System

Store settings in JSON:

```cpp
// config.h
namespace Config {
    struct Settings {
        bool godMode = false;
        bool infiniteSprint = false;
        bool esp = false;
        float espDistance = 100.0f;
    };
    
    extern Settings g_settings;
    
    void Load();
    void Save();
}

// config.cpp
void Config::Load() {
    std::ifstream file("LethalMenu.json");
    if (!file) return;
    
    nlohmann::json j;
    file >> j;
    
    g_settings.godMode = j.value("godMode", false);
    g_settings.infiniteSprint = j.value("infiniteSprint", false);
    // ...
}

void Config::Save() {
    nlohmann::json j;
    j["godMode"] = g_settings.godMode;
    j["infiniteSprint"] = g_settings.infiniteSprint;
    // ...
    
    std::ofstream file("LethalMenu.json");
    file << j.dump(4);
}
```

---

## Memory Safety Patterns

### Safe Pointer Access

```cpp
// memory.h
template<typename T>
T SafeRead(void* address, T defaultValue = T()) {
    __try {
        return *reinterpret_cast<T*>(address);
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        return defaultValue;
    }
}

template<typename T>
bool SafeWrite(void* address, T value) {
    __try {
        *reinterpret_cast<T*>(address) = value;
        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        return false;
    }
}
```

### Pointer Chain Walking

```cpp
// Follow: base -> +offset1 -> +offset2 -> +offset3
void* FollowPointerChain(void* base, std::initializer_list<int> offsets) {
    uint8_t* current = reinterpret_cast<uint8_t*>(base);
    
    for (int offset : offsets) {
        if (!current) return nullptr;
        
        current = SafeRead<uint8_t*>(current);
        if (!current) return nullptr;
        
        current += offset;
    }
    
    return current;
}

// Usage
void* health = FollowPointerChain(gameManager, {0x10, 0x48, 0xC0, 0x178});
```

---

## Best Practices

1. **Cache everything** - Don't call Mono functions every frame
2. **Null check everything** - Game state changes, pointers become invalid
3. **Use SEH** - Catch crashes before they crash the game
4. **Log everything** - Debug console shows what went wrong
5. **Separate concerns** - Each file has one job
6. **Config persistence** - Save/load settings
7. **Clean shutdown** - Unhook everything, free resources

---

## Debugging Tips

1. **Attach Visual Studio debugger** to game process
2. **Use OutputDebugString** / our Console for logging
3. **Build Debug config** for more info on crashes
4. **Add breakpoints** in your hooks
5. **Check return values** - Mono functions return null on failure
