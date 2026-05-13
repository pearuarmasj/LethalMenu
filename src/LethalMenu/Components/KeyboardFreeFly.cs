using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Components
{
    /// Free-fly camera mover. WASD/space/ctrl moves the attached transform;
    /// shift ramps up sprint multiplier. Used by PhantomCheat to detach the gameplay camera.
    public class KeyboardFreeFly : MonoBehaviour
    {
        public float BaseSpeed { get; set; } = 20f;
        public bool IsPaused { get; set; }

        private Vector3 _lastPosition;
        private float _sprintMultiplier = 1f;

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _sprintMultiplier = 1f;
        }

        public void Resync() => _lastPosition = transform.position;

        private void LateUpdate()
        {
            if (IsPaused) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector3 direction = new Vector3(
                kb.dKey.ReadValue() - kb.aKey.ReadValue(),
                kb.spaceKey.ReadValue() - kb.leftCtrlKey.ReadValue(),
                kb.wKey.ReadValue() - kb.sKey.ReadValue()
            );

            _sprintMultiplier = kb.leftShiftKey.IsPressed()
                ? Mathf.Min(_sprintMultiplier + 5f * Time.deltaTime, 5f)
                : 1f;

            Vector3 worldDir =
                transform.right * direction.x +
                transform.up * direction.y +
                transform.forward * direction.z;

            _lastPosition += worldDir * Time.deltaTime * BaseSpeed * _sprintMultiplier;
            transform.position = _lastPosition;
        }
    }
}
