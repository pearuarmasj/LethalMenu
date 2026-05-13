using LethalMenu.Util;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for BlobAI (Hygrodere/Slime).
    /// Primary: Anger (18s aggressive timer).
    /// Secondary HOLD: Tame (resets anger, pauses pursuit).
    /// Secondary RELEASE: Resume normal behaviour.
    /// </summary>
    public class BlobController : IEnemyController<BlobAI>
    {
        public void UsePrimarySkill(BlobAI enemy)
        {
            SetAngeredTimer(enemy, 18.0f);
        }

        public void OnSecondarySkillHold(BlobAI enemy)
        {
            SetAngeredTimer(enemy, 0.0f);
            SetTamedTimer(enemy, 2.0f);
        }

        public void ReleaseSecondarySkill(BlobAI enemy)
        {
            SetTamedTimer(enemy, 0.0f);
        }

        public string? GetPrimarySkillName(BlobAI _) => "Anger";
        public string? GetSecondarySkillName(BlobAI _) => "(HOLD) Tame";

        public float InteractRange(BlobAI _) => 3.5f;
        public float SprintMultiplier(BlobAI _) => 9.8f;
        public bool CanUseEntranceDoors(BlobAI _) => false;

        private static void SetTamedTimer(BlobAI enemy, float time)
            => enemy.Reflect().SetField("tamedTimer", time);

        private static void SetAngeredTimer(BlobAI enemy, float time)
            => enemy.Reflect().SetField("angeredTimer", time);
    }
}
