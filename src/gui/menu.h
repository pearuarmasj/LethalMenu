#pragma once

#include <imgui.h>

namespace gui
{
    class Menu
    {
    public:
        static Menu& Get();

        bool Initialize();
        void Shutdown();

        void Render();
        void Toggle();

        bool IsOpen() const { return m_open; }

    private:
        Menu() = default;
        ~Menu() = default;

        Menu(const Menu&) = delete;
        Menu& operator=(const Menu&) = delete;

        void RenderPlayerTab();
        void RenderESPTab();
        void RenderMiscTab();
        void RenderSettingsTab();

        bool m_open = true;
        bool m_initialized = false;
    };
}
