#include "pch.h"
#include "bindings.h"
#include "utils/logger.h"

namespace sdk
{
    GameBindings& GameBindings::Get()
    {
        static GameBindings instance;
        return instance;
    }

    bool GameBindings::Initialize()
    {
        if (m_initialized)
            return true;

        auto& mono = core::Mono::Get();
        if (!mono.IsInitialized())
        {
            LOG_ERROR("Mono not initialized");
            return false;
        }

        if (!CacheClasses())
        {
            LOG_ERROR("Failed to cache game classes");
            return false;
        }

        if (!CacheFields())
        {
            LOG_ERROR("Failed to cache game fields");
            return false;
        }

        m_initialized = true;
        LOG_INFO("Game bindings initialized");
        return true;
    }

    bool GameBindings::CacheClasses()
    {
        auto& mono = core::Mono::Get();

        // Get Assembly-CSharp image
        m_gameImage = mono.GetImageByName("Assembly-CSharp");
        if (!m_gameImage)
        {
            LOG_ERROR("Failed to find Assembly-CSharp");
            return false;
        }

        // PlayerControllerB is in GameNetcodeStuff namespace
        m_playerControllerClass = mono.GetClass(m_gameImage, "GameNetcodeStuff", "PlayerControllerB");
        if (!m_playerControllerClass)
        {
            LOG_ERROR("Failed to find PlayerControllerB class");
            return false;
        }

        // These are in the global namespace
        m_startOfRoundClass = mono.GetClass(m_gameImage, "", "StartOfRound");
        m_gameNetworkManagerClass = mono.GetClass(m_gameImage, "", "GameNetworkManager");
        m_hudManagerClass = mono.GetClass(m_gameImage, "", "HUDManager");
        m_roundManagerClass = mono.GetClass(m_gameImage, "", "RoundManager");
        m_enemyAIClass = mono.GetClass(m_gameImage, "", "EnemyAI");
        m_grabbableObjectClass = mono.GetClass(m_gameImage, "", "GrabbableObject");

        if (!m_startOfRoundClass || !m_gameNetworkManagerClass)
        {
            LOG_ERROR("Failed to find essential game classes");
            return false;
        }

        LOG_INFO("Cached game classes");
        return true;
    }

