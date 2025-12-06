#pragma once

namespace features
{
    class PlayerFeatures
    {
    public:
        static PlayerFeatures& Get();

        void Update();

        // Feature toggles
        bool godMode = false;
        bool infiniteStamina = false;
        bool infiniteBattery = false;
        bool noFallDamage = false;

        // Feature values
        float speedMultiplier = 1.0f;
        float jumpForce = 13.0f;  // Default game value

    private:
        PlayerFeatures() = default;
        ~PlayerFeatures() = default;

        PlayerFeatures(const PlayerFeatures&) = delete;
        PlayerFeatures& operator=(const PlayerFeatures&) = delete;
    };
}
