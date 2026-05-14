using UnityEngine;
using UnityEngine.Video;

namespace LethalMenu.Menu.Popup
{
    public class BestiaryManagerPopup : PopupMenu
    {
        private readonly CreaturePreviewPopup _creaturePreviewPopup;
        private int _selectedEntryIndex;
        private Vector2 _entryScrollPosition;
        private string? _previewMessage;
        private GameObject? _videoObject;
        private VideoPlayer? _videoPlayer;
        private RenderTexture? _videoTexture;
        private VideoClip? _activeVideoClip;

        public BestiaryManagerPopup(CreaturePreviewPopup creaturePreviewPopup) : base("Bestiary", 20008, 900, 680)
        {
            _creaturePreviewPopup = creaturePreviewPopup;
        }

        protected override void DrawBody()
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                GUILayout.Label("Terminal not available");
                return;
            }

            if (terminal.scannedEnemyIDs == null || terminal.scannedEnemyIDs.Count == 0)
            {
                GUILayout.Label("No data collected on wildlife. Scans are required.");
                return;
            }

            if (terminal.enemyFiles == null || terminal.enemyFiles.Count == 0)
            {
                GUILayout.Label("No bestiary files loaded.");
                return;
            }

            _selectedEntryIndex = Mathf.Clamp(_selectedEntryIndex, 0, terminal.scannedEnemyIDs.Count - 1);

            GUILayout.Label("--- Wildlife On Record ---");
            _entryScrollPosition = GUILayout.BeginScrollView(_entryScrollPosition, GUILayout.Height(150));
            for (int i = 0; i < terminal.scannedEnemyIDs.Count; i++)
            {
                int fileId = terminal.scannedEnemyIDs[i];
                var node = GetEnemyFile(terminal, fileId);
                string name = node?.creatureName ?? $"Unknown File {fileId}";
                string newTag = terminal.newlyScannedEnemyIDs != null && terminal.newlyScannedEnemyIDs.Contains(fileId) ? " (NEW)" : "";

                GUILayout.BeginHorizontal();
                bool selected = i == _selectedEntryIndex;
                if (GUILayout.Toggle(selected, "", GUILayout.Width(20)))
                    _selectedEntryIndex = i;
                GUILayout.Label($"{name}{newTag}");
                if (node != null && GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    terminal.LoadNewNode(node);
                    terminal.newlyScannedEnemyIDs?.Remove(fileId);
                    _selectedEntryIndex = i;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);

            int selectedFileId = terminal.scannedEnemyIDs[_selectedEntryIndex];
            var selectedNode = GetEnemyFile(terminal, selectedFileId);
            if (selectedNode == null)
            {
                GUILayout.Label($"Selected bestiary file {selectedFileId} is missing.");
                return;
            }

            GUILayout.Label($"--- {selectedNode.creatureName ?? "Unknown"} ---");
            GUILayout.BeginHorizontal();
            DrawTerminalMedia(selectedNode);
            GUILayout.BeginVertical();
            GUILayout.TextArea(CleanTerminalText(selectedNode.displayText), GUILayout.MinHeight(220));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("3D Preview", GUILayout.Width(120)))
                OpenCreaturePreview(selectedNode);
            if (!string.IsNullOrWhiteSpace(_previewMessage))
                GUILayout.Label(_previewMessage);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            DisposeVideoPreview();
        }

        private static TerminalNode? GetEnemyFile(Terminal terminal, int fileId)
        {
            if (terminal.enemyFiles == null || fileId < 0 || fileId >= terminal.enemyFiles.Count)
                return null;

            return terminal.enemyFiles[fileId];
        }

        private static string CleanTerminalText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "No terminal entry text.";

            return text.Replace("\r\n", "\n").Trim();
        }

        private void DrawTerminalMedia(TerminalNode node)
        {
            float mediaWidth = Mathf.Clamp(WindowRect.width * 0.42f, 320f, 620f);
            float mediaHeight = Mathf.Clamp(WindowRect.height * 0.46f, 240f, 520f);

            GUILayout.BeginVertical(GUILayout.Width(mediaWidth));
            if (node.displayTexture != null)
            {
                StopVideoPreview();
                float aspect = node.displayTexture.height > 0
                    ? node.displayTexture.width / (float)node.displayTexture.height
                    : 1f;
                float height = Mathf.Clamp(mediaWidth / Mathf.Max(0.1f, aspect), 120f, 220f);
                height = Mathf.Min(mediaHeight, Mathf.Max(height, 220f));
                Rect rect = GUILayoutUtility.GetRect(mediaWidth, height, GUILayout.Width(mediaWidth), GUILayout.Height(height));
                GUI.DrawTexture(rect, node.displayTexture, ScaleMode.ScaleToFit, true);
            }
            else if (node.displayVideo != null)
            {
                EnsureVideoPreview(node.displayVideo);
                Rect rect = GUILayoutUtility.GetRect(mediaWidth, mediaHeight, GUILayout.Width(mediaWidth), GUILayout.Height(mediaHeight));
                if (_videoTexture != null)
                    GUI.DrawTexture(rect, _videoTexture, ScaleMode.ScaleToFit, false);
                else
                    GUI.Box(rect, "Terminal video loading");
            }
            else
            {
                StopVideoPreview();
                GUILayout.Box("No terminal image", GUILayout.Width(mediaWidth), GUILayout.Height(mediaHeight));
            }
            GUILayout.EndVertical();
        }

        private void OpenCreaturePreview(TerminalNode node)
        {
            var enemyType = CreaturePreviewPopup.FindEnemyType(node.creatureName);
            if (enemyType == null)
            {
                _previewMessage = "Enemy prefab not found.";
                return;
            }

            _previewMessage = null;
            _creaturePreviewPopup.Show(enemyType, node.creatureName);
        }

        private void EnsureVideoPreview(VideoClip clip)
        {
            if (_videoPlayer != null && _videoTexture != null && _activeVideoClip == clip)
            {
                if (!_videoPlayer.isPlaying)
                    _videoPlayer.Play();
                return;
            }

            DisposeVideoPreview();

            _videoTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32)
            {
                name = "LethalMenuBestiaryVideo"
            };
            _videoTexture.Create();

            _videoObject = new GameObject("LethalMenu Bestiary Video")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _videoPlayer = _videoObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _videoTexture;
            _videoPlayer.clip = clip;
            _activeVideoClip = clip;
            _videoPlayer.Play();
        }

        private void StopVideoPreview()
        {
            if (_videoPlayer != null && _videoPlayer.isPlaying)
                _videoPlayer.Stop();
        }

        private void DisposeVideoPreview()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.targetTexture = null;
            }

            if (_videoObject != null)
                Object.Destroy(_videoObject);

            if (_videoTexture != null)
            {
                _videoTexture.Release();
                Object.Destroy(_videoTexture);
            }

            _videoObject = null;
            _videoPlayer = null;
            _videoTexture = null;
            _activeVideoClip = null;
        }
    }
}