    bool GameBindings::CacheFields()
    {
        auto& mono = core::Mono::Get();

        // PlayerControllerB fields - core
        m_field_health = mono.GetField(m_playerControllerClass, "health");
        m_field_sprintMeter = mono.GetField(m_playerControllerClass, "sprintMeter");
        m_field_movementSpeed = mono.GetField(m_playerControllerClass, "movementSpeed");
        m_field_jumpForce = mono.GetField(m_playerControllerClass, "jumpForce");
        m_field_isPlayerDead = mono.GetField(m_playerControllerClass, "isPlayerDead");
        m_field_isInsideFactory = mono.GetField(m_playerControllerClass, "isInsideFactory");
        m_field_carryWeight = mono.GetField(m_playerControllerClass, "carryWeight");
        m_field_playerUsername = mono.GetField(m_playerControllerClass, "playerUsername");
        m_field_playerClientId = mono.GetField(m_playerControllerClass, "playerClientId");
        m_field_playerSteamId = mono.GetField(m_playerControllerClass, "playerSteamId");
        m_field_actualClientId = mono.GetField(m_playerControllerClass, "actualClientId");
        m_field_ItemSlots = mono.GetField(m_playerControllerClass, "ItemSlots");
        m_field_currentlyHeldObject = mono.GetField(m_playerControllerClass, "currentlyHeldObject");
        m_field_thisPlayerBody = mono.GetField(m_playerControllerClass, "thisPlayerBody");
        m_field_thisPlayerModel = mono.GetField(m_playerControllerClass, "thisPlayerModel");
        m_field_playersManager = mono.GetField(m_playerControllerClass, "playersManager");
        m_field_gameplayCamera = mono.GetField(m_playerControllerClass, "gameplayCamera");

        // PlayerControllerB fields - status
        m_field_isSpeedCheating = mono.GetField(m_playerControllerClass, "isSpeedCheating");
        m_field_insanityLevel = mono.GetField(m_playerControllerClass, "insanityLevel");
        m_field_insanitySpeedMultiplier = mono.GetField(m_playerControllerClass, "insanitySpeedMultiplier");
        m_field_isSprinting = mono.GetField(m_playerControllerClass, "isSprinting");
        m_field_isExhausted = mono.GetField(m_playerControllerClass, "isExhausted");
        m_field_isPlayerControlled = mono.GetField(m_playerControllerClass, "isPlayerControlled");
        m_field_criticallyInjured = mono.GetField(m_playerControllerClass, "criticallyInjured");
        m_field_bleedingHeavily = mono.GetField(m_playerControllerClass, "bleedingHeavily");

        // StartOfRound fields
        m_field_allPlayerScripts = mono.GetField(m_startOfRoundClass, "allPlayerScripts");
        m_field_connectedPlayersAmount = mono.GetField(m_startOfRoundClass, "connectedPlayersAmount");
        m_field_localPlayerController = mono.GetField(m_startOfRoundClass, "localPlayerController");
        m_field_shipHasLanded = mono.GetField(m_startOfRoundClass, "shipHasLanded");
        m_field_shipIsLeaving = mono.GetField(m_startOfRoundClass, "shipIsLeaving");
        m_field_inShipPhase = mono.GetField(m_startOfRoundClass, "inShipPhase");
        m_field_fearLevel = mono.GetField(m_startOfRoundClass, "fearLevel");
        m_field_fearLevelIncreasing = mono.GetField(m_startOfRoundClass, "fearLevelIncreasing");
        m_field_spectateCamera = mono.GetField(m_startOfRoundClass, "spectateCamera");
        m_field_allowLocalPlayerDeath = mono.GetField(m_startOfRoundClass, "allowLocalPlayerDeath");

        // GameNetworkManager fields
        m_field_gnm_localPlayerController = mono.GetField(m_gameNetworkManagerClass, "localPlayerController");

        // GrabbableObject fields
        if (m_grabbableObjectClass)
        {
            m_field_scrapValue = mono.GetField(m_grabbableObjectClass, "scrapValue");
            m_field_itemProperties = mono.GetField(m_grabbableObjectClass, "itemProperties");
        }

        // EnemyAI fields
        if (m_enemyAIClass)
        {
            m_field_isEnemyDead = mono.GetField(m_enemyAIClass, "isEnemyDead");
            m_field_enemyType = mono.GetField(m_enemyAIClass, "enemyType");
        }

        // Field enumeration available for debugging if needed
        // mono.EnumerateClassFields(m_gameNetworkManagerClass);
        // mono.EnumerateClassFields(m_startOfRoundClass);

        // Singleton static backing fields (C# auto-property backing field naming convention)
        // These are static fields, so we read them with GetStaticFieldValue
        m_field_StartOfRound_Instance = mono.GetField(m_startOfRoundClass, "<Instance>k__BackingField");
        m_field_GameNetworkManager_Instance = mono.GetField(m_gameNetworkManagerClass, "<Instance>k__BackingField");

        // Debug: log if backing fields were found
        if (!m_field_StartOfRound_Instance)
            LOG_WARN("StartOfRound Instance backing field not found - will use property getter");
        else
            LOG_INFO("StartOfRound.<Instance>k__BackingField found: %p", m_field_StartOfRound_Instance);
        if (!m_field_GameNetworkManager_Instance)
            LOG_WARN("GameNetworkManager Instance backing field not found - will use property getter");
        else
            LOG_INFO("GameNetworkManager.<Instance>k__BackingField found: %p", m_field_GameNetworkManager_Instance);

        // Cache property getters as fallback
        m_prop_StartOfRound_Instance = mono.GetProperty(m_startOfRoundClass, "Instance");
        m_prop_GameNetworkManager_Instance = mono.GetProperty(m_gameNetworkManagerClass, "Instance");

        if (m_prop_StartOfRound_Instance)
            LOG_INFO("StartOfRound.Instance property found");
        if (m_prop_GameNetworkManager_Instance)
            LOG_INFO("GameNetworkManager.Instance property found");

        if (m_hudManagerClass)
            m_field_HUDManager_Instance = mono.GetField(m_hudManagerClass, "<Instance>k__BackingField");
        if (m_roundManagerClass)
            m_field_RoundManager_Instance = mono.GetField(m_roundManagerClass, "<Instance>k__BackingField");

        // Validate essential fields
        if (!m_field_health || !m_field_sprintMeter || !m_field_isPlayerDead)
        {
            LOG_ERROR("Failed to find essential PlayerControllerB fields");
            return false;
        }

        LOG_INFO("Cached game fields");
        return true;
    }

