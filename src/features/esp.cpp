#include "pch.h"
#include "esp.h"

namespace features
{
    ESPFeatures& ESPFeatures::Get()
    {
        static ESPFeatures instance;
        return instance;
    }

    void ESPFeatures::Render()
    {
        if (!enabled)
            return;

        // TODO: Implement world-to-screen projection
        // TODO: Get camera matrices from Unity

        if (players)
            RenderPlayers();

        if (enemies)
            RenderEnemies();

        if (items)
            RenderItems();

        if (ship)
            RenderShip();

        if (entrances)
            RenderEntrances();
    }

    void ESPFeatures::RenderPlayers()
    {
        // TODO: Iterate PlayerControllerB instances
        // TODO: Draw boxes/names/health
    }

    void ESPFeatures::RenderEnemies()
    {
        // TODO: Iterate EnemyAI instances
        // TODO: Draw boxes/names/type
    }

    void ESPFeatures::RenderItems()
    {
        // TODO: Iterate GrabbableObject instances
        // TODO: Draw item names/values
    }

    void ESPFeatures::RenderShip()
    {
        // TODO: Get ship position
        // TODO: Draw ship indicator
    }

    void ESPFeatures::RenderEntrances()
    {
        // TODO: Get entrance/exit positions
        // TODO: Draw entrance indicators
    }
}
