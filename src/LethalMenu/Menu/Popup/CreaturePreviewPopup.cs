using System;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalMenu.Menu.Popup
{
    public class CreaturePreviewPopup : PopupMenu
    {
        private const int PreviewLayer = 30;
        private static readonly Vector3 PreviewPosition = new(10000f, -10000f, 10000f);
        private static readonly System.Collections.Generic.List<AssetBundle> PreviewBundles = new();
        private static bool _previewBundlesLoaded;
        private static string? _loadedBundleInfo;

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
        private Material? _previewMaterial;
        private readonly System.Collections.Generic.List<Material> _runtimeMaterials = new();
        private Texture? _debugTexture;
        private float _yaw = 180f;
        private bool _autoRotate = true;
        private MaterialMode _materialMode = MaterialMode.Original;
        private int _rendererCount;
        private int _texturedMaterialCount;
        private int _missingTextureMaterialCount;
        private string? _debugInfo;
        private string? _error;
        private string? _resolvedShaderName;
        private string? _firstSourceShaderName;
        private string? _firstTextureInfo;

        public CreaturePreviewPopup() : base("Creature Preview", 20010, 540, 470) { }

        private enum MaterialMode
        {
            Textured,
            Flat,
            Original
        }

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
            if (GUILayout.Button($"Material: {GetMaterialModeLabel()}", GUILayout.Width(150)))
            {
                CycleMaterialMode();
                RebuildPreview();
            }
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
            ReloadPreviewBundles();
            _error = null;
            _debugInfo = null;
            _debugTexture = null;
            _rendererCount = 0;
            _texturedMaterialCount = 0;
            _missingTextureMaterialCount = 0;
            _resolvedShaderName = null;
            _firstSourceShaderName = null;
            _firstTextureInfo = null;
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
            var liveInstance = TryCreateLivePreviewInstance(parent);
            if (liveInstance != null)
                return liveInstance;

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

        private GameObject? TryCreateLivePreviewInstance(Transform parent)
        {
            var sourcePrefab = _enemyType?.enemyPrefab;
            if (sourcePrefab == null)
                return null;

            GameObject? staging = null;
            GameObject? instance = null;
            try
            {
                staging = new GameObject("LethalMenu Preview Staging") { hideFlags = HideFlags.HideAndDontSave };
                staging.SetActive(false);

                instance = UnityEngine.Object.Instantiate(sourcePrefab, staging.transform);
                instance.name = $"LethalMenu Live Preview {sourcePrefab.name}";

                StripDangerousComponents(instance);
                StripRuntimeComponents(instance);
                DisableAnimationComponents(instance);
                SnapToIdlePose(instance);

                instance.transform.SetParent(parent, worldPositionStays: false);
                UnityEngine.Object.Destroy(staging);
                staging = null;

                _previewSource = "Live prefab";
                return instance;
            }
            catch (Exception)
            {
                if (instance != null)
                    UnityEngine.Object.Destroy(instance);
                if (staging != null)
                    UnityEngine.Object.Destroy(staging);
                return null;
            }
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
            _loadedBundleInfo = null;
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
                    {
                        PreviewBundles.Add(bundle);
                        var timestamp = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm:ss");
                        string info = $"{Path.GetFileName(file)} ({timestamp})";
                        _loadedBundleInfo = string.IsNullOrWhiteSpace(_loadedBundleInfo)
                            ? info
                            : $"{_loadedBundleInfo}, {info}";
                    }
                }
                catch
                {
                    // Ignore non-bundle files; this folder can also hold notes/source exports.
                }
            }
        }

        private static void ReloadPreviewBundles()
        {
            foreach (var bundle in PreviewBundles)
            {
                if (bundle != null)
                    bundle.Unload(true);
            }

            PreviewBundles.Clear();
            _previewBundlesLoaded = false;
            LoadPreviewBundles();
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
            DisableAnimationComponents(model);
            model.SetActive(true);
            ForcePreviewRenderersVisible(model);

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
            _camera.backgroundColor = Color.black;
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 20f;
            _camera.orthographic = true;
            _camera.orthographicSize = 1.25f;
            _camera.targetTexture = _renderTexture;
            _camera.forceIntoRenderTexture = true;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.useOcclusionCulling = false;
            _camera.enabled = false;

            ConfigureHDRPCameraIsolation(_camera);
        }

        private static void ConfigureHDRPCameraIsolation(Camera camera)
        {
            try
            {
                var hdData = camera.gameObject.GetComponent<HDAdditionalCameraData>()
                    ?? camera.gameObject.AddComponent<HDAdditionalCameraData>();

                hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                hdData.backgroundColorHDR = Color.black;
                hdData.clearDepth = true;
                hdData.volumeLayerMask = 0;
                hdData.probeLayerMask = 0;
                hdData.customRenderingSettings = true;

                var frameSettings = hdData.renderingPathCustomFrameSettings;
                var overrideMask = hdData.renderingPathCustomFrameSettingsOverrideMask;

                void Override(FrameSettingsField field, bool value)
                {
                    int index = (int)field;
                    overrideMask.mask[(uint)index] = true;
                    frameSettings.SetEnabled(field, value);
                }

                Override(FrameSettingsField.Postprocess, false);
                Override(FrameSettingsField.ExposureControl, false);
                Override(FrameSettingsField.AtmosphericScattering, false);
                Override(FrameSettingsField.Volumetrics, false);
                Override(FrameSettingsField.SkyReflection, false);
                Override(FrameSettingsField.SSR, false);
                Override(FrameSettingsField.SSAO, false);
                Override(FrameSettingsField.ContactShadows, false);
                Override(FrameSettingsField.ShadowMaps, false);
                Override(FrameSettingsField.Shadowmask, false);
                Override(FrameSettingsField.ScreenSpaceShadows, false);
                Override(FrameSettingsField.MotionVectors, false);
                Override(FrameSettingsField.ObjectMotionVectors, false);
                Override(FrameSettingsField.TransparentObjects, true);
                Override(FrameSettingsField.OpaqueObjects, true);

                hdData.renderingPathCustomFrameSettings = frameSettings;
                hdData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
            }
            catch (Exception)
            {
            }
        }

        private void CreatePreviewLight()
        {
            _lightObject = new GameObject("LethalMenu Creature Preview Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lightObject.transform.position = PreviewPosition + new Vector3(0f, 5f, -3f);
            _lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            _lightObject.layer = PreviewLayer;

            var light = _lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.cullingMask = 1 << PreviewLayer;
            light.shadows = LightShadows.None;
            light.color = Color.white;

            try
            {
                var hdLight = _lightObject.AddComponent<HDAdditionalLightData>();
                hdLight.intensity = 30000f;
                hdLight.lightUnit = LightUnit.Lux;
                hdLight.affectsVolumetric = false;
                hdLight.useContactShadow.useOverride = true;
                hdLight.useContactShadow.@override = false;
                hdLight.EnableShadows(false);
            }
            catch (Exception)
            {
            }
        }

        private static void StripDangerousComponents(GameObject root)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                    UnityEngine.Object.DestroyImmediate(behaviour);
            }

            foreach (var networkObject in root.GetComponentsInChildren<NetworkObject>(true))
            {
                if (networkObject != null)
                    UnityEngine.Object.DestroyImmediate(networkObject);
            }

            foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true))
            {
                if (agent != null)
                    UnityEngine.Object.DestroyImmediate(agent);
            }
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

        private static void DisableAnimationComponents(GameObject root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.enabled = false;
            }

            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
                animation.enabled = false;
        }

        private static void SnapToIdlePose(GameObject root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                if (animator == null || animator.runtimeAnimatorController == null)
                    continue;

                try
                {
                    animator.Play("Idle", 0, 0f);
                    animator.Update(0f);
                }
                catch
                {
                }

                animator.enabled = false;
            }
        }

        private void ForcePreviewRenderersVisible(GameObject root)
        {
            DisableNonCreatureRenderers(root);
            var renderers = GetPreviewRenderers(root);
            _rendererCount = renderers.Length;
            foreach (var renderer in renderers)
            {
                renderer.gameObject.SetActive(true);
                renderer.gameObject.layer = PreviewLayer;
                renderer.enabled = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = true;
                    skinned.forceMatrixRecalculationPerRender = true;
                }

                switch (_materialMode)
                {
                    case MaterialMode.Flat:
                        ApplyFlatMaterial(renderer);
                        break;
                    case MaterialMode.Textured:
                        ApplyTexturePreviewMaterials(renderer);
                        break;
                    case MaterialMode.Original:
                        break;
                }
            }
        }

        private void ApplyFlatMaterial(Renderer renderer)
        {
            var previewMaterial = GetFlatMaterial();
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = previewMaterial;
                return;
            }

            for (int i = 0; i < materials.Length; i++)
                materials[i] = previewMaterial;
            renderer.sharedMaterials = materials;
        }

        private void ApplyTexturePreviewMaterials(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return;

            for (int i = 0; i < materials.Length; i++)
                materials[i] = CreateRenderableMaterialCopy(materials[i]);
            renderer.sharedMaterials = materials;
        }

        private Material CreateRenderableMaterialCopy(Material? source)
        {
            Texture? texture = FindMainTexture(source);
            if (texture != null)
            {
                _texturedMaterialCount++;
                if (_debugTexture == null)
                {
                    _debugTexture = texture;
                    var tex2d = texture as Texture2D;
                    _firstTextureInfo = $"{texture.name} {texture.width}x{texture.height} {(tex2d != null ? tex2d.format.ToString() : texture.GetType().Name)}";
                }
            }
            else
            {
                _missingTextureMaterialCount++;
            }

            Color color = texture == null ? GetMaterialColor(source, GetFallbackCreatureColor()) : Color.white;
            Shader shader = texture != null
                ? FindTexturePreviewShader()
                : FindFlatPreviewShader();

            _resolvedShaderName ??= shader != null ? shader.name : "(null)";
            _firstSourceShaderName ??= source?.shader != null ? source.shader.name : "(null)";

            var material = new Material(shader)
            {
                name = $"LethalMenu Preview {source?.name ?? "Material"}",
                hideFlags = HideFlags.HideAndDontSave,
                color = color
            };

            SetMaterialColor(material, color);
            if (texture != null)
                SetMaterialTexture(material, texture);

            if (material.HasProperty("_Mode"))
                material.SetFloat("_Mode", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", 0f);
            material.renderQueue = -1;

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.15f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);

            _runtimeMaterials.Add(material);
            return material;
        }

        private static Shader FindTexturePreviewShader()
        {
            return Shader.Find("HDRP/Unlit") ??
                Shader.Find("HDRP/Lit") ??
                Shader.Find("Unlit/Texture") ??
                Shader.Find("Mobile/Diffuse") ??
                Shader.Find("Legacy Shaders/Diffuse") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
        }

        private static Shader FindFlatPreviewShader()
        {
            return Shader.Find("HDRP/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Hidden/Internal-Colored") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
        }

        private string GetMaterialModeLabel()
        {
            switch (_materialMode)
            {
                case MaterialMode.Textured:
                    return "Textured";
                case MaterialMode.Flat:
                    return "Flat";
                case MaterialMode.Original:
                    return "Original";
                default:
                    return _materialMode.ToString();
            }
        }

        private void CycleMaterialMode()
        {
            _materialMode = _materialMode switch
            {
                MaterialMode.Original => MaterialMode.Textured,
                MaterialMode.Textured => MaterialMode.Flat,
                _ => MaterialMode.Original
            };
        }

        private static void DisableNonCreatureRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    if (!IsCreatureRenderer(renderer))
                        renderer.enabled = false;
                }
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
            string materialInfo = _materialMode == MaterialMode.Textured
                ? $"  Textures: {_texturedMaterialCount}/{_texturedMaterialCount + _missingTextureMaterialCount}"
                : string.Empty;
            string bundleInfo = string.IsNullOrWhiteSpace(_loadedBundleInfo) ? string.Empty : $"  Bundle: {_loadedBundleInfo}";
            string shaderInfo = string.IsNullOrWhiteSpace(_resolvedShaderName) ? string.Empty : $"  Shader: {_resolvedShaderName}";
            string srcShaderInfo = string.IsNullOrWhiteSpace(_firstSourceShaderName) ? string.Empty : $"  SrcShader: {_firstSourceShaderName}";
            string texInfo = string.IsNullOrWhiteSpace(_firstTextureInfo) ? string.Empty : $"  Tex: {_firstTextureInfo}";
            _debugInfo = $"Source: {_previewSource}  Renderers: {_rendererCount}  Material: {GetMaterialModeLabel()}{materialInfo}{shaderInfo}{srcShaderInfo}{texInfo}  Bounds: {bounds.size.x:F2}, {bounds.size.y:F2}, {bounds.size.z:F2}{bundleInfo}";

            return true;
        }

        private static Renderer[] GetPreviewRenderers(GameObject root)
        {
            var allRenderers = root.GetComponentsInChildren<Renderer>(true);
            var previewRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && IsCreatureRenderer(renderer))
                    previewRenderers.Add(renderer);
            }

            return previewRenderers.ToArray();
        }

        private static bool IsCreatureRenderer(Renderer renderer)
        {
            string path = GetTransformPath(renderer.transform).ToLowerInvariant();
            return !path.Contains("scannode") &&
                !path.Contains("scan node") &&
                !path.Contains("scan") &&
                !path.Contains("mapdot") &&
                !path.Contains("map dot") &&
                !path.Contains("map") &&
                !path.Contains("radar") &&
                !path.Contains("terminal");
        }

        private static string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private Material GetFlatMaterial()
        {
            if (_previewMaterial != null)
                return _previewMaterial;

            Shader shader = Shader.Find("Unlit/Color") ??
                FindFlatPreviewShader();

            Color color = GetFallbackCreatureColor();
            _previewMaterial = new Material(shader)
            {
                name = "LethalMenu Creature Preview Material",
                hideFlags = HideFlags.HideAndDontSave,
                color = color
            };

            SetMaterialColor(_previewMaterial, color);

            if (_previewMaterial.HasProperty("_Glossiness"))
                _previewMaterial.SetFloat("_Glossiness", 0.15f);
            if (_previewMaterial.HasProperty("_Metallic"))
                _previewMaterial.SetFloat("_Metallic", 0f);

            return _previewMaterial;
        }

        private static Color GetFallbackCreatureColor()
        {
            return new Color(0.38f, 0.35f, 0.30f, 1f);
        }

        private static Color GetMaterialColor(Material? material, Color fallback)
        {
            if (material == null)
                return fallback;

            foreach (string property in new[] { "_Color", "_BaseColor", "_TintColor" })
            {
                if (!material.HasProperty(property))
                    continue;

                Color color = material.GetColor(property);
                if (color.maxColorComponent > 0.04f)
                    return color;
            }

            return fallback;
        }

        private static Texture? FindMainTexture(Material? material)
        {
            if (material == null)
                return null;

            foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_BaseColorTexture", "_UnlitColorMap", "_Albedo", "_DiffuseMap" })
            {
                if (!material.HasProperty(property))
                    continue;

                Texture texture = material.GetTexture(property);
                if (texture != null)
                    return texture;
            }

            return material.mainTexture;
        }

        private static void SetMaterialTexture(Material material, Texture texture)
        {
            foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_BaseColorTexture", "_UnlitColorMap", "_Albedo", "_DiffuseMap" })
            {
                if (material.HasProperty(property))
                    material.SetTexture(property, texture);
            }

            material.mainTexture = texture;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", color);
            if (material.HasProperty("_TintColor"))
                material.SetColor("_TintColor", color);
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

            if (_materialMode == MaterialMode.Textured && _debugTexture != null)
            {
                Rect textureRect = new(rect.x + 8f, rect.y + 8f, 88f, 88f);
                GUI.Box(textureRect, GUIContent.none);
                GUI.DrawTexture(textureRect, _debugTexture, ScaleMode.ScaleToFit, true);
            }
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

            if (_previewMaterial != null)
            {
                UnityEngine.Object.Destroy(_previewMaterial);
                _previewMaterial = null;
            }

            foreach (var material in _runtimeMaterials)
            {
                if (material != null)
                    UnityEngine.Object.Destroy(material);
            }
            _runtimeMaterials.Clear();

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
        }
    }
}
