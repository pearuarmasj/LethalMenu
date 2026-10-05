using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// Keeps the held TZP inhalant full.
    public class UnlimitedTZPCheat : CheatBase
    {
        public override string Name => "Unlimited TZP";
        public override Hack HackType => Hack.UnlimitedTZP;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            if (LethalMenuMod.LocalPlayer?.currentlyHeldObjectServer is TetraChemicalItem tzp)
                tzp.fuel = 1f;
        }
    }

    /// Suppresses the drunk/gas effects of inhaling TZP.
    public class NoTZPEffectsCheat : CheatBase
    {
        public override string Name => "No TZP Effects";
        public override Hack HackType => Hack.NoTZPEffects;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            var player = LethalMenuMod.LocalPlayer;
            if (player?.currentlyHeldObjectServer is not TetraChemicalItem tzp) return;

            player.drunknessInertia = 0f;
            player.increasingDrunknessThisFrame = false;
            tzp.emittingGas = false;
            HUDManager.Instance?.gasHelmetAnimator.SetBool("gasEmitting", false);
        }
    }

    /// Fires the held shotgun every frame while LMB is held.
    public class MinigunShotgunCheat : CheatBase
    {
        public override string Name => "Minigun Shotgun";
        public override Hack HackType => Hack.MinigunShotgun;

        public override void OnUpdate()
        {
            if (!IsEnabled || Settings.ShowMenu) return;
            var player = LethalMenuMod.LocalPlayer;
            if (player?.currentlyHeldObjectServer is not ShotgunItem shotgun) return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed) return;

            var pos = player.transform.position - player.gameplayCamera.transform.up * 0.45f;
            shotgun.ShootGunServerRpc(pos, player.gameplayCamera.transform.forward);
        }
    }
}
