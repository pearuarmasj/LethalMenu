#pragma once

#include "core/mono.h"
#include "sdk/unity.h"
#include <string>

namespace sdk
{
    // Cached Mono class/field references for fast access
    class GameBindings
    {
    public:
        static GameBindings& Get();

        bool Initialize();
        bool IsInitialized() const { return m_initialized; }

        // Get singleton instances
        MonoObject* GetStartOfRound();
        MonoObject* GetGameNetworkManager();
        MonoObject* GetLocalPlayerController();
        MonoObject* GetHUDManager();
        MonoObject* GetRoundManager();

        // Player access
        int GetPlayerHealth(MonoObject* player);
        void SetPlayerHealth(MonoObject* player, int health);

        float GetSprintMeter(MonoObject* player);
        void SetSprintMeter(MonoObject* player, float value);

        float GetMovementSpeed(MonoObject* player);
        void SetMovementSpeed(MonoObject* player, float speed);

        float GetJumpForce(MonoObject* player);
        void SetJumpForce(MonoObject* player, float force);

        bool IsPlayerDead(MonoObject* player);
        bool IsInsideFactory(MonoObject* player);

        float GetCarryWeight(MonoObject* player);
        void SetCarryWeight(MonoObject* player, float weight);

        float GetInsanityLevel(MonoObject* player);
        void SetInsanityLevel(MonoObject* player, float level);

        bool IsSpeedCheating(MonoObject* player);
        void SetSpeedCheating(MonoObject* player, bool value);

        bool IsSprinting(MonoObject* player);
        void SetSprinting(MonoObject* player, bool value);

        bool IsExhausted(MonoObject* player);
        void SetExhausted(MonoObject* player, bool value);

        bool IsPlayerControlled(MonoObject* player);

        std::string GetPlayerUsername(MonoObject* player);
        uint64_t GetPlayerClientId(MonoObject* player);
        uint64_t GetActualClientId(MonoObject* player);
        uint64_t GetPlayerSteamId(MonoObject* player);

        Vector3 GetPlayerPosition(MonoObject* player);
        void SetPlayerPosition(MonoObject* player, const Vector3& pos);

        // Item access
        MonoArray* GetPlayerItemSlots(MonoObject* player);
        MonoObject* GetCurrentlyHeldObject(MonoObject* player);
        int GetScrapValue(MonoObject* item);
        std::string GetItemName(MonoObject* item);

        // Player array
        MonoArray* GetAllPlayerScripts();
        int GetConnectedPlayersAmount();

        // Enemy access (iterate via FindObjectsOfType or similar)
        bool IsEnemyDead(MonoObject* enemy);
        std::string GetEnemyTypeName(MonoObject* enemy);
        Vector3 GetEnemyPosition(MonoObject* enemy);

        // Ship/Environment
        Vector3 GetShipPosition();
        bool IsShipLanded();
        bool IsShipLeaving();
        bool IsInShipPhase();
        float GetFearLevel();
        void SetFearLevel(float level);

    private:
        GameBindings() = default;

        bool CacheClasses();
        bool CacheFields();

        bool m_initialized = false;

        // Cached MonoImage for Assembly-CSharp
        MonoImage* m_gameImage = nullptr;

        // Cached classes
        MonoClass* m_playerControllerClass = nullptr;
        MonoClass* m_startOfRoundClass = nullptr;
        MonoClass* m_gameNetworkManagerClass = nullptr;
        MonoClass* m_hudManagerClass = nullptr;
        MonoClass* m_roundManagerClass = nullptr;
        MonoClass* m_enemyAIClass = nullptr;
        MonoClass* m_grabbableObjectClass = nullptr;

        // PlayerControllerB fields
        MonoClassField* m_field_health = nullptr;
        MonoClassField* m_field_sprintMeter = nullptr;
        MonoClassField* m_field_movementSpeed = nullptr;
        MonoClassField* m_field_jumpForce = nullptr;
        MonoClassField* m_field_isPlayerDead = nullptr;
        MonoClassField* m_field_isInsideFactory = nullptr;
        MonoClassField* m_field_carryWeight = nullptr;
        MonoClassField* m_field_playerUsername = nullptr;
        MonoClassField* m_field_playerClientId = nullptr;
        MonoClassField* m_field_playerSteamId = nullptr;
        MonoClassField* m_field_actualClientId = nullptr;  // Different from playerClientId
        MonoClassField* m_field_ItemSlots = nullptr;
        MonoClassField* m_field_currentlyHeldObject = nullptr;
        MonoClassField* m_field_thisPlayerBody = nullptr;
        MonoClassField* m_field_thisPlayerModel = nullptr;  // SkinnedMeshRenderer for ESP
        MonoClassField* m_field_playersManager = nullptr;   // StartOfRound reference
        MonoClassField* m_field_gameplayCamera = nullptr;   // Camera
        MonoClassField* m_field_isSpeedCheating = nullptr;
        MonoClassField* m_field_insanityLevel = nullptr;
        MonoClassField* m_field_insanitySpeedMultiplier = nullptr;
        MonoClassField* m_field_isSprinting = nullptr;
        MonoClassField* m_field_isExhausted = nullptr;
        MonoClassField* m_field_isPlayerControlled = nullptr;

        // StartOfRound fields
        MonoClassField* m_field_allPlayerScripts = nullptr;
        MonoClassField* m_field_connectedPlayersAmount = nullptr;
        MonoClassField* m_field_localPlayerController = nullptr;
        MonoClassField* m_field_shipHasLanded = nullptr;
        MonoClassField* m_field_shipIsLeaving = nullptr;
        MonoClassField* m_field_inShipPhase = nullptr;
        MonoClassField* m_field_fearLevel = nullptr;
        MonoClassField* m_field_fearLevelIncreasing = nullptr;
        MonoClassField* m_field_spectateCamera = nullptr;

        // GameNetworkManager fields
        MonoClassField* m_field_gnm_localPlayerController = nullptr;

        // GrabbableObject fields
        MonoClassField* m_field_scrapValue = nullptr;
        MonoClassField* m_field_itemProperties = nullptr;

        // EnemyAI fields
        MonoClassField* m_field_isEnemyDead = nullptr;
        MonoClassField* m_field_enemyType = nullptr;

        // Singleton property getters
        MonoProperty* m_prop_StartOfRound_Instance = nullptr;
        MonoProperty* m_prop_GameNetworkManager_Instance = nullptr;
        MonoProperty* m_prop_HUDManager_Instance = nullptr;
        MonoProperty* m_prop_RoundManager_Instance = nullptr;
    };
}
