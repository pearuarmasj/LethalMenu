#pragma once

#include <MinHook.h>
#include <functional>
#include <unordered_map>
#include <string>

namespace core
{
    class Hooks
    {
    public:
        static Hooks& Get();

        bool Initialize();
        void Shutdown();

        // Add a hook with a name for tracking
        bool AddHook(const std::string& name, void* target, void* detour, void** original);

        template<typename T>
        bool CreateHook(void* target, void* detour, T** original, const std::string& name)
        {
            return AddHook(name, target, detour, reinterpret_cast<void**>(original));
        }

        bool RemoveHook(const std::string& name);
        bool RemoveAllHooks();

    private:
        Hooks() = default;
        ~Hooks() = default;

        Hooks(const Hooks&) = delete;
        Hooks& operator=(const Hooks&) = delete;

        std::unordered_map<std::string, void*> m_hooks;
        bool m_initialized = false;
    };

    // Alias for backward compatibility
    using HookManager = Hooks;
}
