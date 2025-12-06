// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "core/cheat.h"
#include "utils/logger.h"

DWORD WINAPI MainThread(LPVOID lpParam)
{
    auto module = static_cast<HMODULE>(lpParam);

    utils::Logger::Get().Initialize();
    LOG_INFO("LethalMenu injected");

    if (!core::Cheat::Get().Initialize(module))
    {
        LOG_ERROR("Failed to initialize cheat");
        utils::Logger::Get().Shutdown();
        FreeLibraryAndExitThread(module, 1);
        return 1;
    }

    LOG_INFO("Cheat initialized successfully");
    core::Cheat::Get().Run();

    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD  ul_reason_for_call,
                      LPVOID lpReserved)
{
    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hModule);
        HANDLE hThread = CreateThread(nullptr, 0, MainThread, hModule, 0, nullptr);
        if (hThread)
            CloseHandle(hThread);
    }
    return TRUE;
}


