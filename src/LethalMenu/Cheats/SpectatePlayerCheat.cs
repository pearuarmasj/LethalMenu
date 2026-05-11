using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public class SpectatePlayerCheat : CheatBase
    {
        public override string Name => "SpectatePlayer";
        public override Hack HackType => Hack.SpectatePlayer;

        private Camera? _specCam;
        private AudioListener? _audioListener;
        private bool _wasEnabled;
        private Camera? _originalCamera;

        public override void OnUpdate()
        {
            bool shouldEnable = IsEnabled && Settings.SpectatePlayerIndex >= 0;

            if (shouldEnable && !_wasEnabled)
            {
                EnableSpectate();
            }
            else if (!shouldEnable && _wasEnabled)
            {
                DisableSpectate();
            }

            _wasEnabled = shouldEnable;

            if (!shouldEnable || _specCam == null) return;

            FollowPlayer();
        }

        public override void OnGUI()
        {
            if (!IsEnabled || Settings.SpectatePlayerIndex < 0) return;

            var player = GetSpectatedPlayer();
            if (player == null) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.white;

            string text = $"Spectating: {player.playerUsername}";
            var content = new GUIContent(text);
            var size = style.CalcSize(content);

            var shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(Screen.width / 2 - size.x / 2 + 2, 52, size.x, size.y), text, shadowStyle);

            GUI.Label(new Rect(Screen.width / 2 - size.x / 2, 50, size.x, size.y), text, style);
        }

        private PlayerControllerB? GetSpectatedPlayer()
        {
            if (Settings.SpectatePlayerIndex < 0 || Settings.SpectatePlayerIndex >= LethalMenuMod.Players.Count)
                return null;

            return LethalMenuMod.Players[Settings.SpectatePlayerIndex];
        }

        private void EnableSpectate()
        {
            if (LethalMenuMod.LocalPlayer == null) return;

            var player = GetSpectatedPlayer();
            if (player == null || player == LethalMenuMod.LocalPlayer)
            {
                Hack.SpectatePlayer.SetEnabled(false);
                return;
            }

            _originalCamera = LethalMenuMod.LocalPlayer.gameplayCamera;
            if (_originalCamera == null) return;

            var camObj = new GameObject("LethalMenuSpecCam");
            _specCam = camObj.AddComponent<Camera>();
            _specCam.CopyFrom(_originalCamera);
            _specCam.nearClipPlane = 0.01f;
            _specCam.farClipPlane = 1000f;

            _audioListener = camObj.AddComponent<AudioListener>();

            _originalCamera.enabled = false;

            if (LethalMenuMod.LocalPlayer.activeAudioListener != null)
            {
                LethalMenuMod.LocalPlayer.activeAudioListener.enabled = false;
            }

            Loader.Log($"Spectating player: {player.playerUsername}");
        }

        private void DisableSpectate()
        {
            if (_specCam != null)
            {
                Object.Destroy(_specCam.gameObject);
                _specCam = null;
            }

            _audioListener = null;

            if (_originalCamera != null)
            {
                _originalCamera.enabled = true;
            }

            if (LethalMenuMod.LocalPlayer != null)
            {
                if (LethalMenuMod.LocalPlayer.activeAudioListener != null)
                {
                    LethalMenuMod.LocalPlayer.activeAudioListener.enabled = true;
                }
            }

            Loader.Log("Spectate disabled");
        }

        private void FollowPlayer()
        {
            if (_specCam == null) return;

            var player = GetSpectatedPlayer();
            if (player == null || player.isPlayerDead)
            {
                Hack.SpectatePlayer.SetEnabled(false);
                return;
            }

            var playerCam = player.gameplayCamera;
            if (playerCam != null)
            {
                _specCam.transform.position = playerCam.transform.position;
                _specCam.transform.rotation = playerCam.transform.rotation;
                _specCam.fieldOfView = playerCam.fieldOfView;
            }
        }

        public void ForceDisable()
        {
            Hack.SpectatePlayer.SetEnabled(false);
            Settings.SpectatePlayerIndex = -1;
            DisableSpectate();
            _wasEnabled = false;
        }

        public void CycleNextPlayer()
        {
            if (LethalMenuMod.Players.Count <= 1) return;

            int startIndex = Settings.SpectatePlayerIndex;
            int nextIndex = (startIndex + 1) % LethalMenuMod.Players.Count;

            while (nextIndex != startIndex)
            {
                var player = LethalMenuMod.Players[nextIndex];
                if (player != null && player != LethalMenuMod.LocalPlayer && !player.isPlayerDead)
                {
                    Settings.SpectatePlayerIndex = nextIndex;
                    if (_wasEnabled)
                    {
                        DisableSpectate();
                        EnableSpectate();
                    }
                    return;
                }
                nextIndex = (nextIndex + 1) % LethalMenuMod.Players.Count;
            }
        }

        public void CyclePreviousPlayer()
        {
            if (LethalMenuMod.Players.Count <= 1) return;

            int startIndex = Settings.SpectatePlayerIndex;
            int prevIndex = (startIndex - 1 + LethalMenuMod.Players.Count) % LethalMenuMod.Players.Count;

            while (prevIndex != startIndex)
            {
                var player = LethalMenuMod.Players[prevIndex];
                if (player != null && player != LethalMenuMod.LocalPlayer && !player.isPlayerDead)
                {
                    Settings.SpectatePlayerIndex = prevIndex;
                    if (_wasEnabled)
                    {
                        DisableSpectate();
                        EnableSpectate();
                    }
                    return;
                }
                prevIndex = (prevIndex - 1 + LethalMenuMod.Players.Count) % LethalMenuMod.Players.Count;
            }
        }
    }
}
