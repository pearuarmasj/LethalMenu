using System.Linq;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// TriggerGun — middle-mouse fires a sphere-cast from the camera and activates whatever it hits.
    public class TriggerGunCheat : CheatBase
    {
        public override string Name => "Trigger Gun";
        public override Hack HackType => Hack.TriggerGun;

        private const float CastRadius = 0.5f;
        private const float CastDistance = 100f;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            if (Settings.ShowMenu) return; // Don't fire while menu open.
            var mouse = Mouse.current;
            if (mouse == null || !mouse.middleButton.wasPressedThisFrame) return;

            var cam = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (cam == null) return;

            var kb = Keyboard.current;
            bool ePressed = kb != null && kb.eKey.isPressed;

            var hits = Physics.SphereCastAll(cam.transform.position, CastRadius, cam.transform.forward, CastDistance);
            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                if (Dispatch(hit, ePressed)) return;
            }
        }

        private bool Dispatch(RaycastHit hit, bool ePressed)
        {
            var col = hit.collider;
            if (col == null) return false;

            if (col.TryGetComponent(out Landmine mine))
            {
                mine.ExplodeMineServerRpc();
                return true;
            }
            if (col.TryGetComponent(out Turret turret))
            {
                turret.EnterBerserkModeServerRpc(-1);
                return true;
            }
            if (col.TryGetComponent(out JetpackItem jetpack))
            {
                jetpack.ExplodeJetpackServerRpc();
                return true;
            }
            if (col.TryGetComponent(out DoorLock door))
            {
                door.UnlockDoorSyncWithServer();
                return true;
            }
            if (col.TryGetComponent(out TerminalAccessibleObject term))
            {
                term.SetDoorOpenServerRpc(!term.isDoorOpen);
                return true;
            }
            if (col.TryGetComponent(out DepositItemsDesk desk))
            {
                desk.AttackPlayersServerRpc();
                return true;
            }
            var enemy = col.GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.isEnemyDead)
            {
                if (ePressed)
                {
                    EnemyControlCheat.TakeControl(enemy);
                }
                else
                {
                    NetworkCheats.KillEnemy(enemy);
                }
                return true;
            }
            if (col.TryGetComponent(out PlayerControllerB target))
            {
                NetworkCheats.LureAllEnemies(target.transform.position);
                return true;
            }
            return false;
        }
    }
}
