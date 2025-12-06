#pragma once

namespace features
{
    class MiscFeatures
    {
    public:
        static MiscFeatures& Get();

        void Update();

        // Visual toggles
        bool noFog = false;
        bool fullbright = false;
        bool alwaysShowClock = false;

        // Teleport triggers (reset after use)
        bool teleportToShip = false;
        bool teleportToEntrance = false;

    private:
        MiscFeatures() = default;
        ~MiscFeatures() = default;

        MiscFeatures(const MiscFeatures&) = delete;
        MiscFeatures& operator=(const MiscFeatures&) = delete;

        void HandleTeleports();
    };
}
