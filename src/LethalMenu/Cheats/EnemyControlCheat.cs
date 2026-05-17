using System;
using System.Linq;
using GameNetcodeStuff;
using LethalMenu.Cheats.EnemyControl;
using LethalMenu.Components;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// 
    /// Enemy control system - allows player to possess and control enemies.
    /// Now uses per-enemy-type controllers with unique abilities.
    /// 
    /// Controls:
    /// - RMB (while not possessing): Target and possess enemy
    /// - WASD: Move
    /// - Shift: Sprint
    /// - Space: Jump (or fly up in NoClip)
    /// - Ctrl: Walk slow (or fly down in NoClip)
    /// - LMB: Primary skill
    /// - RMB: Secondary skill
    /// - E: Interact with door / entrance teleport
    /// - N: Toggle NoClip
    /// - F9: Toggle AI control (let enemy AI take over)
    /// - F11/Z: Release control
    /// - Del/F12: Kill enemy and release
    /// 
    public class EnemyControlCheat : CheatBase
    {
        public override string Name => "Enemy Control";
        public override Hack HackType => Hack.EnemyControl;

        private static EnemyAI? _controlledEnemy;
        private static IEnemyController? _controller;
        private static GameObject? _controllerObject;
        private static MouseInput? _mouseInput;
        private static AIMovement? _movement;
        private static AudioListener? _audioListener;
        private static Transform? _originalCameraParent;

        public static bool IsControlling => _controlledEnemy != null;
        public static bool IsAIControlled { get; private set; } = false;
        public static bool NoClipEnabled { get; private set; } = false;
        private static bool _secondarySkillHeld = false;

        private const float DoorInteractionCooldown = 0.7f;
        private const float TeleportDoorCooldown = 2.5f;
        private static float _doorCooldownRemaining = 0f;
        private static float _teleportCooldownRemaining = 0f;

        private static EntranceTeleport? _mainEntrance;
        private static bool _firstUpdateAfterPossess = true;

        public override void OnUpdate()
        {
            // Update cooldowns
            if (_doorCooldownRemaining > 0) _doorCooldownRemaining -= Time.deltaTime;
            if (_teleportCooldownRemaining > 0) _teleportCooldownRemaining -= Time.deltaTime;

            // Handle right-click targeting when not controlling
            if (IsEnabled && !IsControlling)
            {
                TryTargetEnemy();
                return;
            }

            if (!IsEnabled || _controlledEnemy == null || _controller == null)
            {
                if (IsControlling) StopControl();
                return;
            }

            // Check if enemy died
            if (_controlledEnemy.isEnemyDead)
            {
                _controller.OnDeath(_controlledEnemy);
                StopControl();
                return;
            }

            // First update after possession - cache main entrance
            if (_firstUpdateAfterPossess)
            {
                _firstUpdateAfterPossess = false;
                _mainEntrance = RoundManager.FindMainEntranceScript(true);
            }

            // Keep ownership
            if (_controlledEnemy.IsSpawned && LethalMenuMod.LocalPlayer != null)
            {
                _controlledEnemy.ChangeEnemyOwnerServerRpc(LethalMenuMod.LocalPlayer.actualClientId);
            }

            // Update outside status based on Y position relative to main entrance
            UpdateOutsideStatus();

            // Update controller
            _controller.Update(_controlledEnemy, IsAIControlled);

            // Handle input
            HandleInput();

            // Update enemy position and rotation if we're manually controlling
            if (!IsAIControlled && _movement != null && _mouseInput != null)
            {
                if (_controller.IsAbleToRotate(_controlledEnemy))
                {
                    var euler = _controlledEnemy.transform.eulerAngles;
                    euler.y = _mouseInput.transform.eulerAngles.y;
                    _controlledEnemy.transform.eulerAngles = euler;
                }

                if (_controller.IsAbleToMove(_controlledEnemy))
                {
                    _controlledEnemy.transform.position = _movement.transform.position;
                }

                // Sync movement to controller
                _controller.OnMovement(_controlledEnemy, _movement.IsMoving, _movement.IsSprinting);

                // Auto-interact with doors when walking into them
                AutoInteractWithDoors();
            }

            // Move camera to follow enemy
            UpdateCamera();

            // Update cursor tip
            UpdateCursorTip();
        }

        /// 
        /// Update the enemy's outside status based on Y position relative to main entrance.
        /// This is important for proper enemy behavior and spawning.
        /// 
        private void UpdateOutsideStatus()
        {
            if (_controlledEnemy == null || _mainEntrance == null) return;

            bool isOutside = _controlledEnemy.transform.position.y > _mainEntrance.transform.position.y + 5.0f;
            _controlledEnemy.SetOutsideStatus(isOutside);
        }

        /// 
        /// Automatically interact with doors when walking into them.
        /// Uses raycast forward from enemy position.
        /// 
        private void AutoInteractWithDoors()
        {
            if (_controlledEnemy == null || _controller == null) return;
            if (_doorCooldownRemaining > 0) return;

            float interactRange = _controller.InteractRange(_controlledEnemy);

            // Raycast forward to check for doors
            if (!Physics.Raycast(_controlledEnemy.transform.position + Vector3.up * 0.5f, 
                _controlledEnemy.transform.forward, out var hit, interactRange)) return;

            // Check for regular doors
            if (hit.collider.gameObject.TryGetComponent(out DoorLock doorLock))
            {
                OpenDoorAsEnemy(doorLock);
                _doorCooldownRemaining = DoorInteractionCooldown;
                return;
            }

            // Check for entrance doors
            if (_controller.CanUseEntranceDoors(_controlledEnemy) && _teleportCooldownRemaining <= 0)
            {
                if (hit.collider.gameObject.TryGetComponent(out EntranceTeleport entrance))
                {
                    var exitPoint = GetExitPointFromDoor(entrance);
                    if (exitPoint != null)
                    {
                        TeleportEnemyToPosition(exitPoint.position);
                        _controlledEnemy.EnableEnemyMesh(true, false);
                    }
                    _teleportCooldownRemaining = TeleportDoorCooldown;
                }
            }
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
                var enemy = hit.collider.GetComponentInParent<EnemyAI>();
                if (enemy != null && !enemy.isEnemyDead)
                {
                    TakeControl(enemy);
                    return;
                }
            }

            // Backup: check nearby enemies we're looking at
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAI>())
            {
                if (enemy == null || enemy.isEnemyDead) continue;

                float dist = Vector3.Distance(player.transform.position, enemy.transform.position);
                if (dist < 5f)
                {
                    var toEnemy = (enemy.transform.position - player.gameplayCamera.transform.position).normalized;
                    var forward = player.gameplayCamera.transform.forward;
                    if (Vector3.Dot(forward, toEnemy) > 0.85f)
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
            if (keyboard == null || mouse == null || _controlledEnemy == null || _controller == null) return;

            // Left click - primary skill
            if (mouse.leftButton.wasPressedThisFrame && !IsAIControlled)
            {
                _controller.UsePrimarySkill(_controlledEnemy);
            }

            // Right click - secondary skill
            if (mouse.rightButton.wasPressedThisFrame && !IsAIControlled)
            {
                _controller.UseSecondarySkill(_controlledEnemy);
                _secondarySkillHeld = true;
            }
            if (mouse.rightButton.isPressed && _secondarySkillHeld && !IsAIControlled)
            {
                _controller.OnSecondarySkillHold(_controlledEnemy);
            }
            if (mouse.rightButton.wasReleasedThisFrame && _secondarySkillHeld)
            {
                _controller.ReleaseSecondarySkill(_controlledEnemy);
                _secondarySkillHeld = false;
            }

            // E - Interact with door (manual)
            if (keyboard.eKey.wasPressedThisFrame && _doorCooldownRemaining <= 0)
            {
                TryInteractWithDoor();
            }

            // N - Toggle NoClip (more intuitive than F10)
            if (keyboard.nKey.wasPressedThisFrame)
            {
                ToggleNoClip();
            }

            // F9 - Toggle AI control
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                ToggleAIControl();
            }

            // F10 - Toggle NoClip (alternate)
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                ToggleNoClip();
            }

            // Z or F11 - Release control
            if (keyboard.zKey.wasPressedThisFrame || keyboard.f11Key.wasPressedThisFrame)
            {
                StopControl();
                HUDManager.Instance?.DisplayTip("Enemy Control", "Released control");
            }

            // Del or F12 - Kill enemy and release
            if (keyboard.deleteKey.wasPressedThisFrame || keyboard.f12Key.wasPressedThisFrame)
            {
                KillAndDespawnEnemy();
            }
        }

        /// 
        /// Toggle NoClip mode for the possessed enemy.
        /// 
        private void ToggleNoClip()
        {
            if (_movement == null) return;

            NoClipEnabled = !NoClipEnabled;
            _movement.SetNoClipMode(NoClipEnabled);

            // If turning off NoClip while AI is controlled, that's a conflict
            if (!NoClipEnabled && IsAIControlled)
            {
                // NoClip off is fine with AI
            }

            HUDManager.Instance?.DisplayTip("Enemy Control", NoClipEnabled ? "NoClip: ON (Space=Up, Ctrl=Down)" : "NoClip: OFF");
        }

        /// 
        /// Kill the controlled enemy and despawn it (if host).
        /// 
        private void KillAndDespawnEnemy()
        {
            if (_controlledEnemy == null) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // Kill the enemy
            _controlledEnemy.KillEnemyOnOwnerClient(false);

            // If we're the host, despawn the network object
            if (localPlayer.IsHost || localPlayer.IsServer)
            {
                if (_controlledEnemy.TryGetComponent(out NetworkObject networkObject))
                {
                    try
                    {
                        networkObject.Despawn(true);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[EnemyControl] Failed to despawn: {ex.Message}");
                    }
                }
            }

            HUDManager.Instance?.DisplayTip("Enemy Control", "Enemy killed" + (localPlayer.IsHost ? " and despawned" : ""));
            StopControl();
        }

        private void TryInteractWithDoor()
        {
            if (_controlledEnemy == null || _controller == null) return;

            float interactRange = _controller.InteractRange(_controlledEnemy);
            var position = _controlledEnemy.transform.position;

            // Check for entrance doors (teleport between inside/outside)
            if (_controller.CanUseEntranceDoors(_controlledEnemy) && _teleportCooldownRemaining <= 0)
            {
                foreach (var entrance in UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: true))
                {
                    if (Vector3.Distance(position, entrance.transform.position) < interactRange)
                    {
                        // Find the matching exit point (inside<->outside)
                        var exitPoint = GetExitPointFromDoor(entrance);
                        if (exitPoint != null)
                        {
                            TeleportEnemyToPosition(exitPoint.position);
                            _controlledEnemy.EnableEnemyMesh(true, false);
                        }
                        _teleportCooldownRemaining = TeleportDoorCooldown;
                        return;
                    }
                }
            }

            // Check for regular doors
            foreach (var door in UnityEngine.Object.FindObjectsOfType<DoorLock>(includeInactive: true))
            {
                if (Vector3.Distance(position, door.transform.position) < interactRange)
                {
                    OpenDoorAsEnemy(door);
                    _doorCooldownRemaining = DoorInteractionCooldown;
                    return;
                }
            }
        }

        /// 
        /// Find the exit point for an entrance door (the matching door on the other side).
        /// 
        private static Transform? GetExitPointFromDoor(EntranceTeleport entrance)
        {
            var allEntrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: true);
            var exitEntrance = allEntrances.FirstOrDefault(e => 
                e.entranceId == entrance.entranceId && 
                e.isEntranceToBuilding != entrance.isEntranceToBuilding);
            return exitEntrance?.entrancePoint;
        }

        /// 
        /// Teleport the controlled enemy to a position.
        /// 
        private static void TeleportEnemyToPosition(Vector3 position)
        {
            if (_controlledEnemy == null || _movement == null) return;

            _movement.SetPosition(position);
            _controlledEnemy.transform.position = position;
            
            // Sync position to other clients
            if (_controlledEnemy.IsSpawned)
            {
                _controlledEnemy.SyncPositionToClients();
            }
        }

        /// 
        /// Open a door as an enemy (uses proper enemy door open method).
        /// 
        private static void OpenDoorAsEnemy(DoorLock door)
        {
            if (door.isDoorOpened) return;

            // Try to use the animated trigger first
            if (door.gameObject.TryGetComponent(out AnimatedObjectTrigger trigger))
            {
                trigger.TriggerAnimationNonPlayer(false, true, false);
            }

            // Use the enemy-specific door open RPC
            door.OpenDoorAsEnemyServerRpc();
        }

        private void UpdateCamera()
        {
            if (_controlledEnemy == null || _mouseInput == null) return;

            var camera = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (camera == null) return;

            // Position camera behind and above enemy based on mouse input rotation
            float distance = 5f;
            float height = 3f;

            Vector3 offset = -_mouseInput.transform.forward * distance + Vector3.up * height;
            camera.transform.position = _controlledEnemy.transform.position + offset;
            camera.transform.LookAt(_controlledEnemy.transform.position + Vector3.up);
        }

        private void UpdateCursorTip()
        {
            if (_controlledEnemy == null || _controller == null) return;
            if (LethalMenuMod.LocalPlayer == null) return;

            string tip = "";
            var primary = _controller.GetPrimarySkillName(_controlledEnemy);
            var secondary = _controller.GetSecondarySkillName(_controlledEnemy);

            if (!string.IsNullOrEmpty(primary)) tip += $"[LMB] {primary}  ";
            if (!string.IsNullOrEmpty(secondary)) tip += $"[RMB] {secondary}";

            LethalMenuMod.LocalPlayer.cursorTip.text = tip;
        }

        private void ToggleAIControl()
        {
            if (_controlledEnemy?.agent == null || _movement == null) return;

            IsAIControlled = !IsAIControlled;

            if (IsAIControlled)
            {
                // Warp AI to current position
                _controlledEnemy.agent.Warp(_controlledEnemy.transform.position);
                _controlledEnemy.SyncPositionToClients();

                // Disable NoClip when enabling AI control (they conflict)
                if (NoClipEnabled)
                {
                    NoClipEnabled = false;
                    _movement.SetNoClipMode(false);
                }
            }

            _controlledEnemy.agent.updatePosition = IsAIControlled;
            _controlledEnemy.agent.updateRotation = IsAIControlled;
            _controlledEnemy.agent.isStopped = !IsAIControlled;
            _movement.SetPosition(_controlledEnemy.transform.position);
            _movement.enabled = !IsAIControlled;

            HUDManager.Instance?.DisplayTip("Enemy Control", IsAIControlled ? "AI Control: ON (observing)" : "AI Control: OFF (manual)");
        }

        /// 
        /// Take control of an enemy.
        /// 
        public static void TakeControl(EnemyAI enemy)
        {
            if (enemy == null || enemy.isEnemyDead)
            {
                HUDManager.Instance?.DisplayTip("Enemy Control", "Cannot control dead enemy");
                return;
            }

            // Get controller for this enemy type
            var controller = EnemyControllerRegistry.GetController(enemy);
            if (controller == null)
            {
                HUDManager.Instance?.DisplayTip("Enemy Control", 
                    $"No controller for {enemy.enemyType?.enemyName ?? enemy.GetType().Name}");
                return;
            }

            // Stop previous control
            if (IsControlling) StopControl();

            _controlledEnemy = enemy;
            _controller = controller;

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
            _movement.CharacterSprintSpeed = controller.SprintMultiplier(enemy);
            _movement.SetPosition(enemy.transform.position);

            // Sync animation speed if controller wants it
            if (controller.SyncAnimationSpeedEnabled(enemy) && enemy.agent != null)
            {
                _movement.CharacterSpeed = enemy.agent.speed;
            }

            // Disable AI
            if (enemy.agent != null)
            {
                enemy.agent.updatePosition = false;
                enemy.agent.updateRotation = false;
                enemy.agent.isStopped = true;
            }

            IsAIControlled = false;
            NoClipEnabled = false;
            _secondarySkillHeld = false;
            _firstUpdateAfterPossess = true;
            _mainEntrance = null;
            Hack.EnemyControl.SetEnabled(true);

            // Switch audio listener
            if (LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.activeAudioListener.enabled = false;
                if (StartOfRound.Instance != null)
                {
                    StartOfRound.Instance.audioListener = _audioListener;
                }
            }

            // Store original camera parent for restoration
            _originalCameraParent = LethalMenuMod.LocalPlayer?.gameplayCamera?.transform.parent;

            // Notify controller
            _controller.OnTakeControl(enemy);

            // Hide death UI if player is dead
            if (LethalMenuMod.LocalPlayer?.isPlayerDead == true)
            {
                HUDManager.Instance?.holdButtonToEndGameEarlyMeter?.gameObject?.SetActive(false);
            }

            string enemyName = enemy.enemyType?.enemyName ?? "enemy";
            HUDManager.Instance?.DisplayTip("Enemy Control",
                $"Controlling {enemyName}\n" +
                "LMB=Primary | RMB=Secondary | E=Door\n" +
                "N=NoClip | F9=AI | Z=Release | Del=Kill");
        }

        /// 
        /// Stop controlling the current enemy.
        /// 
        public static void StopControl()
        {
            if (_controlledEnemy != null && _controller != null)
            {
                _controller.OnReleaseControl(_controlledEnemy);

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
                if (StartOfRound.Instance != null)
                {
                    StartOfRound.Instance.audioListener = LethalMenuMod.LocalPlayer.activeAudioListener;
                }

                // Reset camera to player
                var camera = LethalMenuMod.LocalPlayer.gameplayCamera;
                if (camera != null && _originalCameraParent != null)
                {
                    camera.transform.SetParent(_originalCameraParent);
                    camera.transform.localPosition = Vector3.zero;
                    camera.transform.localRotation = Quaternion.identity;
                }

                // Restore cursor tip
                LethalMenuMod.LocalPlayer.cursorTip.text = "";

                // Restore death UI if player is dead
                if (LethalMenuMod.LocalPlayer.isPlayerDead)
                {
                    HUDManager.Instance?.holdButtonToEndGameEarlyMeter?.gameObject?.SetActive(true);
                }
            }

            // Cleanup
            if (_controllerObject != null)
            {
                UnityEngine.Object.Destroy(_controllerObject);
            }

            _controlledEnemy = null;
            _controller = null;
            _controllerObject = null;
            _mouseInput = null;
            _movement = null;
            _audioListener = null;
            _originalCameraParent = null;
            _mainEntrance = null;
            IsAIControlled = false;
            NoClipEnabled = false;
            _secondarySkillHeld = false;
            _firstUpdateAfterPossess = true;
            Hack.EnemyControl.SetEnabled(false);
        }

        /// 
        /// Get the currently controlled enemy.
        /// 
        public static EnemyAI? GetControlledEnemy() => _controlledEnemy;

        /// 
        /// Get the current controller.
        /// 
        public static IEnemyController? GetCurrentController() => _controller;
    }
}
