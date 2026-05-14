using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class CreaturePreviewPopup : PopupMenu
    {
        private const int PreviewLayer = 30;
        private static readonly Vector3 PreviewPosition = new(10000f, -10000f, 10000f);
        private static readonly System.Collections.Generic.List<AssetBundle> PreviewBundles = new();
        private static bool _previewBundlesLoaded;

        private EnemyType? _enemyType;
        private string? _creatureName;
        private string _previewSource = "Bundled model";
        private string? _missingModelHint;
        private GameObject? _previewRoot;
        private GameObject? _modelInstance;
        private GameObject? _cameraObject;
        private GameObject? _lightObject;
        private Camera? _camera;
        private RenderTexture? _renderTexture;
        private Animator[] _animators = Array.Empty<Animator>();
        private float _yaw = 180f;
        private bool _autoRotate = true;
        private int _rendererCount;
        private string? _debugInfo;
        private string? _error;

        public CreaturePreviewPopup() : base("Creature Preview", 20010, 540, 470) { }

        public void Show(EnemyType enemyType, string? creatureName = null)
        {
            if (_enemyType == enemyType && _creatureName == creatureName && _renderTexture != null && _modelInstance != null)
            {
                IsOpen = true;
                return;
            }

            _enemyType = enemyType;
            _creatureName = creatureName;
            IsOpen = true;
            RebuildPreview();
        }

        public static EnemyType? FindEnemyType(string? creatureName)
        {
            if (string.IsNullOrWhiteSpace(creatureName))
                return null;

            string normalizedCreatureName = NormalizeEnemyName(creatureName);
            var allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (string alias in GetAliasCandidates(normalizedCreatureName))
            {
                var aliasMatch = FindEnemyTypeByNormalizedName(allEnemyTypes, NormalizeEnemyName(alias));
                if (aliasMatch != null)
                    return aliasMatch;
            }

            var exactMatch = FindEnemyTypeByNormalizedName(allEnemyTypes, normalizedCreatureName);
            if (exactMatch != null)
                return exactMatch;

            foreach (var enemyType in allEnemyTypes)
            {
                if (enemyType == null)
                    continue;

                foreach (string candidate in GetEnemyTypeCandidates(enemyType))
                {
                    if (normalizedCreatureName.Contains(candidate) || candidate.Contains(normalizedCreatureName))
                        return enemyType;
                }
            }

            return null;
        }

        private static EnemyType? FindEnemyTypeByNormalizedName(EnemyType[] allEnemyTypes, string normalizedName)
        {
            foreach (var enemyType in allEnemyTypes)
            {
                if (enemyType == null)
                    continue;

                foreach (string candidate in GetEnemyTypeCandidates(enemyType))
                {
                    if (candidate.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
                        return enemyType;
                }
            }

            return null;
        }

        private static string[] GetEnemyTypeCandidates(EnemyType enemyType)
        {
            var candidates = new System.Collections.Generic.List<string>();

            AddCandidate(candidates, enemyType.enemyName);
            AddCandidate(candidates, enemyType.name);
            if (enemyType.enemyPrefab != null)
            {
                AddCandidate(candidates, enemyType.enemyPrefab.name);
                var enemyAi = enemyType.enemyPrefab.GetComponent<EnemyAI>();
                if (enemyAi != null)
                    AddCandidate(candidates, enemyAi.GetType().Name);
            }

            return candidates.ToArray();
        }

        private static void AddCandidate(System.Collections.Generic.List<string> candidates, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string normalized = NormalizeEnemyName(value);
            if (!string.IsNullOrWhiteSpace(normalized) && !candidates.Contains(normalized))
                candidates.Add(normalized);
        }

        private static string[] GetAliasCandidates(string normalizedCreatureName)
        {
            switch (normalizedCreatureName)
            {
                case "forestkeeper":
                case "forestkeepers":
                    return new[] { "forestgiant", "forestgiantai", "giant" };
                case "bracken":
                case "brackens":
                    return new[] { "flowerman", "flowermanai" };
                case "bunkerspider":
                case "bunkerspiders":
                    return new[] { "sandspider", "sandspiderai" };
                case "hoardingbug":
                case "hoardingbugs":
                    return new[] { "hoarderbug", "hoarderbugai" };
                case "snareflea":
                case "snarefleas":
                    return new[] { "centipede", "centipedeai" };
                case "thumper":
                case "thumpers":
                    return new[] { "crawler", "crawlerai" };
                case "hygrodere":
                case "hygroderes":
                    return new[] { "blob", "blobai" };
                case "sporelizard":
                case "sporelizards":
                    return new[] { "puffer", "pufferai" };
                case "eyelessdog":
                case "eyelessdogs":
                    return new[] { "mouthdog", "mouthdogai" };
                case "circuitbee":
                case "circuitbees":
                    return new[] { "redlocustbees", "redlocustbeesai", "docilelocustbees", "docilelocustbeesai" };
                case "earthleviathan":
                case "earthleviathans":
                    return new[] { "sandworm", "sandwormai" };
                case "babooneagle":
                case "baboonhawk":
                case "baboonhawks":
                    return new[] { "baboonbird", "baboonbirdai" };
                case "coilhead":
                case "coilheads":
                    return new[] { "springman", "springmanai" };
                case "manticoil":
                case "manticoils":
                    return new[] { "doublewing", "doublewingai" };
                case "roaminglocust":
                case "roaminglocusts":
                    return new[] { "docilelocustbees", "docilelocustbeesai" };
                case "oldbird":
                case "oldbirds":
                    return new[] { "radmech", "radmechai" };
                case "maneater":
                case "maneaters":
                    return new[] { "cavedweller", "cavedwellerai" };
                case "kidnapperfox":
                case "kidnapperfoxes":
                    return new[] { "bushwolf", "bushwolfenemy", "bushwolfenemyai" };
                case "tulipsnake":
                case "tulipsnakes":
                    return new[] { "flowersnake", "flowersnakeenemy", "flowersnakeenemyai" };
                case "barber":
                case "barbers":
                    return new[] { "claysurgeon", "claysurgeonai" };
                default:
                    return Array.Empty<string>();
            }
        }

        private static string NormalizeEnemyName(string name)
        {
            string normalized = name
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Trim()
                .ToLowerInvariant();

            return normalized.EndsWith("s") ? normalized.Substring(0, normalized.Length - 1) : normalized;
        }

        protected override void DrawBody()
        {
            if (_enemyType == null)
            {
                GUILayout.Label("No creature selected.");
                return;
            }

            if (_renderTexture == null && string.IsNullOrWhiteSpace(_error))
                RebuildPreview();

            GUILayout.Label(_enemyType.enemyName ?? "Unknown creature");

            if (!string.IsNullOrWhiteSpace(_error))
            {
                GUILayout.Label(_error);
                if (!string.IsNullOrWhiteSpace(_debugInfo))
                    GUILayout.TextArea(_debugInfo);
                if (GUILayout.Button("Retry", GUILayout.Height(24)))
                    RebuildPreview();
                return;
            }

            if (_autoRotate)
                _yaw = Mathf.Repeat(_yaw + Time.unscaledDeltaTime * 25f, 360f);

            UpdatePreviewRotation();
            RenderPreview();
            DrawRenderTexture();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_autoRotate ? "Auto: ON" : "Auto: OFF", GUILayout.Width(85)))
                _autoRotate = !_autoRotate;
            if (GUILayout.Button("<", GUILayout.Width(40)))
                _yaw -= 15f;
            if (GUILayout.Button(">", GUILayout.Width(40)))
                _yaw += 15f;
            if (GUILayout.Button("Reset", GUILayout.Width(70)))
                _yaw = 180f;
            if (GUILayout.Button("Rebuild", GUILayout.Width(80)))
                RebuildPreview();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(_debugInfo))
                GUILayout.Label(_debugInfo);
        }

        protected override void OnClose()
        {
            DisposePreview();
        }

        private void RebuildPreview()
        {
            DisposePreview();
            _error = null;
            _debugInfo = null;
            _rendererCount = 0;
            _missingModelHint = null;

            try
            {
                _renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
                {
                    name = "LethalMenuCreaturePreview"
                };
                _renderTexture.Create();

                _previewRoot = new GameObject("LethalMenu Creature Preview Root")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _previewRoot.transform.position = PreviewPosition;
                _previewRoot.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

                _modelInstance = CreatePreviewInstance(_previewRoot.transform);
                if (_modelInstance == null)
                {
                    _error = "Preview AssetBundle prefab not found.";
                    _debugInfo = _missingModelHint;
                    DisposePreview();
                    return;
                }

                _modelInstance.hideFlags = HideFlags.HideAndDontSave;
                _modelInstance.SetActive(false);

                SetupPreviewModel(_modelInstance);
            }
            catch (Exception ex)
            {
                _error = $"Preview failed: {ex.GetType().Name}";
                DisposePreview();
            }
        }

        private GameObject? CreatePreviewInstance(Transform parent)
        {
            var bundledPrefab = FindBundledPreviewPrefab();
            if (bundledPrefab != null)
            {
                _previewSource = "Bundled model";
                var instance = UnityEngine.Object.Instantiate(bundledPrefab, parent);
                instance.name = $"LethalMenu Bundled Preview {_creatureName ?? _enemyType?.enemyName}";
                return instance;
            }

            return null;
        }

        private GameObject? FindBundledPreviewPrefab()
        {
            LoadPreviewBundles();
            var candidates = GetPreviewAssetCandidates();
            if (PreviewBundles.Count == 0)
            {
                _missingModelHint = $"Put preview AssetBundles here:\n{GetPreviewDirectory()}\n\nExpected .bundle, .assetbundle, .unity3d, or extensionless AssetBundle files containing creature preview prefabs.";
                return null;
            }

            foreach (var bundle in PreviewBundles)
            {
                if (bundle == null) continue;

                foreach (string assetName in bundle.GetAllAssetNames())
                {
                    string normalizedAssetName = NormalizeEnemyName(Path.GetFileNameWithoutExtension(assetName));
                    foreach (string candidate in candidates)
                    {
                        if (normalizedAssetName.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                            return bundle.LoadAsset<GameObject>(assetName);
                    }
                }
            }

            _missingModelHint = $"Put preview AssetBundles here:\n{GetPreviewDirectory()}\n\nMissing bundled prefab. Tried: {string.Join(", ", candidates)}";
            return null;
        }

        private string[] GetPreviewAssetCandidates()
        {
            var candidates = new System.Collections.Generic.List<string>();
            AddCandidate(candidates, _creatureName);

            if (_enemyType != null)
            {
                foreach (string candidate in GetEnemyTypeCandidates(_enemyType))
                    AddCandidate(candidates, candidate);
            }

            foreach (string alias in GetAliasCandidates(NormalizeEnemyName(_creatureName ?? _enemyType?.enemyName ?? string.Empty)))
                AddCandidate(candidates, alias);

            return candidates.ToArray();
        }

        private static void LoadPreviewBundles()
        {
            if (_previewBundlesLoaded)
                return;

            _previewBundlesLoaded = true;
            string previewDirectory = GetPreviewDirectory();
            if (!Directory.Exists(previewDirectory))
            {
                Directory.CreateDirectory(previewDirectory);
                return;
            }

            foreach (string file in Directory.GetFiles(previewDirectory, "*", SearchOption.AllDirectories))
            {
                if (!LooksLikePreviewBundleFile(file))
                    continue;

                try
                {
                    var bundle = AssetBundle.LoadFromFile(file);
                    if (bundle != null)
                        PreviewBundles.Add(bundle);
                }
                catch
                {
                    // Ignore non-bundle files; this folder can also hold notes/source exports.
                }
            }
        }

        private static bool LooksLikePreviewBundleFile(string file)
        {
            string extension = Path.GetExtension(file);
            return string.IsNullOrWhiteSpace(extension) ||
                extension.Equals(".bundle", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".assetbundle", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".unity3d", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPreviewDirectory()
        {
            string menuDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            return Path.Combine(menuDirectory, "CreaturePreviews");
        }

        private void SetupPreviewModel(GameObject model)
        {
            SetLayerRecursive(model, PreviewLayer);
            StripRuntimeComponents(model);
            ForcePreviewRenderersVisible(model);
            PrepareAnimators(model);
            model.SetActive(true);

            if (!NormalizeModel(model))
            {
                _error = "Creature has no renderers.";
                DisposePreview();
                return;
            }

            CreatePreviewCamera();
            CreatePreviewLight();
            UpdatePreviewRotation();
        }

        private void CreatePreviewCamera()
        {
            _cameraObject = new GameObject("LethalMenu Creature Preview Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _cameraObject.transform.position = PreviewPosition + new Vector3(0f, 0.25f, -4f);
            _cameraObject.transform.LookAt(PreviewPosition + new Vector3(0f, 0.2f, 0f));

            _camera = _cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 20f;
            _camera.orthographic = true;
            _camera.orthographicSize = 1.25f;
            _camera.targetTexture = _renderTexture;
            _camera.forceIntoRenderTexture = true;
            _camera.enabled = false;
        }

        private void CreatePreviewLight()
        {
            _lightObject = new GameObject("LethalMenu Creature Preview Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var light = _lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.cullingMask = 1 << PreviewLayer;
        }

        private static void StripRuntimeComponents(GameObject root)
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;

            foreach (var light in root.GetComponentsInChildren<Light>(true))
                light.enabled = false;

            foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            foreach (var trailRenderer in root.GetComponentsInChildren<TrailRenderer>(true))
                trailRenderer.enabled = false;

            foreach (var lineRenderer in root.GetComponentsInChildren<LineRenderer>(true))
                lineRenderer.enabled = false;

            foreach (var audioSource in root.GetComponentsInChildren<AudioSource>(true))
                audioSource.enabled = false;

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
        }

        private void PrepareAnimators(GameObject root)
        {
            _animators = root.GetComponentsInChildren<Animator>(true);
            foreach (var animator in _animators)
            {
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void ForcePreviewRenderersVisible(GameObject root)
        {
            var renderers = GetPreviewRenderers(root);
            _rendererCount = renderers.Length;
            foreach (var renderer in renderers)
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private bool NormalizeModel(GameObject model)
        {
            var renderers = GetPreviewRenderers(model);
            if (renderers.Length == 0)
                return false;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            CenterModel(model, bounds);

            renderers = GetPreviewRenderers(model);
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float largestSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (largestSize > 0.001f)
                model.transform.localScale *= 1.9f / largestSize;

            renderers = GetPreviewRenderers(model);
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            CenterModel(model, bounds);

            _rendererCount = renderers.Length;
            _debugInfo = $"Source: {_previewSource}  Renderers: {_rendererCount}  Bounds: {bounds.size.x:F2}, {bounds.size.y:F2}, {bounds.size.z:F2}";

            return true;
        }

        private static Renderer[] GetPreviewRenderers(GameObject root)
        {
            var allRenderers = root.GetComponentsInChildren<Renderer>(true);
            var previewRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                    previewRenderers.Add(renderer);
            }

            return previewRenderers.ToArray();
        }

        private static void CenterModel(GameObject model, Bounds bounds)
        {
            var parent = model.transform.parent;
            if (parent == null) return;

            Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
            model.transform.localPosition -= localCenter;
        }

        private void UpdatePreviewRotation()
        {
            if (_previewRoot != null)
                _previewRoot.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void RenderPreview()
        {
            if (_camera == null || _renderTexture == null)
                return;

            var previousActive = RenderTexture.active;
            try
            {
                float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 1f / 60f);
                foreach (var animator in _animators)
                {
                    if (animator != null && animator.enabled)
                        animator.Update(deltaTime);
                }

                _camera.targetTexture = _renderTexture;
                _camera.Render();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private void DrawRenderTexture()
        {
            if (_renderTexture == null)
                return;

            Rect rect = GUILayoutUtility.GetRect(500f, 320f, GUILayout.ExpandWidth(true), GUILayout.Height(320f));
            GUI.Box(rect, GUIContent.none);
            GUI.DrawTexture(rect, _renderTexture, ScaleMode.ScaleToFit, false);
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private void DisposePreview()
        {
            if (_camera != null)
                _camera.targetTexture = null;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_modelInstance != null)
                UnityEngine.Object.Destroy(_modelInstance);
            if (_previewRoot != null)
                UnityEngine.Object.Destroy(_previewRoot);
            if (_cameraObject != null)
                UnityEngine.Object.Destroy(_cameraObject);
            if (_lightObject != null)
                UnityEngine.Object.Destroy(_lightObject);

            _modelInstance = null;
            _previewRoot = null;
            _cameraObject = null;
            _lightObject = null;
            _camera = null;
            _animators = Array.Empty<Animator>();
        }
    }
}
