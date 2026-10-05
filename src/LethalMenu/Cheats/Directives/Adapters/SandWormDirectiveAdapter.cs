using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Earth Leviathan. Surface-only (CanUseEntrances = false): it never leaves the outside nav mesh, so a target
    /// inside the facility is simply not reachable. Vanilla chase = behaviour state 1 (rumbling pursuit via
    /// SetMovingTowardsTargetPlayer); within 4 m, 17 % per AI interval and not within 9 m of the ship,
    /// SandWormAI.DoAIInterval calls StartEmergeAnimation -> EmergeServerRpc(yRot) -> EmergeClientRpc, and the victim's
    /// own OnCollideWithPlayer -> EatPlayer kills while emerged, so it replicates normally. StartEmergeAnimation is
    /// host-only (IsServer) but we own the worm from any client, so Emerge below repeats its body and sends
    /// EmergeServerRpc, which only needs ownership. Update state 1 counts chaseTimer when the target is untargetable
    /// or beyond 22 m and falls back to state 0 at 6 s; AfterUpdate zeroes it. During the emerge animation
    /// (inEmergingState / emerged) the agent is off and Engage / Follow leave the worm alone.
    public sealed class SandWormDirectiveAdapter : DirectiveAdapter<SandWormAI>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;
        private const float EmergeRange = 4f;
        private const float ShipExclusion = 9f;
        private const int EmergeChancePercent = 17;
        private const float RoamSpeed = 4f;

        public override void Engage(SandWormAI enemy, PlayerControllerB target)
        {
            if (IsEmerging(enemy)) return;
            if (enemy.roamMap.inProgress) enemy.StopSearch(enemy.roamMap);
            if (enemy.currentBehaviourStateIndex != ChaseState) enemy.SwitchToBehaviourState(ChaseState);
            enemy.chaseTimer = 0f;
            enemy.SetMovingTowardsTargetPlayer(target);

            Vector3 position = enemy.transform.position;
            if (Vector3.Distance(position, target.transform.position) < EmergeRange &&
                Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(position), position) >= ShipExclusion &&
                Random.Range(0, 100) < EmergeChancePercent)
                Emerge(enemy);
        }

        public override void AfterUpdate(SandWormAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.chaseTimer = 0f;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(SandWormAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(SandWormAI enemy, Vector3 position)
        {
            if (IsEmerging(enemy)) return;
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            if (enemy.roamMap.inProgress) enemy.StopSearch(enemy.roamMap);
            enemy.agent.speed = RoamSpeed;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(SandWormAI enemy) => Disengage(enemy);

        public override bool CanUseEntrances(SandWormAI enemy) => false;

        private static bool IsEmerging(SandWormAI enemy) => enemy.inEmergingState || enemy.emerged;

        /// SandWormAI.StartEmergeAnimation without its IsServer gate: pick a flight direction whose arc ends in
        /// natural ground, record where the worm lands, then let the owner-only EmergeServerRpc replicate it.
        private static void Emerge(SandWormAI enemy)
        {
            float yRot = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(enemy.transform.position + Vector3.up * 1.5f, 30f);
            yRot += Random.Range(-45f, 45f);
            enemy.inEmergingState = true;
            enemy.agent.enabled = false;
            enemy.inSpecialAnimation = true;
            enemy.transform.eulerAngles = new Vector3(0f, yRot, 0f);

            bool landsOnGround = false;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                RaycastHit hitInfo;
                for (int j = 0; j < enemy.airPathNodes.Length - 1; j++)
                {
                    Vector3 direction = enemy.airPathNodes[j + 1].position - enemy.airPathNodes[j].position;
                    if (!Physics.SphereCast(enemy.airPathNodes[j].position, 5f, direction, out hitInfo, direction.magnitude, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                        continue;
                    landsOnGround = false;
                    if (Terrain.activeTerrain == null)
                    {
                        for (int k = 0; k < StartOfRound.Instance.naturalSurfaceTags.Length; k++)
                        {
                            if (hitInfo.collider.CompareTag(StartOfRound.Instance.naturalSurfaceTags[k]) ||
                                (StartOfRound.Instance.currentLevel.levelID == 12 && hitInfo.collider.CompareTag("Rock")))
                                landsOnGround = true;
                        }
                    }
                    else if (hitInfo.collider.gameObject == Terrain.activeTerrain.gameObject)
                    {
                        landsOnGround = true;
                    }
                    if (!landsOnGround) break;
                }

                if (!landsOnGround)
                {
                    yRot += 60f;
                    enemy.transform.eulerAngles = new Vector3(0f, yRot, 0f);
                }
                else if (Physics.Raycast(enemy.endingPosition.position + Vector3.up * 50f, Vector3.down, out hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    enemy.endOfFlightPathPosition = RoundManager.Instance.GetNavMeshPosition(hitInfo.point, enemy.navHit, 8f, enemy.agent.areaMask);
                    if (!RoundManager.Instance.GotNavMeshPositionResult)
                        enemy.endOfFlightPathPosition = RoundManager.Instance.GetClosestNode(hitInfo.point).position;
                    break;
                }
            }

            if (!landsOnGround)
            {
                enemy.inSpecialAnimation = false;
                enemy.agent.enabled = true;
                enemy.inEmergingState = false;
                return;
            }
            enemy.EmergeServerRpc((int)yRot);
        }
    }
}
