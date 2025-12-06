#include "pch.h"
#include "cheat.h"
#include "hooks.h"
#include "renderer.h"
#include "mono.h"
#include "exception.h"
#include "gui/menu.h"
#include "sdk/bindings.h"
#include "utils/logger.h"
#include "utils/console.h"

namespace core
{
    Cheat& Cheat::Get()
    {
        static Cheat instance;
        return instance;
    }

    bool Cheat::Initialize(HMODULE module)
    {
        if (m_initialized)
            return true;

        m_module = module;

        // First thing: Initialize debug console
        if (!utils::DebugConsole::Get().Initialize("LethalMenu Debug Console"))
        {
            // Can't log this since console failed, but continue anyway
        }

        // Initialize exception handler
        auto sehResult = SEH_TRY("ExceptionHandler::Initialize", {
            ExceptionHandler::Get().Initialize();
            ExceptionHandler::Get().InstallGlobalHandler();
        });

        if (!sehResult)
        {
            LOG_ERROR("Failed to initialize exception handler: %s", sehResult.error.c_str());
        }

        LOG_INFO("====================================");
        LOG_INFO("  LethalMenu Initialization Start");
        LOG_INFO("====================================");

        // Initialize hooks first (MinHook)
        auto hooksResult = SEH_TRY("Hooks::Initialize", {
            if (!Hooks::Get().Initialize())
                throw std::runtime_error("Hook initialization failed");
        });

        if (!hooksResult)
        {
            LOG_ERROR("Failed to initialize hooks: %s", hooksResult.error.c_str());
            return false;
        }
        LOG_SUCCESS("Hook system initialized");

        // Initialize Mono runtime access
        auto monoResult = SEH_TRY("Mono::Initialize", {
            if (!Mono::Get().Initialize())
                throw std::runtime_error("Mono initialization failed");
        });

        if (!monoResult)
        {
            LOG_ERROR("Failed to initialize Mono: %s", monoResult.error.c_str());
            return false;
        }
        LOG_SUCCESS("Mono runtime initialized");

        // Initialize game bindings
        auto bindingsResult = SEH_TRY("GameBindings::Initialize", {
            if (!sdk::GameBindings::Get().Initialize())
                throw std::runtime_error("Bindings initialization failed");
        });

        if (!bindingsResult)
        {
            LOG_WARNING("Game bindings not ready: %s", bindingsResult.error.c_str());
            LOG_WARNING("This is normal if game is still loading");
        }
        else
        {
            LOG_SUCCESS("Game bindings initialized");
        }

        // Initialize D3D11 renderer (hooks Present)
        auto rendererResult = SEH_TRY("Renderer::Initialize", {
            if (!Renderer::Get().Initialize())
                throw std::runtime_error("Renderer initialization failed");
        });

        if (!rendererResult)
        {
            LOG_ERROR("Failed to initialize renderer: %s", rendererResult.error.c_str());
            return false;
        }
        LOG_SUCCESS("D3D11 renderer initialized");

        // Initialize menu
        auto menuResult = SEH_TRY("Menu::Initialize", {
            if (!gui::Menu::Get().Initialize())
                throw std::runtime_error("Menu initialization failed");
        });

        if (!menuResult)
        {
            LOG_ERROR("Failed to initialize menu: %s", menuResult.error.c_str());
            return false;
        }
        LOG_SUCCESS("Menu initialized");

        m_initialized = true;
        m_running = true;

        LOG_INFO("====================================");
        LOG_SUCCESS("LethalMenu initialized successfully");
        LOG_INFO("====================================");
        LOG_INFO("Press INSERT to toggle menu");
        LOG_INFO("Press END to unload");

        return true;
    }

    void Cheat::Shutdown()
    {
        if (!m_initialized)
            return;

        LOG_INFO("====================================");
        LOG_INFO("  LethalMenu Shutdown");
        LOG_INFO("====================================");

        m_running = false;

        // Shutdown subsystems in reverse order with SEH protection
        SEH_TRY("Menu::Shutdown", { gui::Menu::Get().Shutdown(); });
        LOG_INFO("Menu shutdown");

        SEH_TRY("Renderer::Shutdown", { Renderer::Get().Shutdown(); });
        LOG_INFO("Renderer shutdown");

        SEH_TRY("Hooks::Shutdown", { Hooks::Get().Shutdown(); });
        LOG_INFO("Hooks shutdown");

        SEH_TRY("ExceptionHandler::Shutdown", { ExceptionHandler::Get().Shutdown(); });

        m_initialized = false;

        LOG_SUCCESS("LethalMenu shutdown complete");

        // Give a moment to see the final messages
        Sleep(500);

        utils::DebugConsole::Get().Shutdown();
    }

    void Cheat::Run()
    {
        // Main loop - runs on separate thread
        while (m_running)
        {
            // Check for unload key (END)
            if (GetAsyncKeyState(VK_END) & 1)
            {
                LOG_INFO("Unload key pressed");
                m_running = false;
                break;
            }

            Sleep(100);
        }

        Shutdown();
        FreeLibraryAndExitThread(m_module, 0);
    }

    DWORD WINAPI Cheat::MainThread(LPVOID param)
    {
        auto* cheat = static_cast<Cheat*>(param);
        cheat->Run();
        return 0;
    }
}
