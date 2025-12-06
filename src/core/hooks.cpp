#include "pch.h"
#include "hooks.h"
#include "utils/logger.h"

namespace core
{
    Hooks& Hooks::Get()
    {
        static Hooks instance;
        return instance;
    }

    bool Hooks::Initialize()
    {
        if (m_initialized)
            return true;

        MH_STATUS status = MH_Initialize();
        if (status != MH_OK)
        {
            LOG_ERROR("MH_Initialize failed: %d", status);
            return false;
        }

        m_initialized = true;
        LOG_INFO("MinHook initialized");
        return true;
    }

    void Hooks::Shutdown()
    {
        if (!m_initialized)
            return;

        RemoveAllHooks();
        MH_Uninitialize();
        m_initialized = false;
        LOG_INFO("MinHook shutdown");
    }

    bool Hooks::AddHook(const std::string& name, void* target, void* detour, void** original)
    {
        if (!target || !detour)
        {
            LOG_ERROR("AddHook '%s': null target or detour", name.c_str());
            return false;
        }

        MH_STATUS status = MH_CreateHook(target, detour, original);
        if (status != MH_OK)
        {
            LOG_ERROR("MH_CreateHook '%s' failed: %d", name.c_str(), status);
            return false;
        }

        status = MH_EnableHook(target);
        if (status != MH_OK)
        {
            LOG_ERROR("MH_EnableHook '%s' failed: %d", name.c_str(), status);
            MH_RemoveHook(target);
            return false;
        }

        m_hooks[name] = target;
        LOG_INFO("Hook '%s' installed at %p", name.c_str(), target);
        return true;
    }

    bool Hooks::RemoveHook(const std::string& name)
    {
        auto it = m_hooks.find(name);
        if (it == m_hooks.end())
            return false;

        MH_DisableHook(it->second);
        MH_RemoveHook(it->second);
        m_hooks.erase(it);
        LOG_INFO("Hook '%s' removed", name.c_str());
        return true;
    }

    bool Hooks::RemoveAllHooks()
    {
        for (auto& [name, target] : m_hooks)
        {
            MH_DisableHook(target);
            MH_RemoveHook(target);
        }
        m_hooks.clear();
        return true;
    }
}
