# Unity Mono Hacking

Specific techniques for hacking Unity games using the Mono runtime.

---

## Unity Architecture Overview

Unity games come in two flavors:

### 1. Mono Backend (What Lethal Company Uses)
- C# code compiled to CIL (Common Intermediate Language)
- Mono runtime JIT compiles to native code
- Easy to decompile and modify
- DLLs in `GameName_Data/Managed/`

### 2. IL2CPP Backend
- C# converted to C++ at build time
- Harder to reverse (no readable IL)
- Need IL2CppDumper to recover structure
- We won't cover this in detail

---

## Mono Runtime Basics

Mono is the open-source .NET runtime Unity uses. It provides:

- JIT compilation (IL → native code)
- Garbage collection
- Type system
- Reflection

### Key Mono Structures

```cpp
// Domain - Container for assemblies (like AppDomain in .NET)
MonoDomain* domain;

// Assembly - A loaded DLL
MonoAssembly* assembly;  // The assembly handle
MonoImage* image;        // The metadata for the assembly

// Class - A type definition
MonoClass* klass;        // Note: 'klass' because 'class' is C++ keyword

// Method - A function
MonoMethod* method;

// Field - A member variable
MonoClassField* field;

// Object - An instance of a class
MonoObject* object;
```

---

## Loading Mono Functions

First, get function pointers from mono-2.0-bdwgc.dll:

```cpp
HMODULE mono = GetModuleHandleA("mono-2.0-bdwgc.dll");

// Define function pointer types
using mono_get_root_domain_t = MonoDomain* (*)();
using mono_domain_assembly_foreach_t = void (*)(MonoDomain*, void(*)(void*, void*), void*);
using mono_assembly_get_image_t = MonoImage* (*)(MonoAssembly*);
using mono_class_from_name_t = MonoClass* (*)(MonoImage*, const char*, const char*);
using mono_class_get_method_from_name_t = MonoMethod* (*)(MonoClass*, const char*, int);
using mono_class_get_field_from_name_t = MonoClassField* (*)(MonoClass*, const char*);
using mono_field_get_value_t = void (*)(MonoObject*, MonoClassField*, void*);
using mono_field_set_value_t = void (*)(MonoObject*, MonoClassField*, void*);
using mono_compile_method_t = void* (*)(MonoMethod*);
using mono_runtime_invoke_t = MonoObject* (*)(MonoMethod*, void*, void**, MonoException**);

// Load them
auto mono_get_root_domain = (mono_get_root_domain_t)GetProcAddress(mono, "mono_get_root_domain");
auto mono_class_from_name = (mono_class_from_name_t)GetProcAddress(mono, "mono_class_from_name");
// ... etc
```

---

## Finding Classes

### By Namespace and Name

```cpp
// Get the root domain
MonoDomain* domain = mono_get_root_domain();

// Get Assembly-CSharp.dll image
MonoImage* image = GetAssemblyCSharpImage();  // You need to implement this

// Find PlayerControllerB class
// Parameters: image, namespace, class name
MonoClass* playerClass = mono_class_from_name(image, "", "PlayerControllerB");

// Some classes are in namespaces
MonoClass* hudClass = mono_class_from_name(image, "GameNetcodeStuff", "HUDManager");
```

### Assembly Enumeration

The game loads assemblies into memory. We enumerate to find Assembly-CSharp:

```cpp
struct AssemblySearchContext {
    const char* targetName;
    MonoAssembly* result;
};

void AssemblySearchCallback(void* assembly, void* userData) {
    auto* ctx = static_cast<AssemblySearchContext*>(userData);
    MonoAssemblyName* aname = mono_assembly_get_name((MonoAssembly*)assembly);
    const char* name = mono_assembly_name_get_name(aname);
    
    if (strcmp(name, ctx->targetName) == 0) {
        ctx->result = (MonoAssembly*)assembly;
    }
}

MonoAssembly* FindAssembly(const char* name) {
    AssemblySearchContext ctx = { name, nullptr };
    mono_domain_assembly_foreach(mono_get_root_domain(), AssemblySearchCallback, &ctx);
    return ctx.result;
}

// Usage
MonoAssembly* asmCSharp = FindAssembly("Assembly-CSharp");
MonoImage* image = mono_assembly_get_image(asmCSharp);
```

