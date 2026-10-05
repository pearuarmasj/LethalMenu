namespace LethalMenu
{
    public enum Hack
    {
        // === Self ===
        GodMode, InfiniteStamina, SpeedHack, JumpHack, NoClip, NightVision,
        NoFallDamage, NoWeight, UnlimitedOxygen, AntiFlash, NoQuicksand,
        UnlimitedJump, FastClimb, TauntSlide, ExtraItemSlots, TeleportWithItems,
        BHop, SaneMode, LookDown, NoCooldown,

        // === Enemy ===
        Untargetable, AntiGhostGirl, EnemyControl, KillClick, StunClick,

        // === Items ===
        InfiniteBattery, OneHanded, Reach, LootThroughWalls, InteractThroughWalls,
        LootBeforeGameStarts, GrabNutcrackerShotgun, SuperShovel, SuperKnife, UnlimitedAmmo,
        MinigunShotgun, UnlimitedZapGun, UnlimitedTZP, NoTZPEffects, UnlimitedPresents,
        EggsAlwaysExplode, EggsNeverExplode,
        InfiniteScanRange, InfiniteGrab, InfiniteDeposit,
        LootAnyItemBeltBag, LootThroughWallsBeltBag,

        // === Visuals ===
        EnableESP, PlayerESP, EnemyESP, ItemESP, DoorESP, MineESP, TurretESP, FuseboxESP,
        PlayerHealthBars, FreeCam, ThirdPerson, SpectatePlayer, AlwaysShowClock, Crosshair,
        HPDisplay, InfoDisplay, InfoDisplayCredits, InfoDisplayQuota, InfoDisplayDeadline,
        InfoDisplayEnemies, InfoDisplayBodies, InfoDisplayMapLoot, InfoDisplayShipLoot,
        InfoDisplayMoon, InfoDisplayTime, NoVisor, NoCameraShake, NoDepthOfField,
        FullRenderResolution, CustomFOV, Breadcrumbs, NoFog, VisibleBody,
        MinimalGUI, RadarPatch, EnemyDeathNotification,
        SteamValveESP, BigDoorESP, ShipDoorESP, EnemyVentESP, ItemDropshipESP,
        CruiserESP, MoldSporeESP, MineshaftElevatorESP, SpikeRoofTrapESP,

        // === Chams ===
        EnableChams,
        PlayerChams, EnemyChams, ItemChams, LandmineChams, TurretChams,
        DoorChams, BigDoorChams, ShipDoorChams, BreakerChams, EnemyVentChams,
        ItemDropshipChams, CruiserChams, MoldSporeChams, MineshaftElevatorChams,
        EntranceChams, SpikeRoofTrapChams, SteamValveChams,

        // === World ===
        BridgeNeverFalls, AutoOpenDropship, ShipDoorInSpace, NoShipDoorClose, Shoplifter,
        AntiJeb, BuildAnywhere, InstantInteract,
        VehicleGodMode, TriggerGun,

        // === Network ===
        AntiKick, ShowKickedLobbies, HearEveryone, Invisibility, DeathNotifications, HearDeadPeople,
        FollowPlayer, JoinSpam, ShowOffensiveLobbyNames,

        // === Spam/Troll ===
        HornSpam, DoorSpam, SignalSpam, RPCLagSpam, EarrapeSpam,
        ChatSpam, CarHornSpam, DeskDoorSpam,

        // === Actions (one-shot, not toggles) ===
        SelfRevive, FakeDeath, CancelFakeDeath, TeleportToShip, TeleportToEntrance,
        TeleportToFireExit, KillAllEnemies, StunAllEnemies, TeleportAllEnemiesAway,
        TPAllItemsToShip, TPNearbyItems, UnlockAllDoors, BlowUpAllMines, ToggleMines,
        ToggleTurrets, BerserkTurrets, FlickerLights, MaxChaos, ForceShipLeave,
        EjectAllPlayers, ForceStart, ForceEnd, ReviveAllPlayers, TeleportAllToMe,
        SetCredits, SellQuota,
        Disconnect, ReconnectFromClipboard,
        RecruitEscort, ReleaseAllDirected,
    }
}
