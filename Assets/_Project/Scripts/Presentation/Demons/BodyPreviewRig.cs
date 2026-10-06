#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Data;
using DemonFighter.Simulation.Anatomy;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Renders a body for the mutation menu (GAME_DESIGN, "UI"): the same prefab, figure composition and part views
    /// the world uses, built from a body the menu composes, on a stage far below the world on the Preview layer,
    /// filmed by its own camera into a texture the menu shows. The figure turns slowly by itself and by drag. Fog is
    /// switched off while this camera renders, so the preview stays readable in a foggy cavern. No game rules live here.
    /// </summary>
    public sealed class BodyPreviewRig : MonoBehaviour
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 640;
        private const float CameraDistancePerMeter = 2.6f;
        private const float CameraHeightPerMeter = 0.6f;
        private const float LookAtHeightPerMeter = 0.45f;
        private const float FieldOfViewDegrees = 30f;
        private const float AutoTurnDegreesPerSecond = 20f;
        private const float AutoTurnPauseSeconds = 2f;
        private const float StartYawDegrees = 160f;
        private const float LightRange = 40f;
        private const float LightIntensity = 1.5f;
        private static readonly Vector3 StagePosition = new Vector3(0f, -500f, 0f);
        private static readonly Color BackgroundColor = new Color(0.05f, 0.02f, 0.02f, 1f);

        private DemonView _prefab = null!;
        private DemonViewSettings _settings = null!;
        private PartVisuals _visuals = null!;
        private Material _ownerMaterial = null!;
        private Transform _stage = null!;
        private Camera _camera = null!;
        private RenderTexture _texture = null!;
        private GameObject? _figure;
        private float _yawDegrees = StartYawDegrees;
        private float _autoTurnPausedFor;
        private bool _fogWas;

        /// <summary>The texture the camera renders into; the menu shows it as a background image.</summary>
        public RenderTexture Texture => _texture;

        /// <summary>Builds the rig under the parent: stage, camera with its texture, a light; nothing is shown until <see cref="Show"/>.</summary>
        public static BodyPreviewRig Create(Transform parent, DemonView prefab, PlaceholderPalette palette, DemonViewSettings settings, ContentCatalogDefinition catalog, Material ownerMaterial)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (ownerMaterial == null)
            {
                throw new ArgumentNullException(nameof(ownerMaterial));
            }

            var rigObject = new GameObject("Body Preview");
            rigObject.transform.SetParent(parent, false);
            rigObject.transform.position = StagePosition;
            BodyPreviewRig rig = rigObject.AddComponent<BodyPreviewRig>();
            rig._prefab = prefab;
            rig._settings = settings;
            rig._visuals = new PartVisuals(catalog, palette);
            rig._ownerMaterial = ownerMaterial;
            rig.Build();
            return rig;
        }

        /// <summary>Renders only while active, so a closed menu costs nothing.</summary>
        public void SetActive(bool active)
        {
            _camera.enabled = active;
        }

        /// <summary>Turns the figure by hand and pauses the slow automatic turn for a moment.</summary>
        public void Rotate(float degrees)
        {
            _yawDegrees += degrees;
            _autoTurnPausedFor = AutoTurnPauseSeconds;
            ApplyYaw();
        }

        /// <summary>Replaces the figure with this body at this size: the core of the prefab plus a view per attached part, composed like a world body.</summary>
        public void Show(Body body, float sizeMeters)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (_figure != null)
            {
                Destroy(_figure);
            }

            DemonView view = Instantiate(_prefab, _stage);
            GameObject figureObject = view.gameObject;
            figureObject.name = "Figure";
            _figure = figureObject;

            // The prefab brings the view and the controller for a demon it has not got; only its figure is wanted.
            BodyPartView core = view.GetComponentInChildren<BodyPartView>(true);
            DemonFigure figure = view.Figure;
            Destroy(view);
            CharacterController controller = figureObject.GetComponent<CharacterController>();
            if (controller != null)
            {
                Destroy(controller);
            }

            figure.Prepare(_visuals, body.Core.Spec.Id, _settings);
            figure.ApplySize(sizeMeters, _settings.RadiusPerMeter);
            figure.InitializeCore(core, null, body.Core.Index, _ownerMaterial);
            figure.SetDecorationsVisible(true);

            IReadOnlyList<BodyPart> parts = body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                if (part.Spec.IsCore || part.IsLost)
                {
                    continue;
                }

                int copyIndex = 0;
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(parts[j].Spec.Id, part.Spec.Id, StringComparison.Ordinal))
                    {
                        copyIndex++;
                    }
                }

                foreach (BodyPartView partView in figure.CreatePartViews(_visuals, part, null, _ownerMaterial, copyIndex, out _))
                {
                    partView.ShowUpgrade(part.UpgradeLevel, _settings.UpgradeGrowthPerLevel);
                }
            }

            // Measured before the part views are stripped: the core stands on its legs here too (D-094).
            figure.SnapStance();
            StripForPreview(figureObject);
            FrameCamera(sizeMeters + figure.Stance);
            ApplyYaw();
        }

        private void Update()
        {
            if (!_camera.enabled)
            {
                return;
            }

            if (_autoTurnPausedFor > 0f)
            {
                _autoTurnPausedFor -= Time.deltaTime;
                return;
            }

            _yawDegrees += AutoTurnDegreesPerSecond * Time.deltaTime;
            ApplyYaw();
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }

        private void Build()
        {
            _stage = new GameObject("Stage").transform;
            _stage.SetParent(transform, false);

            _texture = new RenderTexture(TextureWidth, TextureHeight, 24) { name = "Body Preview" };
            _texture.Create();

            var cameraObject = new GameObject("Preview Camera");
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = BackgroundColor;
            _camera.cullingMask = Layers.PreviewMask;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 100f;
            _camera.fieldOfView = FieldOfViewDegrees;
            _camera.depth = -10f;
            _camera.useOcclusionCulling = false;
            _camera.enabled = false;

            // The cavern glow is dim and far above; a light on the camera makes the figure readable.
            Light light = cameraObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = LightRange;
            light.intensity = LightIntensity;
            light.shadows = LightShadows.None;

            // The world camera must never see the stage, and the preview camera nothing but the stage.
            Camera? main = Camera.main;
            if (main != null)
            {
                main.cullingMask &= ~Layers.PreviewMask;
            }

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            FrameCamera(1f);
            ApplyYaw();
        }

        // Fog is a scene setting; it is switched off for the frames this camera renders and restored right after.
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                _fogWas = RenderSettings.fog;
                RenderSettings.fog = false;
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                RenderSettings.fog = _fogWas;
            }
        }

        private void FrameCamera(float sizeMeters)
        {
            _camera.transform.localPosition = new Vector3(0f, sizeMeters * CameraHeightPerMeter, -sizeMeters * CameraDistancePerMeter);
            _camera.transform.LookAt(transform.position + Vector3.up * (sizeMeters * LookAtHeightPerMeter));
        }

        private void ApplyYaw()
        {
            _stage.localRotation = Quaternion.Euler(0f, _yawDegrees, 0f);
        }

        // Part views, colliders and the Demon layer belong to bodies that fight; the figure only has to be seen.
        private static void StripForPreview(GameObject figure)
        {
            BodyPartView[] views = figure.GetComponentsInChildren<BodyPartView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                Destroy(views[i]);
            }

            Collider[] colliders = figure.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Destroy(colliders[i]);
            }

            Transform[] transforms = figure.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = Layers.Preview;
            }
        }
    }
}