---

## Getting and Setting Fields

### Finding Field Offset

```cpp
MonoClass* playerClass = mono_class_from_name(image, "", "PlayerControllerB");
MonoClassField* healthField = mono_class_get_field_from_name(playerClass, "health");

// Get offset - this is distance from object start
int offset = mono_field_get_offset(healthField);
// For PlayerControllerB.health, this might be 0x178 or similar
```

### Reading Fields (Direct Memory)

Once you have the offset, read directly:

```cpp
// playerInstance is a MonoObject* or void* pointing to a player
int health = *reinterpret_cast<int*>(
    reinterpret_cast<uint8_t*>(playerInstance) + healthOffset
);
```

### Writing Fields (Direct Memory)

```cpp
*reinterpret_cast<int*>(
    reinterpret_cast<uint8_t*>(playerInstance) + healthOffset
) = 100;  // Set health to 100
```

### Using Mono API (Safer)

```cpp
// Read
int health;
mono_field_get_value(playerInstance, healthField, &health);

// Write
int newHealth = 100;
mono_field_set_value(playerInstance, healthField, &newHealth);
```

---

## Calling Methods

### Getting Method Pointer

```cpp
// Get method by name and argument count
MonoMethod* takeDamageMethod = mono_class_get_method_from_name(
    playerClass, 
    "TakeDamage", 
    4  // Number of arguments
);

// Compile to get native address
void* methodAddr = mono_compile_method(takeDamageMethod);
```

### Calling with mono_runtime_invoke

```cpp
// Set up arguments
void* args[4];
int damage = 50;
bool hasSFX = true;
bool callRPC = true;
int causeOfDeath = 0;

args[0] = &damage;
args[1] = &hasSFX;
args[2] = &callRPC;
args[3] = &causeOfDeath;

// Call the method
MonoException* exception = nullptr;
MonoObject* result = mono_runtime_invoke(takeDamageMethod, playerInstance, args, &exception);

if (exception) {
    // Handle error
}
```

### Calling Directly (Faster)

```cpp
// Define the native signature
using TakeDamage_t = void (*)(void* thisPtr, int damage, bool hasSFX, bool callRPC, int causeOfDeath);
auto TakeDamage = reinterpret_cast<TakeDamage_t>(methodAddr);

// Call it
TakeDamage(playerInstance, 50, true, true, 0);
```

---

## Finding Game Objects

Unity uses a global object management system.

### Method 1: Static Fields

Many important objects are stored in static fields:

```cpp
MonoClass* startOfRoundClass = mono_class_from_name(image, "", "StartOfRound");
MonoClassField* instanceField = mono_class_get_field_from_name(startOfRoundClass, "Instance");

// Get the static value
void* vtable = mono_class_vtable(domain, startOfRoundClass);
MonoObject* instance = nullptr;
mono_field_static_get_value(vtable, instanceField, &instance);
```

### Method 2: FindObjectOfType (via Mono)

```cpp
// Get UnityEngine.Object type
MonoAssembly* unityAsm = FindAssembly("UnityEngine.CoreModule");
MonoImage* unityImage = mono_assembly_get_image(unityAsm);
MonoClass* objectClass = mono_class_from_name(unityImage, "UnityEngine", "Object");

// Get FindObjectOfType method
MonoMethod* findMethod = mono_class_get_method_from_name(objectClass, "FindObjectOfType", 1);
// This is generic, need to use mono_method_inflate or similar
// ... complex, usually easier to use static Instance fields
```

### Method 3: Walking Object Lists

Unity maintains lists of objects. Access via `Object.FindObjectsOfType<T>()` pattern.

---

## Common Lethal Company Classes

Based on dnSpy analysis:

### PlayerControllerB

