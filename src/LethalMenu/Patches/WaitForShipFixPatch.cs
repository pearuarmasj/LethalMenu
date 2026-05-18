using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Vanilla deadlock: in large lobbies, end-of-round occasionally hangs on "Wait for ship
    /// to land" because the server's playersRevived counter never reaches connectedPlayers —
    /// at least one client's PlayerHasRevivedServerRpc packet dropped. The start lever stays
    /// non-interactable and the round can't advance.
    ///
    /// This Postfix wraps EndOfGame: after the original coroutine finishes and the ship phase
    /// settles, periodically re-send PlayerHasRevivedServerRpc until the lever becomes
    /// interactable again. A small randomized stagger prevents every patched client from
    /// firing the RPC on the same frame.
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
    internal static class WaitForShipFixPatch
    {
        [HarmonyPostfix]
        private static IEnumerator Postfix(IEnumerator endOfGame)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null) yield break;

            var startMatchLever = Object.FindObjectOfType<StartMatchLever>(includeInactive: true);
            if (startMatchLever == null) yield break;

            while (endOfGame.MoveNext()) yield return endOfGame.Current;

            yield return new WaitUntil(() => !startOfRound.shipIsLeaving);
            yield return new WaitForSeconds(5f);

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(2f, 5f));

                if (startOfRound.travellingToNewLevel) continue;

                if (startOfRound.inShipPhase && startMatchLever.triggerScript != null && !startMatchLever.triggerScript.interactable)
                {
                    startOfRound.PlayerHasRevivedServerRpc();
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}
