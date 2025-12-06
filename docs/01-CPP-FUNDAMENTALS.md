# C++ Fundamentals for Game Hacking

This guide covers the C++ knowledge you need for writing game cheats. Not a full C++ tutorial - focused on what matters for this context.

---

## Memory and Pointers

Everything in game hacking revolves around memory. You need to understand this cold.

### Basic Types and Sizes (x64)

```cpp
// These are the sizes on 64-bit Windows
char      // 1 byte
short     // 2 bytes
int       // 4 bytes
long      // 4 bytes (Windows-specific, 8 on Linux)
long long // 8 bytes
float     // 4 bytes
double    // 8 bytes
void*     // 8 bytes (pointer size on x64)
```

### Pointers

A pointer is just a memory address stored in a variable.

```cpp
int value = 42;
int* ptr = &value;  // ptr now holds the ADDRESS of value

// Dereferencing - getting the value AT an address
int x = *ptr;       // x is now 42

// Pointer arithmetic
int arr[5] = {10, 20, 30, 40, 50};
int* p = arr;       // Points to arr[0]
p++;                // Now points to arr[1] (moved 4 bytes, sizeof(int))
int val = *(p + 2); // Gets arr[3] = 40
```

### Pointer to Pointer (Multi-level pointers)

Game cheats often deal with chains of pointers:

```cpp
// In games, you often see:
// BaseAddress -> Pointer1 -> Pointer2 -> ActualValue

void* base = (void*)0x12345678;
void* level1 = *(void**)base;           // Read pointer at base
void* level2 = *(void**)((char*)level1 + 0x10);  // Read at level1 + offset
float health = *(float*)((char*)level2 + 0x48); // Finally get health
```

### nullptr and Invalid Pointers

```cpp
void* ptr = nullptr;  // Null pointer (address 0)

// ALWAYS check before dereferencing
if (ptr != nullptr) {
    int value = *(int*)ptr;  // Safe
}

// Common crash: dereferencing null or invalid pointer
int* bad = nullptr;
int x = *bad;  // CRASH - access violation
```

---

## Casting

### C-Style Casts

Quick and dirty, commonly used in cheat code:

```cpp
void* address = (void*)0x12345678;
int* intPtr = (int*)address;
float value = *(float*)address;
```

### C++ Casts

More explicit about what you're doing:

```cpp
// reinterpret_cast - "treat these bytes as this type"
void* addr = reinterpret_cast<void*>(0x12345678);
int* iptr = reinterpret_cast<int*>(addr);

// static_cast - conversion between related types
float f = 3.14f;
int i = static_cast<int>(f);  // i = 3

// const_cast - remove const (rarely needed)
const int* cptr = &value;
int* mptr = const_cast<int*>(cptr);
```

For game hacking, `reinterpret_cast` is your bread and butter - you're constantly reinterpreting raw memory as different types.

---

## Classes and Structs

In C++, `class` and `struct` are nearly identical. Only difference: `struct` members are public by default, `class` members are private by default.

### Memory Layout

```cpp
struct Player {
    float x;        // offset 0x00
    float y;        // offset 0x04
    float z;        // offset 0x08
    int health;     // offset 0x0C
    int ammo;       // offset 0x10
};  // Total size: 0x14 (20 bytes)

// Accessing via pointer
Player* player = GetLocalPlayer();
player->health = 100;  // Same as (*player).health = 100

// Accessing via raw address + offset
void* playerAddr = (void*)0x12345678;
int* healthPtr = (int*)((char*)playerAddr + 0x0C);
*healthPtr = 100;
```

### Inheritance and VTables

When a class has virtual functions, it gets a vtable pointer:

```cpp
class Entity {
public:
    virtual void Update() { }      // Virtual function
    virtual void Render() { }
    float x, y, z;
};

class Player : public Entity {
public:
    void Update() override { }     // Overrides Entity::Update
    int health;
};

// Memory layout of Player:
// 0x00: void** vtable  (pointer to virtual function table)
// 0x08: float x
// 0x0C: float y
// 0x10: float z
// 0x14: int health
```