```csharp
// The main player class
public class PlayerControllerB : MonoBehaviour {
    public int health = 100;
    public float sprintMeter = 1f;
    public bool isPlayerDead;
    public bool isInsideFactory;
    public float movementSpeed;
    public float jumpForce;
    public bool hasBeenCriticallyInjured;
    public CauseOfDeath causeOfDeath;
    
    public void TakeDamage(int damageNumber, bool hasDamageSFX, 
        bool callRPC, CauseOfDeath causeOfDeath);
    public void KillPlayer(Vector3 bodyVelocity, bool spawnBody, 
        CauseOfDeath causeOfDeath, int deathAnimation);
}
```

### StartOfRound

```csharp
// Game state singleton
public class StartOfRound : MonoBehaviour {
    public static StartOfRound Instance;
    
    public PlayerControllerB[] allPlayerScripts;  // All players
    public SelectableLevel currentLevel;
    public int connectedPlayersAmount;
    public bool shipHasLanded;
    public bool inShipPhase;
}
```

### GameNetworkManager

```csharp
// Network manager
public class GameNetworkManager : MonoBehaviour {
    public static GameNetworkManager Instance;
    
    public PlayerControllerB localPlayerController;  // Your local player
    public bool isHostingGame;
}
```

### Terminal

```csharp
// In-game terminal
public class Terminal : MonoBehaviour {
    public int groupCredits;  // Team money
    public Item[] buyableItemsList;
}
```

### EnemyAI (Base Class)

```csharp
// All enemies inherit from this
public abstract class EnemyAI : MonoBehaviour {
    public bool isEnemyDead;
    public int currentBehaviourStateIndex;
    public float enemyHP;
    public PlayerControllerB targetPlayer;
    
    public virtual void KillEnemy(bool destroy = false);
}
```

---

## Practical Examples

### God Mode

```cpp
// Find local player
MonoClass* gnmClass = mono_class_from_name(image, "", "GameNetworkManager");
MonoClassField* instanceField = mono_class_get_field_from_name(gnmClass, "Instance");
MonoClassField* localPlayerField = mono_class_get_field_from_name(gnmClass, "localPlayerController");

// Get instance
void* gnmInstance = GetStaticField(gnmClass, instanceField);
void* localPlayer = nullptr;
mono_field_get_value(gnmInstance, localPlayerField, &localPlayer);

// Get health field offset
MonoClass* playerClass = mono_class_from_name(image, "", "PlayerControllerB");
MonoClassField* healthField = mono_class_get_field_from_name(playerClass, "health");
int healthOffset = mono_field_get_offset(healthField);

// In your update loop: keep setting health to 100
*reinterpret_cast<int*>((uint8_t*)localPlayer + healthOffset) = 100;
```

### Infinite Sprint

```cpp
MonoClassField* sprintField = mono_class_get_field_from_name(playerClass, "sprintMeter");
int sprintOffset = mono_field_get_offset(sprintField);

// Keep sprint at 100%
*reinterpret_cast<float*>((uint8_t*)localPlayer + sprintOffset) = 1.0f;
```

### ESP (Drawing Players/Enemies)

```cpp
// Get all players from StartOfRound
MonoClass* sorClass = mono_class_from_name(image, "", "StartOfRound");
// ... get Instance ...
// ... get allPlayerScripts field (it's an array) ...

// Unity arrays have a header, actual data starts at offset
// MonoArray structure: vtable, monitor, bounds, max_length, then elements
struct MonoArrayHeader {
    void* vtable;
    void* monitor;
    void* bounds;
    int max_length;
    // elements follow
};

auto* arrayHeader = (MonoArrayHeader*)playersArray;
void** players = (void**)(arrayHeader + 1);  // Elements after header

for (int i = 0; i < arrayHeader->max_length; i++) {
    void* player = players[i];
    if (!player) continue;
    
    // Get position (Vector3 at some offset, from Transform component)
    // Draw on screen
}
```

### Kill All Enemies

