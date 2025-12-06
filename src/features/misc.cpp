#include "pch.h"
#include "misc.h"

namespace features
{
    MiscFeatures& MiscFeatures::Get()
    {
        static MiscFeatures instance;
        return instance;
    }

    void MiscFeatures::Update()
    {
        // Handle one-shot actions
        HandleTeleports();

        if (noFog)
        {
            // TODO: Disable fog rendering
        }

        if (fullbright)
        {
            // TODO: Set ambient light to max
        }

        if (alwaysShowClock)
        {
            // TODO: Force clock visibility
        }
    }

    void MiscFeatures::HandleTeleports()
    {
        if (teleportToShip)
        {
            teleportToShip = false;
            // TODO: Get ship position and teleport player
        }

        if (teleportToEntrance)
        {
            teleportToEntrance = false;
            // TODO: Get entrance position and teleport player
        }
    }
}
