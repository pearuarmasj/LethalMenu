using UnityEngine;

namespace LethalMenu.Cheats
{
    /// BHop — continuous jumping with air-strafe momentum while space held.
    /// Ports LethalMenu-master's BHop with jump-timer + air-velocity accumulation.
    public class BHopCheat : CheatBase
    {
        public override string Name => "BHop";
        public override Hack HackType => Hack.BHop;

        private bool _isInAir;
        private Vector3 _airVelocity;
        private Vector3 _lastForwardDirection;
        private float _jumpTimer;

        public override void OnUpdate()
        {
            var p = LethalMenuMod.LocalPlayer;
            if (p == null) return;

            if (!IsEnabled)
            {
                _isInAir = false;
                _jumpTimer = 0f;
                _airVelocity = Vector3.Lerp(_airVelocity, Vector3.zero, Time.deltaTime * 4.2f);
                _lastForwardDirection = p.transform.forward;
                return;
            }

            if (p.playerBodyAnimator != null && p.playerBodyAnimator.GetBool("Jumping") && _jumpTimer < 0.1f)
            {
                p.fallValue = p.jumpForce;
                _jumpTimer += Time.deltaTime * 10f;
            }

            if (p.thisController != null && !p.thisController.isGrounded && !p.isClimbingLadder)
            {
                if (!_isInAir)
                {
                    _isInAir = true;
                    Vector3 v = p.thisController.velocity;
                    v.y = 0f;
                    _airVelocity += v * 0.006f;
                }
                _airVelocity.y = 0f;
                p.thisController.Move(p.transform.forward * _airVelocity.magnitude);

                Vector3 forward = p.transform.forward;
                if ((forward - _lastForwardDirection).magnitude > 0.01f)
                    _airVelocity += Vector3.one * 0.0005f;
                _lastForwardDirection = forward;
            }
            else
            {
                _airVelocity = Vector3.Lerp(_airVelocity, Vector3.zero, Time.deltaTime * 4.2f);
                _isInAir = false;
                _jumpTimer = 0f;
            }
        }
    }
}