```cpp
// Get all EnemyAI instances
// Call KillEnemy() on each

MonoClass* enemyClass = mono_class_from_name(image, "", "EnemyAI");
MonoMethod* killMethod = mono_class_get_method_from_name(enemyClass, "KillEnemy", 1);
void* killAddr = mono_compile_method(killMethod);

using KillEnemy_t = void (*)(void* thisPtr, bool destroy);
auto KillEnemy = (KillEnemy_t)killAddr;

for (auto enemy : allEnemies) {
    KillEnemy(enemy, true);
}
```

---

## Caching for Performance

Don't call mono_class_from_name every frame. Cache everything:

```cpp
struct GameBindings {
    // Classes
    MonoClass* playerClass = nullptr;
    MonoClass* enemyClass = nullptr;
    MonoClass* startOfRoundClass = nullptr;
    MonoClass* terminalClass = nullptr;
    
    // Field offsets
    int playerHealth = 0;
    int playerSprint = 0;
    int playerDead = 0;
    int terminalCredits = 0;
    
    // Method pointers
    void* takeDamageAddr = nullptr;
    void* killEnemyAddr = nullptr;
    
    bool Initialize() {
        playerClass = mono_class_from_name(image, "", "PlayerControllerB");
        if (!playerClass) return false;
        
        auto healthField = mono_class_get_field_from_name(playerClass, "health");
        playerHealth = mono_field_get_offset(healthField);
        
        // ... cache everything else ...
        
        return true;
    }
};

// Global instance, initialize once
GameBindings g_bindings;
```

---

## Mono Internals: Object Layout

Every MonoObject has this layout:

```
┌───────────────────────────────────────┐
│ vtable pointer (8 bytes on x64)       │  ← Object address points here
├───────────────────────────────────────┤
│ synchronization (monitor) (8 bytes)   │
├───────────────────────────────────────┤
│ Field 1                               │  ← Offsets from mono_field_get_offset
├───────────────────────────────────────┤
│ Field 2                               │
├───────────────────────────────────────┤
│ ...                                   │
└───────────────────────────────────────┘
```

So actual field offset = mono_field_get_offset result (already accounts for header).

### Value Types vs Reference Types

```cpp
// Value type (int, float, bool, struct): stored inline
int health;  // 4 bytes directly in the object

// Reference type (class, array): stored as pointer
PlayerControllerB targetPlayer;  // 8 byte pointer to another object
```

---

## Common Issues

### 1. Game Not Fully Loaded

If you inject too early, classes won't be found:

```cpp
// Wait for Assembly-CSharp to load
while (!FindAssembly("Assembly-CSharp")) {
    Sleep(100);
}
```

### 2. Null Instance Fields

Singletons might not be set yet:

```cpp
void* instance = GetStaticField(startOfRoundClass, instanceField);
if (!instance) {
    // Not in game yet, wait
    return;
}
```

### 3. Thread Safety

Mono has its own threading. Attach your thread if calling Mono from a non-Mono thread:

```cpp
mono_thread_attach(mono_get_root_domain());
```

### 4. Garbage Collection

Objects can move in memory. Use GC handles for long-term references:

```cpp
uint32_t handle = mono_gchandle_new(object, false);
// Later...
MonoObject* obj = mono_gchandle_get_target(handle);
// When done
mono_gchandle_free(handle);
```

---

## Tools for Unity Reversing

1. **dnSpy** - Decompile Assembly-CSharp.dll, see all code
2. **Unity Explorer** - In-game object browser (BepInEx mod)
3. **Runtime Unity Editor** - Another in-game explorer
4. **Cheat Engine: Mono Dissect** - CE has Unity/Mono support

---

## Summary

1. Load mono functions from mono-2.0-bdwgc.dll
2. Get domain and find Assembly-CSharp image
3. Get classes with mono_class_from_name
4. Get field offsets with mono_field_get_offset
5. Read/write memory directly at object + offset
6. Get method pointers with mono_compile_method
7. Hook methods or call them directly

The Mono runtime gives you full access to the game's managed code, making Unity Mono games very hackable compared to IL2CPP or native games.

---

## Next Steps

1. Open `Assembly-CSharp.dll` in dnSpy and explore
2. Look at our `mono.cpp` and `game.cpp` implementation
3. Try reading player health value
4. Add a simple feature (god mode, infinite sprint)