The vtable is how we hook virtual functions - replace the function pointer in the vtable with our own function.

---

## Function Pointers

Critical for hooking.

```cpp
// Function pointer type
typedef int (*AddFunc)(int, int);

// Or using 'using' (modern C++)
using AddFunc = int(*)(int, int);

// Example
int Add(int a, int b) { return a + b; }

AddFunc ptr = &Add;      // Or just: AddFunc ptr = Add;
int result = ptr(2, 3);  // result = 5

// Calling convention matters for hooks (Windows x64 uses __fastcall)
typedef void (__fastcall *GameFunc)(void* thisPtr, int param);
```

### Member Function Pointers

More complex - need to handle the hidden `this` pointer:

```cpp
class Foo {
public:
    int Bar(int x) { return x * 2; }
};

// Member function pointer
int (Foo::*memberPtr)(int) = &Foo::Bar;

Foo obj;
int result = (obj.*memberPtr)(5);  // result = 10
```

---

## The `this` Pointer

Every non-static member function receives a hidden `this` pointer.

```cpp
class Player {
    int health;
public:
    void TakeDamage(int damage) {
        // 'this' is implicitly passed
        this->health -= damage;
        // Same as:
        health -= damage;
    }
};

// When hooking, you often see the this pointer explicitly:
void __fastcall HookedTakeDamage(Player* thisPtr, int damage) {
    // thisPtr is what would normally be 'this'
    if (IsGodmodeEnabled()) {
        damage = 0;  // Negate damage
    }
    OriginalTakeDamage(thisPtr, damage);
}
```

---

## Templates

Generic programming. You write code once, compiler generates versions for each type.

```cpp
template<typename T>
T* SafeRead(void* address) {
    if (!address) return nullptr;
    return reinterpret_cast<T*>(address);
}

// Usage - compiler generates specific versions
int* iptr = SafeRead<int>(someAddr);
float* fptr = SafeRead<float>(otherAddr);

// Our SafeResult template from exception.h:
template<typename T>
struct SafeResult {
    bool success;
    T value;
    std::string error;
};
```

---

## Preprocessor and Macros

Text substitution before compilation.

```cpp
// Simple constant
#define MAX_PLAYERS 32

// Function-like macro
#define LOG_INFO(...) Logger::Get().LogFormat(LogLevel::Info, __VA_ARGS__)

// Conditional compilation
#ifdef _DEBUG
    #define ASSERT(x) if(!(x)) { __debugbreak(); }
#else
    #define ASSERT(x)
#endif

// Include guards (prevent double inclusion)
#pragma once  // Modern way
// Or traditional:
#ifndef MY_HEADER_H
#define MY_HEADER_H
// ... header content ...
#endif
```

---

## Headers and Source Files

### Header (.h)

Declarations - tells the compiler "this thing exists":

```cpp
// player.h
#pragma once

class Player {
public:
    void TakeDamage(int damage);  // Declaration only
    int GetHealth() const;
private:
    int m_health;
};
```

### Source (.cpp)

Definitions - the actual implementation:

```cpp
// player.cpp
#include "player.h"

void Player::TakeDamage(int damage) {
    m_health -= damage;  // Implementation
}

int Player::GetHealth() const {
    return m_health;
}
```

### Why Separate?

1. Faster compilation - only recompile .cpp files that changed
2. Hide implementation details
3. Avoid circular dependencies

---

## Namespaces

Prevent name collisions:

```cpp
namespace core {
    class Hooks { };
}

namespace sdk {
    class Player { };
}

// Usage
core::Hooks hooks;
sdk::Player player;

// Or bring into scope
using namespace core;  // Now can use Hooks directly (avoid in headers)
using core::Hooks;     // Just bring Hooks into scope
```

---

## Important Standard Library

### std::string

