using LethalMenu.Util;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for CaveDwellerAI (Maneater).
    /// Primary: Transform To Adult (one-shot, then no skills).
    /// </summary>
    public class CaveDwellerController : IEnemyController<CaveDwellerAI>
    {
        public void UsePrimarySkill(CaveDwellerAI enemy)
        {
            if (!enemy.adultContainer.activeSelf) TransformIntoAdult(enemy);
        }

        public string? GetPrimarySkillName(CaveDwellerAI enemy)
            => !enemy.adultContainer.activeSelf ? "Transform To Adult" : "";

        public string? GetSecondarySkillName(CaveDwellerAI _) => "";

        public bool CanUseEntranceDoors(CaveDwellerAI _) => true;
        public float InteractRange(CaveDwellerAI _) => 5f;

        private static void TransformIntoAdult(CaveDwellerAI enemy)
        {
            enemy.SwitchToBehaviourStateOnLocalClient(1);
            enemy.TurnIntoAdultServerRpc();
            enemy.Reflect().Invoke("StartTransformationAnim");
        }
    }
}
