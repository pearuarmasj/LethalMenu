using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using LethalMenu.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// <summary>
    /// Enemy control system - allows player to possess and control enemies.
    /// </summary>
    public class EnemyControlCheat : CheatBase
    {
        public override string Name => "Enemy Control";

        // Currently controlled enemy
        private static EnemyAI? _controlledEnemy;
        private static GameObject? _controllerObject;
        private static MouseInput? _mouseInput;
        private static AIMovement? _movement;
        private static AudioListener? _audioListener;

        public static bool IsControlling => _controlledEnemy != null;
        public static bool IsAIControlled { get; private set; } = false;

        // Controller dictionary - maps enemy types to their attack methods (simplified for API compatibility)
        private static readonly Dictionary<Type, Action<EnemyAI>> PrimaryAttacks = new()
        {
            { typeof(FlowermanAI), e => { e.SwitchToBehaviourState(2); } }, // Chase mode
            { typeof(NutcrackerEnemyAI), e => { var n = (NutcrackerEnemyAI)e; if (n.gun != null) n.FireGunServerRpc(); } },
            { typeof(MouthDogAI), e => { e.SwitchToBehaviourState(2); } }, // Chase mode
            { typeof(SpringManAI), e => { e.SwitchToBehaviourState(1); } }, // Active mode
            { typeof(BlobAI), e => { e.SwitchToBehaviourState(1); } },
            { typeof(CentipedeAI), e => { e.SwitchToBehaviourState(2); } }, // Attack mode
            { typeof(CrawlerAI), e => { e.SwitchToBehaviourState(2); } },
            { typeof(SandSpiderAI), e => { e.SwitchToBehaviourState(2); } },
            { typeof(ForestGiantAI), e => { e.SwitchToBehaviourState(1); } }, // Chase mode
            { typeof(JesterAI), e => { e.SwitchToBehaviourState(2); } }, // Rampage
            { typeof(BaboonBirdAI), e => { e.SwitchToBehaviourState(2); } },
        };

        public new bool IsEnabled
        {
            get => Settings.EnemyControl;
            set => Settings.EnemyControl = value;
        }

        public override void OnUpdate()
        {
            // Handle right-click targeting when not controlling
            if (IsEnabled && !IsControlling)
            {
                TryTargetEnemy();
                return;
            }

            if (!IsEnabled || _controlledEnemy == null)
            {
                if (IsControlling) StopControl();
                return;
            }

            // Check if enemy died
            if (_controlledEnemy.isEnemyDead)
            {
                StopControl();
                return;
            }

            // Keep ownership
            if (_controlledEnemy.IsSpawned && LethalMenuMod.LocalPlayer != null)
            {
                _controlledEnemy.ChangeEnemyOwnerServerRpc(LethalMenuMod.LocalPlayer.actualClientId);
            }

            // Update enemy position and rotation
            if (!IsAIControlled && _movement != null && _mouseInput != null)
            {
                var euler = _controlledEnemy.transform.eulerAngles;
                euler.y = _mouseInput.transform.eulerAngles.y;
                _controlledEnemy.transform.eulerAngles = euler;
                _controlledEnemy.transform.position = _movement.transform.position;
            }

            // Handle input
            HandleInput();

            // Move camera to follow enemy
            UpdateCamera();
        }

        private static void TryTargetEnemy()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!mouse.rightButton.wasPressedThisFrame) return;
            if (Settings.ShowMenu) return; // Don't target while menu open

            var player = LethalMenuMod.LocalPlayer;
            if (player == null || player.gameplayCamera == null) return;

            // Raycast from camera center
            var ray = player.gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                // Check if we hit an enemy
                var enemy = hit.collider.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    TakeControl(enemy);
                    return;
                }
            }

            // Also check all enemies for close proximity (backup method)
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAI>())
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                
                float dist = Vector3.Distance(player.transform.position, enemy.transform.position);
                if (dist < 5f)
                {
                    // Check if we're looking at it
                    var toEnemy = (enemy.transform.position - player.gameplayCamera.transform.position).normalized;
                    var forward = player.gameplayCamera.transform.forward;
                    if (Vector3.Dot(forward, toEnemy) > 0.85f) // ~30 degree cone
                    {
                        TakeControl(enemy);
                        return;
                    }
                }
            }
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            // Left click - primary attack
            if (mouse.leftButton.wasPressedThisFrame)
            {
                UsePrimarySkill();
            }

            // F9 - Toggle AI control
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                ToggleAIControl();
            }

            // F10 - Toggle NoClip
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                _movement?.SetNoClipMode(true);
            }

            // F11 - Release control
            if (keyboard.f11Key.wasPressedThisFrame)
            {
                StopControl();
                HUDManager.Instance?.DisplayTip("Enemy Control", "Released control");
            }

            // F12 - Kill enemy and release
            if (keyboard.f12Key.wasPressedThisFrame)
            {
                if (_controlledEnemy != null)
                {
                    // Try to kill properly with death animation
                    _controlledEnemy.KillEnemyOnOwnerClient(false); // false = show death anim
                    HUDManager.Instance?.DisplayTip("Enemy Control", "Enemy killed");
                }
                StopControl();
            }
        }

        private void UpdateCamera()
        {
            if (_controlledEnemy == null) return;

            // Position camera behind and above enemy
            var camera = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (camera == null) return;

            var pos = _controlledEnemy.transform.position + Vector3.up * 3f - _controlledEnemy.transform.forward * 3f;
            camera.transform.position = pos;
            camera.transform.LookAt(_controlledEnemy.transform.position + Vector3.up);
        }

        private void UsePrimarySkill()
        {
            if (_controlledEnemy == null || IsAIControlled) return;

            var enemyType = _controlledEnemy.GetType();
            if (PrimaryAttacks.TryGetValue(enemyType, out var attack))
            {
                try
                {
                    attack(_controlledEnemy);
                }
                catch (Exception ex)
                {
                    Loader.LogError($"[EnemyControl] Attack failed: {ex.Message}");
                }
            }
        }

        private void ToggleAIControl()
        {
            if (_controlledEnemy?.agent == null || _movement == null) return;

            IsAIControlled = !IsAIControlled;

            if (IsAIControlled)
            {
                _controlledEnemy.agent.Warp(_controlledEnemy.transform.position);
                _controlledEnemy.SyncPositionToClients();
            }

            _controlledEnemy.agent.updatePosition = IsAIControlled;
            _controlledEnemy.agent.updateRotation = IsAIControlled;
            _controlledEnemy.agent.isStopped = !IsAIControlled;
            _movement.SetPosition(_controlledEnemy.transform.position);
            _movement.enabled = !IsAIControlled;

            HUDManager.Instance?.DisplayTip("Enemy Control", IsAIControlled ? "AI Control: ON" : "AI Control: OFF");
        }

        /// <summary>
        /// Take control of an enemy.
        /// </summary>
        public static void TakeControl(EnemyAI enemy)
        {
            if (enemy == null || enemy.isEnemyDead)
            {
                HUDManager.Instance?.DisplayTip("Enemy Control", "Cannot control dead enemy");
                return;
            }

            // Stop previous control
            if (IsControlling) StopControl();

            _controlledEnemy = enemy;

            // Take ownership
            if (enemy.IsSpawned && LethalMenuMod.LocalPlayer != null)
            {
                enemy.ChangeEnemyOwnerServerRpc(LethalMenuMod.LocalPlayer.actualClientId);
            }

            // Create controller object
            _controllerObject = new GameObject("EnemyController");
            _controllerObject.transform.position = enemy.transform.position;
            _controllerObject.transform.rotation = enemy.transform.rotation;

            _mouseInput = _controllerObject.AddComponent<MouseInput>();
            _movement = _controllerObject.AddComponent<AIMovement>();
            _audioListener = _controllerObject.AddComponent<AudioListener>();

            _movement.CalibrateCollision(enemy);
            _movement.CharacterSprintSpeed = 2.8f;
            _movement.SetPosition(enemy.transform.position);

            // Disable AI
            if (enemy.agent != null)
            {
                enemy.agent.updatePosition = false;
                enemy.agent.updateRotation = false;
                enemy.agent.isStopped = true;
            }

            IsAIControlled = false;
            Settings.EnemyControl = true;

            // Switch audio listener
            if (LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.activeAudioListener.enabled = false;
            }

            // Store original camera position for restoration
            _originalCameraParent = LethalMenuMod.LocalPlayer?.gameplayCamera?.transform.parent;

            HUDManager.Instance?.DisplayTip("Enemy Control", 
                $"Controlling {enemy.enemyType?.enemyName ?? "enemy"}\n" +
                "F9=Toggle AI | F10=NoClip | F11=Release | F12=Kill");
        }

        private static Transform? _originalCameraParent;

        /// <summary>
        /// Stop controlling the current enemy.
        /// </summary>
        public static void StopControl()
        {
            if (_controlledEnemy != null)
            {
                // Re-enable AI
                if (_controlledEnemy.agent != null && _controlledEnemy.agent.isOnNavMesh)
                {
                    _controlledEnemy.agent.updatePosition = true;
                    _controlledEnemy.agent.updateRotation = true;
                    _controlledEnemy.agent.isStopped = false;
                    _controlledEnemy.agent.Warp(_controlledEnemy.transform.position);
                }
            }

            // Restore audio listener
            if (LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.activeAudioListener.enabled = true;
                
                // Reset camera to player
                var camera = LethalMenuMod.LocalPlayer.gameplayCamera;
                if (camera != null && _originalCameraParent != null)
                {
                    camera.transform.SetParent(_originalCameraParent);
                    camera.transform.localPosition = Vector3.zero;
                    camera.transform.localRotation = Quaternion.identity;
                }
            }

            // Cleanup
            if (_controllerObject != null)
            {
                UnityEngine.Object.Destroy(_controllerObject);
            }

            _controlledEnemy = null;
            _controllerObject = null;
            _mouseInput = null;
            _movement = null;
            _audioListener = null;
            _originalCameraParent = null;
            IsAIControlled = false;
            Settings.EnemyControl = false;
        }

        /// <summary>
        /// Get the currently controlled enemy.
        /// </summary>
        public static EnemyAI? GetControlledEnemy() => _controlledEnemy;
    }
}