```cpp
#include <string>

std::string name = "Player1";
std::string full = name + " - Health: 100";  // Concatenation
const char* cstr = name.c_str();  // Get C-style string for APIs
```

### std::vector

Dynamic array:

```cpp
#include <vector>

std::vector<int> numbers;
numbers.push_back(10);
numbers.push_back(20);

for (int n : numbers) {
    printf("%d\n", n);
}

// Access
int first = numbers[0];
int second = numbers.at(1);  // Bounds-checked
```

### std::unordered_map

Hash table for fast lookups:

```cpp
#include <unordered_map>

std::unordered_map<std::string, int> scores;
scores["player1"] = 100;
scores["player2"] = 200;

if (scores.find("player1") != scores.end()) {
    int score = scores["player1"];
}
```

### std::function

Type-erased function wrapper:

```cpp
#include <functional>

std::function<int(int, int)> operation;
operation = [](int a, int b) { return a + b; };
int result = operation(2, 3);  // 5
```

---

## Lambdas

Anonymous functions, crucial for modern C++:

```cpp
// Basic lambda
auto add = [](int a, int b) { return a + b; };
int sum = add(2, 3);

// Capture external variables
int multiplier = 10;
auto multiply = [multiplier](int x) { return x * multiplier; };

// Capture by reference (can modify)
int counter = 0;
auto increment = [&counter]() { counter++; };
increment();  // counter is now 1

// Capture everything by reference
auto func = [&]() { /* can access all local vars by reference */ };

// Capture everything by value
auto func2 = [=]() { /* copies of all local vars */ };
```

---

## RAII and Smart Pointers

RAII = Resource Acquisition Is Initialization. Cleanup happens automatically when object goes out of scope.

```cpp
#include <memory>

// unique_ptr - single owner
std::unique_ptr<Player> player = std::make_unique<Player>();
player->TakeDamage(10);
// Automatically deleted when unique_ptr goes out of scope

// shared_ptr - reference counted
std::shared_ptr<Player> p1 = std::make_shared<Player>();
std::shared_ptr<Player> p2 = p1;  // Both point to same object
// Deleted when last shared_ptr is destroyed
```

For game hacking, you often work with raw pointers from the game - don't wrap those in smart pointers since you don't own that memory.

---

## Practical Example: Reading Game Memory

```cpp
// Suppose we found the player base address and offsets through reversing
constexpr uintptr_t PLAYER_BASE = 0x1234567890;
constexpr size_t OFFSET_HEALTH = 0x100;
constexpr size_t OFFSET_POSITION = 0x50;

struct Vector3 {
    float x, y, z;
};

class GamePlayer {
public:
    // Read from game memory
    static GamePlayer* GetLocal() {
        // Multi-level pointer dereference
        void* base = *(void**)PLAYER_BASE;
        if (!base) return nullptr;
        return reinterpret_cast<GamePlayer*>(base);
    }
    
    int GetHealth() const {
        return *reinterpret_cast<int*>(
            reinterpret_cast<uintptr_t>(this) + OFFSET_HEALTH
        );
    }
    
    void SetHealth(int value) {
        *reinterpret_cast<int*>(
            reinterpret_cast<uintptr_t>(this) + OFFSET_HEALTH
        ) = value;
    }
    
    Vector3 GetPosition() const {
        return *reinterpret_cast<Vector3*>(
            reinterpret_cast<uintptr_t>(this) + OFFSET_POSITION
        );
    }
};

// Usage
void GodMode() {
    auto* player = GamePlayer::GetLocal();
    if (player) {
        player->SetHealth(9999);
    }
}
```

---

## Next Steps

Once you're comfortable with these fundamentals:
1. Read `02-WINDOWS-INTERNALS.md` - DLLs, memory, process injection
2. Read `03-REVERSING-BASICS.md` - Finding offsets and structures
3. Read `04-HOOKING-TECHNIQUES.md` - How hooks work
4. Read `05-UNITY-MONO-HACKING.md` - Specific to this project
