#include "pch.h"
#include "game.h"

namespace sdk
{
    GameState& GameState::Get()
    {
        static GameState instance;
        return instance;
    }

    void GameState::Update()
    {
        // TODO: Update cached pointers from Unity runtime
        // This will use IL2CPP API calls to find game objects
    }

    bool GameState::IsInGame() const
    {
        return startOfRound != nullptr && localPlayer != nullptr;
    }
}
