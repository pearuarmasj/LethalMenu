using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Components
{
    /// 
    /// Mouse input component for controlling rotation.
    /// 
    public class MouseInput : MonoBehaviour
    {
        private float _yaw = 0f;
        private float _pitch = 0f;
        
        public float Sensitivity { get; set; } = 0.2f;

        private void Update()
        {
            if (Cursor.visible) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            _yaw += mouse.delta.x.ReadValue() * Sensitivity;
            _yaw = (_yaw + 360) % 360;

            _pitch -= mouse.delta.y.ReadValue() * Sensitivity;
            _pitch = Mathf.Clamp(_pitch, -90, 90);

            transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
        }
    }
}
