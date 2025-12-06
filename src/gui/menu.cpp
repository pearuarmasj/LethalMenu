#include "pch.h"
#include "menu.h"
#include "features/features.h"

namespace gui
{
    Menu& Menu::Get()
    {
        static Menu instance;
        return instance;
    }

    bool Menu::Initialize()
    {
        if (m_initialized)
            return true;

        // ImGui initialization will be done after D3D hook is set up
        m_initialized = true;
        return true;
    }

    void Menu::Shutdown()
    {
        if (!m_initialized)
            return;

        m_initialized = false;
    }

    void Menu::Toggle()
    {
        m_open = !m_open;
    }

    void Menu::Render()
    {
        if (!m_open)
            return;

        ImGui::SetNextWindowSize(ImVec2(500, 400), ImGuiCond_FirstUseEver);

        if (ImGui::Begin("Lethal Menu", &m_open, ImGuiWindowFlags_NoCollapse))
        {
            if (ImGui::BeginTabBar("MainTabs"))
            {
                if (ImGui::BeginTabItem("Player"))
                {
                    RenderPlayerTab();
                    ImGui::EndTabItem();
                }

                if (ImGui::BeginTabItem("ESP"))
                {
                    RenderESPTab();
                    ImGui::EndTabItem();
                }

                if (ImGui::BeginTabItem("Misc"))
                {
                    RenderMiscTab();
                    ImGui::EndTabItem();
                }

                if (ImGui::BeginTabItem("Settings"))
                {
                    RenderSettingsTab();
                    ImGui::EndTabItem();
                }

                ImGui::EndTabBar();
            }
        }
        ImGui::End();
    }

    void Menu::RenderPlayerTab()
    {
        auto& player = features::PlayerFeatures::Get();

        ImGui::Checkbox("God Mode", &player.godMode);
        ImGui::Checkbox("Infinite Stamina", &player.infiniteStamina);
        ImGui::Checkbox("Infinite Battery", &player.infiniteBattery);
        ImGui::Checkbox("No Fall Damage", &player.noFallDamage);

        ImGui::Separator();

        ImGui::SliderFloat("Speed Multiplier", &player.speedMultiplier, 1.0f, 10.0f);
        ImGui::SliderFloat("Jump Force", &player.jumpForce, 1.0f, 20.0f);
    }

    void Menu::RenderESPTab()
    {
        auto& esp = features::ESPFeatures::Get();

        ImGui::Checkbox("Enable ESP", &esp.enabled);

        ImGui::Separator();

        ImGui::Checkbox("Players", &esp.players);
        ImGui::Checkbox("Enemies", &esp.enemies);
        ImGui::Checkbox("Items", &esp.items);
        ImGui::Checkbox("Ship", &esp.ship);
        ImGui::Checkbox("Entrance/Exit", &esp.entrances);

        ImGui::Separator();

        ImGui::Checkbox("Show Distance", &esp.showDistance);
        ImGui::Checkbox("Show Health", &esp.showHealth);
        ImGui::SliderFloat("Max Distance", &esp.maxDistance, 100.0f, 2000.0f);
    }

    void Menu::RenderMiscTab()
    {
        auto& misc = features::MiscFeatures::Get();

        ImGui::Checkbox("No Fog", &misc.noFog);
        ImGui::Checkbox("Fullbright", &misc.fullbright);
        ImGui::Checkbox("Always Show Clock", &misc.alwaysShowClock);

        ImGui::Separator();

        if (ImGui::Button("Teleport to Ship"))
        {
            misc.teleportToShip = true;
        }

        if (ImGui::Button("Teleport to Entrance"))
        {
            misc.teleportToEntrance = true;
        }
    }

    void Menu::RenderSettingsTab()
    {
        ImGui::Text("Menu Toggle Key: INSERT");
        ImGui::Text("Unload Key: END");

        ImGui::Separator();

        if (ImGui::Button("Save Config"))
        {
            // TODO: Implement config save
        }

        ImGui::SameLine();

        if (ImGui::Button("Load Config"))
        {
            // TODO: Implement config load
        }
    }
}