    // Check if we're in game by seeing if GameNetworkManager.Instance.localPlayerController exists
    // This caches the result to avoid expensive checks every frame
    // All Mono calls are now SEH-protected in mono.cpp, so this should never crash
    bool GameBindings::IsInGame()
    {
        if (!m_initialized)
            return false;

        // Try to get the GNM singleton via backing field first, then property getter
        MonoObject* gnm = nullptr;

        if (m_field_GameNetworkManager_Instance && m_gameNetworkManagerClass)
        {
            gnm = static_cast<MonoObject*>(
                core::Mono::Get().GetStaticFieldValue(m_gameNetworkManagerClass, m_field_GameNetworkManager_Instance));
        }

        // Fallback to property getter if backing field didn't work
        if (!gnm && m_prop_GameNetworkManager_Instance)
        {
            gnm = core::Mono::Get().GetPropertyValue(nullptr, m_prop_GameNetworkManager_Instance);
        }
        
        if (!gnm)
        {
            if (m_inGame)
            {
                LOG_INFO("Left game");
                m_inGame = false;
            }
            return false;
        }

        // GNM exists, check if localPlayerController is set
        if (!m_field_gnm_localPlayerController)
        {
            if (m_inGame)
            {
                LOG_INFO("Left game");
                m_inGame = false;
            }
            return false;
        }

        auto* player = static_cast<MonoObject*>(core::Mono::Get().GetFieldValue(gnm, m_field_gnm_localPlayerController));

        bool wasInGame = m_inGame;
        m_inGame = (player != nullptr);
        
        // Log transitions
        if (m_inGame && !wasInGame)
        {
            LOG_INFO("Entered game");
        }
        else if (!m_inGame && wasInGame)
        {
            LOG_INFO("Left game");
        }
        
        return m_inGame;
    }

    // Singleton accessors - use static field reads instead of property invocations
    MonoObject* GameBindings::GetStartOfRound()
    {
        // Try backing field first
        if (m_field_StartOfRound_Instance && m_startOfRoundClass)
        {
            auto* result = static_cast<MonoObject*>(
                core::Mono::Get().GetStaticFieldValue(m_startOfRoundClass, m_field_StartOfRound_Instance));
            if (result)
                return result;
        }

        // Fallback to property getter
        if (m_prop_StartOfRound_Instance)
            return core::Mono::Get().GetPropertyValue(nullptr, m_prop_StartOfRound_Instance);

        return nullptr;
    }

    MonoObject* GameBindings::GetGameNetworkManager()
    {
        // Try backing field first
        if (m_field_GameNetworkManager_Instance && m_gameNetworkManagerClass)
        {
            auto* result = static_cast<MonoObject*>(
                core::Mono::Get().GetStaticFieldValue(m_gameNetworkManagerClass, m_field_GameNetworkManager_Instance));
            if (result)
                return result;
        }

        // Fallback to property getter
        if (m_prop_GameNetworkManager_Instance)
            return core::Mono::Get().GetPropertyValue(nullptr, m_prop_GameNetworkManager_Instance);

        return nullptr;
    }

    MonoObject* GameBindings::GetLocalPlayerController()
    {
        // Fast path: if we know we're not in game, don't even try
        if (!m_inGame && !IsInGame())
            return nullptr;

        auto* gnm = GetGameNetworkManager();
        if (!gnm || !m_field_gnm_localPlayerController)
            return nullptr;

        return static_cast<MonoObject*>(core::Mono::Get().GetFieldValue(gnm, m_field_gnm_localPlayerController));
    }

    MonoObject* GameBindings::GetHUDManager()
    {
        if (!m_field_HUDManager_Instance || !m_hudManagerClass)
            return nullptr;

        return static_cast<MonoObject*>(
            core::Mono::Get().GetStaticFieldValue(m_hudManagerClass, m_field_HUDManager_Instance));
    }

    MonoObject* GameBindings::GetRoundManager()
    {
        if (!m_field_RoundManager_Instance || !m_roundManagerClass)
            return nullptr;

        return static_cast<MonoObject*>(
            core::Mono::Get().GetStaticFieldValue(m_roundManagerClass, m_field_RoundManager_Instance));
    }

    // Player field accessors
    int GameBindings::GetPlayerHealth(MonoObject* player)
    {
        if (!player || !m_field_health)
            return 0;

        return core::Mono::Get().GetFieldValue<int>(player, m_field_health);
    }

