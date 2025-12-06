#pragma once

namespace features
{
    class ESPFeatures
    {
    public:
        static ESPFeatures& Get();

        void Render();

        // Master toggle
        bool enabled = false;

        // Object toggles
        bool players = true;
        bool enemies = true;
        bool items = true;
        bool ship = true;
        bool entrances = true;

        // Display options
        bool showDistance = true;
        bool showHealth = true;
        float maxDistance = 500.0f;

    private:
        ESPFeatures() = default;
        ~ESPFeatures() = default;

        ESPFeatures(const ESPFeatures&) = delete;
        ESPFeatures& operator=(const ESPFeatures&) = delete;

        void RenderPlayers();
        void RenderEnemies();
        void RenderItems();
        void RenderShip();
        void RenderEntrances();
    };
}
