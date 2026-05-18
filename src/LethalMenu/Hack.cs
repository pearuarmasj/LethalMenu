namespace LethalMenu
{
    public enum Hack
    {
        // === Self ===
        GodMode, DemiGod, InfiniteStamina, SpeedHack, JumpHack, NoClip, NightVision,
        NoFallDamage, NoWeight, UnlimitedOxygen, AntiFlash, NoQuicksand, SuperSpeed,
        SuperJump, UnlimitedJump, FastClimb, TauntSlide, ExtraItemSlots, TeleportWithItems,
        BHop, SaneMod, LookDown, NoCooldown, Phantom,

        // === Enemy ===
        Untargetable, GhostMode, AntiGhostGirl, EnemyControl, KillClick, StunClick,

        // === Items ===
        InfiniteBattery, OneHanded, StrongHands, Reach, LootThroughWalls, InteractThroughWalls,
        LootBeforeGameStarts, GrabNutcrackerShotgun, SuperShovel, SuperKnife, UnlimitedAmmo,
        MinigunShotgun, UnlimitedZapGun, UnlimitedTZP, NoTZPEffects, UnlimitedPresents,
        EggsAlwaysExplode, EggsNeverExplode,
        InfiniteScanRange, InfiniteGrab, InfiniteItemUsage, InfiniteDeposit,
        LootAnyItemBeltBag, LootThroughWallsBeltBag,

        // === Visuals ===
        EnableESP, PlayerESP, EnemyESP, ItemESP, DoorESP, MineESP, TurretESP, FuseboxESP,
        PlayerHealthBars, FreeCam, ThirdPerson, SpectatePlayer, AlwaysShowClock, Crosshair,
        HPDisplay, InfoDisplay, InfoDisplayCredits, InfoDisplayQuota, InfoDisplayDeadline,
        InfoDisplayEnemies, InfoDisplayBodies, InfoDisplayMapLoot, InfoDisplayShipLoot,
        InfoDisplayMoon, InfoDisplayTime, NoVisor, NoCameraShake, NoDepthOfField,
        FullRenderResolution, CustomFOV, Breadcrumbs, NoFog, VisibleBody,
        MinimalGUIMod, ClearVisionMod, RadarPatch, EnemyDeathNotification,
        SteamValveESP, BigDoorESP, ShipDoorESP, EnemyVentESP, ItemDropshipESP,
        CruiserESP, MoldSporeESP, MineshaftElevatorESP, EntranceESP, SpikeRoofTrapESP,

        // === Chams ===
        EnableChams,
        PlayerChams, EnemyChams, ItemChams, LandmineChams, TurretChams,
        DoorChams, BigDoorChams, ShipDoorChams, BreakerChams, EnemyVentChams,
        ItemDropshipChams, CruiserChams, MoldSporeChams, MineshaftElevatorChams,
        EntranceChams, SpikeRoofTrapChams, SteamValveChams,

        // === World ===
        BridgeNeverFalls, AutoOpenDropship, ShipDoorInSpace, NoShipDoorClose, Shoplifter,
        GrabInLobby, AntiJeb, BuildAnywhere, InstantInteract,
        VehicleGodMode, TriggerGun,

        // === Network ===
        AntiKick, ShowKickedLobbies, HearEveryone, Invisibility, DeathNotifications, HearDeadPeople,
        FollowPlayer, PJSpammer, ShowOffensiveLobbyNames,

        // === Spam/Troll ===
        HornSpam, DoorSpam, SignalSpam, RPCLagSpam, TerminalSoundSpam, EarrapeSpam,
        ChatSpam, CarHornSpam, DeskDoorSpam,

        // === Actions (one-shot, not toggles) ===
        SelfRevive, FakeDeath, CancelFakeDeath, TeleportToShip, TeleportToEntrance,
        TeleportToFireExit, KillAllEnemies, StunAllEnemies, TeleportAllEnemiesAway,
        TPAllItemsToShip, TPNearbyItems, UnlockAllDoors, BlowUpAllMines, ToggleMines,
        ToggleTurrets, BerserkTurrets, FlickerLights, MaxChaos, ForceShipLeave,
        EjectAllPlayers, ForceStart, ForceEnd, ReviveAllPlayers, TeleportAllToMe,
        SetCredits, SellQuota,
        DisconnectMod, ReconnectFromClipboard,
    }
}
