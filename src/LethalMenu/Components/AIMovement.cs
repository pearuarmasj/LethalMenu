using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Components
{
    /// <summary>
    /// AI Movement component for controlling enemy movement.
    /// </summary>
    public class AIMovement : MonoBehaviour
    {
        private const float WalkingSpeed = 0.5f;
        private const float JumpForce = 9.2f;
        private const float Gravity = 18.0f;

        public float CharacterSpeed { get; set; } = 5.0f;
        public float CharacterSprintSpeed { get; set; } = 2.8f;

        public bool IsMoving { get; private set; } = false;
        public bool IsSprinting { get; private set; } = false;

        private float _velocityY = 0.0f;
        private CharacterController? _controller;
        private bool _noClipMode = false;

        public void SetNoClipMode(bool enabled)
        {
            _noClipMode = enabled;
        }

        public void SetPosition(Vector3 newPosition)
        {
            if (_controller == null) return;

            _controller.enabled = false;
            transform.position = newPosition;
            _controller.enabled = true;
        }

        public void CalibrateCollision(EnemyAI enemy)
        {
            if (_controller == null) return;

            _controller.height = 1.0f;
            _controller.radius = 0.5f;
            _controller.center = new Vector3(0.0f, 0.5f, 0.0f);

            float maxStepOffset = 0.25f;
            _controller.stepOffset = Mathf.Min(_controller.stepOffset, maxStepOffset);

            // Ignore collision with enemy's own colliders
            foreach (var collider in enemy.GetComponentsInChildren<Collider>())
            {
                if (collider != _controller)
                {
                    Physics.IgnoreCollision(_controller, collider);
                }
            }
        }

        private void Awake()
        {
            _controller = gameObject.AddComponent<CharacterController>();
        }

        private void Update()
        {
            if (_controller == null || !_controller.enabled) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Get input
            Vector2 moveInput = new Vector2(
                keyboard.dKey.ReadValue() - keyboard.aKey.ReadValue(),
                keyboard.wKey.ReadValue() - keyboard.sKey.ReadValue()
            ).normalized;

            IsMoving = moveInput.magnitude > 0.0f;
            IsSprinting = keyboard.leftShiftKey.isPressed && IsMoving;

            float speedModifier = keyboard.leftCtrlKey.isPressed ? WalkingSpeed : 1.0f;

            // Calculate movement direction
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;

            // Apply speed
            float currentSpeed = IsSprinting ? CharacterSpeed * CharacterSprintSpeed : CharacterSpeed;
            moveDirection *= speedModifier * currentSpeed;

            if (_noClipMode)
            {
                // NoClip mode - just move in camera direction
                if (keyboard.spaceKey.isPressed)
                    moveDirection += Vector3.up * currentSpeed;
                if (keyboard.leftCtrlKey.isPressed)
                    moveDirection += Vector3.down * currentSpeed;

                transform.position += moveDirection * Time.deltaTime;
            }
            else
            {
                // Normal mode with gravity
                if (_controller.isGrounded)
                {
                    _velocityY = -0.5f;
                    if (keyboard.spaceKey.wasPressedThisFrame)
                    {
                        _velocityY = JumpForce;
                    }
                }
                else
                {
                    _velocityY -= Gravity * Time.deltaTime;
                }

                moveDirection.y = _velocityY;
                _controller.Move(moveDirection * Time.deltaTime);
            }
        }
    }
}
