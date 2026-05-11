namespace LethalMenu.Mixins
{
    public interface IHazardController { }

    public static class HazardControllerMixin
    {
        public static void DetonateMine(this IHazardController _, Landmine mine)
        {
            mine?.TriggerMineOnLocalClientByExiting();
        }

        public static void DetonateAllMines(this IHazardController _)
        {
            foreach (var mine in LethalMenuMod.Landmines)
                _.DetonateMine(mine);
        }

        public static void ToggleAllTurrets(this IHazardController _)
        {
            foreach (var turret in LethalMenuMod.Turrets)
            {
                var termObj = turret?.GetComponent<TerminalAccessibleObject>();
                termObj?.CallFunctionFromTerminal();
            }
        }

        public static void BerserkAllTurrets(this IHazardController _)
        {
            foreach (var turret in LethalMenuMod.Turrets)
                turret?.SwitchTurretMode(2);
        }
    }
}