    void GameBindings::SetPlayerHealth(MonoObject* player, int health)
    {
        if (!player || !m_field_health)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_health, health);
    }

    float GameBindings::GetSprintMeter(MonoObject* player)
    {
        if (!player || !m_field_sprintMeter)
            return 0.0f;

        return core::Mono::Get().GetFieldValue<float>(player, m_field_sprintMeter);
    }

    void GameBindings::SetSprintMeter(MonoObject* player, float value)
    {
        if (!player || !m_field_sprintMeter)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_sprintMeter, value);
    }

    float GameBindings::GetMovementSpeed(MonoObject* player)
    {
        if (!player || !m_field_movementSpeed)
            return 0.0f;

        return core::Mono::Get().GetFieldValue<float>(player, m_field_movementSpeed);
    }

    void GameBindings::SetMovementSpeed(MonoObject* player, float speed)
    {
        if (!player || !m_field_movementSpeed)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_movementSpeed, speed);
    }

    float GameBindings::GetJumpForce(MonoObject* player)
    {
        if (!player || !m_field_jumpForce)
            return 13.0f;

        return core::Mono::Get().GetFieldValue<float>(player, m_field_jumpForce);
    }

    void GameBindings::SetJumpForce(MonoObject* player, float force)
    {
        if (!player || !m_field_jumpForce)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_jumpForce, force);
    }

    bool GameBindings::IsPlayerDead(MonoObject* player)
    {
        if (!player || !m_field_isPlayerDead)
            return true;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_isPlayerDead);
    }

    bool GameBindings::IsInsideFactory(MonoObject* player)
    {
        if (!player || !m_field_isInsideFactory)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_isInsideFactory);
    }

    float GameBindings::GetCarryWeight(MonoObject* player)
    {
        if (!player || !m_field_carryWeight)
            return 1.0f;

        return core::Mono::Get().GetFieldValue<float>(player, m_field_carryWeight);
    }

    void GameBindings::SetCarryWeight(MonoObject* player, float weight)
    {
        if (!player || !m_field_carryWeight)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_carryWeight, weight);
    }

    std::string GameBindings::GetPlayerUsername(MonoObject* player)
    {
        if (!player || !m_field_playerUsername)
            return "";

        auto* str = static_cast<MonoString*>(core::Mono::Get().GetFieldValue(player, m_field_playerUsername));
        return core::Mono::Get().MonoStringToUTF8(str);
    }

    uint64_t GameBindings::GetPlayerClientId(MonoObject* player)
    {
        if (!player || !m_field_playerClientId)
            return 0;

        return core::Mono::Get().GetFieldValue<uint64_t>(player, m_field_playerClientId);
    }

    uint64_t GameBindings::GetPlayerSteamId(MonoObject* player)
    {
        if (!player || !m_field_playerSteamId)
            return 0;

        return core::Mono::Get().GetFieldValue<uint64_t>(player, m_field_playerSteamId);
    }

    uint64_t GameBindings::GetActualClientId(MonoObject* player)
    {
        if (!player || !m_field_actualClientId)
            return 0;

        return core::Mono::Get().GetFieldValue<uint64_t>(player, m_field_actualClientId);
    }

    float GameBindings::GetInsanityLevel(MonoObject* player)
    {
        if (!player || !m_field_insanityLevel)
            return 0.0f;

        return core::Mono::Get().GetFieldValue<float>(player, m_field_insanityLevel);
    }

    void GameBindings::SetInsanityLevel(MonoObject* player, float level)
    {
        if (!player || !m_field_insanityLevel)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_insanityLevel, level);
    }

    bool GameBindings::IsSpeedCheating(MonoObject* player)
    {
        if (!player || !m_field_isSpeedCheating)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_isSpeedCheating);
    }

    void GameBindings::SetSpeedCheating(MonoObject* player, bool value)
    {
        if (!player || !m_field_isSpeedCheating)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_isSpeedCheating, value);
    }

    bool GameBindings::IsSprinting(MonoObject* player)
    {
        if (!player || !m_field_isSprinting)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_isSprinting);
    }

    void GameBindings::SetSprinting(MonoObject* player, bool value)
    {
        if (!player || !m_field_isSprinting)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_isSprinting, value);
    }

    bool GameBindings::IsExhausted(MonoObject* player)
    {
        if (!player || !m_field_isExhausted)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_isExhausted);
    }

    void GameBindings::SetExhausted(MonoObject* player, bool value)
    {
        if (!player || !m_field_isExhausted)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_isExhausted, value);
    }

    bool GameBindings::IsPlayerControlled(MonoObject* player)
    {
        if (!player || !m_field_isPlayerControlled)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_isPlayerControlled);
    }

    Vector3 GameBindings::GetPlayerPosition(MonoObject* player)
    {
        // TODO: Get position from Transform component
        // This requires calling Transform.get_position() via Mono
        return Vector3();
    }

    void GameBindings::SetPlayerPosition(MonoObject* player, const Vector3& pos)
    {
        // TODO: Set position via Transform component
    }

    MonoArray* GameBindings::GetPlayerItemSlots(MonoObject* player)
    {
        if (!player || !m_field_ItemSlots)
            return nullptr;

        return static_cast<MonoArray*>(core::Mono::Get().GetFieldValue(player, m_field_ItemSlots));
    }

    MonoObject* GameBindings::GetCurrentlyHeldObject(MonoObject* player)
    {
        if (!player || !m_field_currentlyHeldObject)
            return nullptr;

        return static_cast<MonoObject*>(core::Mono::Get().GetFieldValue(player, m_field_currentlyHeldObject));
    }

    int GameBindings::GetScrapValue(MonoObject* item)
    {
        if (!item || !m_field_scrapValue)
            return 0;

        return core::Mono::Get().GetFieldValue<int>(item, m_field_scrapValue);
    }

    std::string GameBindings::GetItemName(MonoObject* item)
    {
        // TODO: Get name from itemProperties.itemName
        return "";
    }

    MonoArray* GameBindings::GetAllPlayerScripts()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_allPlayerScripts)
            return nullptr;

        return static_cast<MonoArray*>(core::Mono::Get().GetFieldValue(sor, m_field_allPlayerScripts));
    }

    int GameBindings::GetConnectedPlayersAmount()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_connectedPlayersAmount)
            return 0;

        return core::Mono::Get().GetFieldValue<int>(sor, m_field_connectedPlayersAmount);
    }

    bool GameBindings::IsEnemyDead(MonoObject* enemy)
    {
        if (!enemy || !m_field_isEnemyDead)
            return true;

        return core::Mono::Get().GetFieldValue<bool>(enemy, m_field_isEnemyDead);
    }

    std::string GameBindings::GetEnemyTypeName(MonoObject* enemy)
    {
        // TODO: Get from enemyType.enemyName
        return "";
    }

    Vector3 GameBindings::GetEnemyPosition(MonoObject* enemy)
    {
        // TODO: Get from Transform
        return Vector3();
    }

    Vector3 GameBindings::GetShipPosition()
    {
        // TODO: Get from StartOfRound.shipBounds or similar
        return Vector3();
    }

    bool GameBindings::IsShipLanded()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_shipHasLanded)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(sor, m_field_shipHasLanded);
    }

    bool GameBindings::IsShipLeaving()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_shipIsLeaving)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(sor, m_field_shipIsLeaving);
    }

    bool GameBindings::IsInShipPhase()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_inShipPhase)
            return true;

        return core::Mono::Get().GetFieldValue<bool>(sor, m_field_inShipPhase);
    }

    float GameBindings::GetFearLevel()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_fearLevel)
            return 0.0f;

        return core::Mono::Get().GetFieldValue<float>(sor, m_field_fearLevel);
    }

    void GameBindings::SetFearLevel(float level)
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_fearLevel)
            return;

        core::Mono::Get().SetFieldValue(sor, m_field_fearLevel, level);
    }

    // God Mode support
    bool GameBindings::GetAllowLocalPlayerDeath()
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_allowLocalPlayerDeath)
            return true;

        return core::Mono::Get().GetFieldValue<bool>(sor, m_field_allowLocalPlayerDeath);
    }

    void GameBindings::SetAllowLocalPlayerDeath(bool allow)
    {
        auto* sor = GetStartOfRound();
        if (!sor || !m_field_allowLocalPlayerDeath)
            return;

        core::Mono::Get().SetFieldValue(sor, m_field_allowLocalPlayerDeath, allow);
    }

    bool GameBindings::GetCriticallyInjured(MonoObject* player)
    {
        if (!player || !m_field_criticallyInjured)
            return false;

        return core::Mono::Get().GetFieldValue<bool>(player, m_field_criticallyInjured);
    }

    void GameBindings::SetCriticallyInjured(MonoObject* player, bool value)
    {
        if (!player || !m_field_criticallyInjured)
            return;

        core::Mono::Get().SetFieldValue(player, m_field_criticallyInjured, value);
    }
}
