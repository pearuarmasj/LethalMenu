#pragma once

#include <Windows.h>

namespace core
{
    class Cheat
    {
    public:
        static Cheat& Get();

        bool Initialize(HMODULE module);
        void Shutdown();
        void Run();

        HMODULE GetModule() const { return m_module; }
        bool IsRunning() const { return m_running; }

    private:
        Cheat() = default;
        ~Cheat() = default;

        Cheat(const Cheat&) = delete;
        Cheat& operator=(const Cheat&) = delete;

        static DWORD WINAPI MainThread(LPVOID param);

        HMODULE m_module = nullptr;
        bool m_running = false;
        bool m_initialized = false;
    };
}
