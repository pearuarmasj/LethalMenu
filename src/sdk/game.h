#pragma once

#include "unity.h"

// Lethal Company game structures
// Uses Unity Mono (.NET) runtime, NOT IL2CPP
// Derived from Assembly-CSharp.dll disassembly
// 
// Reference implementations:
// - lc-hax (winstxnhdw/lc-hax): C# BepInEx mod, uses Helper.LocalPlayer pattern
// - Project Apparatus: C# BepInEx mod, uses GameObjectManager.Instance pattern
//
// NOTE: We access these via Mono runtime API (mono.h/bindings.h), not direct memory.

namespace sdk
{
    // Forward declarations
    struct PlayerControllerB;
    struct EnemyAI;
    struct GrabbableObject;
    struct StartOfRound;
    struct RoundManager;
    struct HUDManager;
    struct GameNetworkManager;

    // GameNetcodeStuff.PlayerControllerB - extends Unity.Netcode.NetworkBehaviour
    // All fields accessed via bindings.h GameBindings class
    // These struct definitions are for documentation only
    struct PlayerControllerB
    {
        // Transform references (MonoBehaviour inherits from Component which has transform)
        void* thisPlayerBody;           // Transform
        void* gameplayCamera;           // Camera
        void* playerEye;                // Transform
        void* thisController;           // CharacterController
        void* thisPlayerModel;          // SkinnedMeshRenderer (for ESP rendering)

        // Movement
        float movementSpeed;
        float sprintMeter;              // Stamina (0.0 - 1.0)
        float sprintMultiplier;
        bool isSprinting;
        bool isExhausted;
        bool isSpeedCheating;           // Server-side speed cheat detection flag
        float jumpForce;                // Default: 13.0
        bool isJumping;
        float fallValue;
        bool isCrouching;
        int isMovementHindered;
        float hinderedMultiplier;

        // Health/Status
        int health;                     // Default: 100
        bool criticallyInjured;
        bool isPlayerDead;
        float carryWeight;              // 1.0 = no items, increases with item weight
        bool bleedingHeavily;
        float insanityLevel;            // Fear/sanity system
        float insanitySpeedMultiplier;

        // Items
        void** ItemSlots;               // GrabbableObject[]
        int currentItemSlot;
        void* currentlyHeldObject;      // GrabbableObject
        bool isHoldingObject;
        bool twoHanded;

        // State
        bool isInsideFactory;
        bool isInElevator;
        bool isInHangarShipRoom;
        bool isPlayerControlled;
        uint64_t playerClientId;
        uint64_t actualClientId;        // Network actual client ID (different from playerClientId)
        const char* playerUsername;
        uint64_t playerSteamId;

        // References
        void* playersManager;           // StartOfRound reference

        // Position sync
        Vector3 serverPlayerPosition;
        void* physicsParent;            // Transform
    };

    // EnemyAI - base class for all enemies
    struct EnemyAI
    {
        void* transform;                // Unity Transform
        void* enemyType;                // EnemyType scriptable object
        bool isEnemyDead;
        void* targetPlayer;             // PlayerControllerB
        void* stunnedByPlayer;          // PlayerControllerB
    };

    // GrabbableObject - base class for all items
    struct GrabbableObject
    {
        void* transform;                // Unity Transform
        void* itemProperties;           // Item scriptable object
        int scrapValue;
        bool isHeld;
        bool isPocketed;
        void* playerHeldBy;             // PlayerControllerB
    };

    // StartOfRound - main game manager singleton (StartOfRound.Instance)
    struct StartOfRound
    {
        void** allPlayerScripts;        // PlayerControllerB[]
        int connectedPlayersAmount;
        void* localPlayerController;    // PlayerControllerB
        void* shipBounds;               // Transform
        bool inShipPhase;
        bool shipHasLanded;
        bool shipIsLeaving;
        float fearLevel;                // Global fear level
        bool fearLevelIncreasing;
        void* spectateCamera;           // Camera for dead players
    };

    // GameNetworkManager singleton (GameNetworkManager.Instance)
    struct GameNetworkManager
    {
        void* localPlayerController;    // PlayerControllerB
    };

    // Helper to get game instances via Mono runtime
    class GameState
    {
    public:
        static GameState& Get();

        // Cached game object pointers (updated each frame)
        StartOfRound* startOfRound = nullptr;
        PlayerControllerB* localPlayer = nullptr;
        RoundManager* roundManager = nullptr;
        HUDManager* hudManager = nullptr;
        GameNetworkManager* gameNetworkManager = nullptr;

        void Update();
        bool IsInGame() const;

    private:
        GameState() = default;
    };
}
