#include "pch.h"
#include "player.h"
#include "sdk/bindings.h"
#include "core/exception.h"
#include "utils/logger.h"

namespace features
{
    PlayerFeatures& PlayerFeatures::Get()
    {
        static PlayerFeatures instance;
        return instance;
    }

    void PlayerFeatures::Update()
    {
        auto& bindings = sdk::GameBindings::Get();
        if (!bindings.IsInitialized())
            return;

        // Don't do anything if we're not in a game
        if (!bindings.IsInGame())
            return;

        // Get local player
        MonoObject* localPlayer = bindings.GetLocalPlayerController();
        if (!localPlayer)
            return;

        // Check if player is dead (with null safety)
        bool isDead = bindings.IsPlayerDead(localPlayer);
        if (isDead)
            return;

        // God Mode - prevent death and keep health at 100
        static bool wasGodModeEnabled = false;
        if (godMode)
        {
            SEH_TRY("GodMode", {
                // Disable death by setting allowLocalPlayerDeath to false
                bindings.SetAllowLocalPlayerDeath(false);

                // Keep health at 100
                int currentHealth = bindings.GetPlayerHealth(localPlayer);
                if (currentHealth < 100)
                {
                    bindings.SetPlayerHealth(localPlayer, 100);
                }

                // Prevent critically injured state (limping, screen effects)
                bindings.SetCriticallyInjured(localPlayer, false);
            });
            wasGodModeEnabled = true;
        }
        else if (wasGodModeEnabled)
        {
            // Re-enable death when god mode is turned off
            SEH_TRY("GodModeDisable", {
                bindings.SetAllowLocalPlayerDeath(true);
            });
            wasGodModeEnabled = false;
        }

        // Infinite Stamina - keep sprint meter full
        if (infiniteStamina)
        {
            SEH_TRY("InfiniteStamina", {
                float currentStamina = bindings.GetSprintMeter(localPlayer);
                if (currentStamina < 1.0f)
                {
                    bindings.SetSprintMeter(localPlayer, 1.0f);
                    bindings.SetExhausted(localPlayer, false);
                }
            });
        }

        // Infinite Battery - TODO: needs GrabbableObject iteration
        if (infiniteBattery)
        {
            // TODO: Iterate player's held items and set battery to max
        }

        // No Fall Damage - TODO
        if (noFallDamage)
        {
            // TODO: Either hook damage function or zero out fall velocity tracking
        }

        // Speed Multiplier - only apply if not default
        if (speedMultiplier != 1.0f)
        {
            SEH_TRY("SpeedMultiplier", {
                bindings.SetMovementSpeed(localPlayer, 4.6f * speedMultiplier);
            });
        }

        // Jump Force - only apply if changed from default
        if (jumpForce != 13.0f)
        {
            SEH_TRY("JumpForce", {
                bindings.SetJumpForce(localPlayer, jumpForce);
            });
        }
    }
}
