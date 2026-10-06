#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Data;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The transforms a demon body is composed of and the rules that place things on them, shared by the world view
    /// and the menu preview. Under the figure root sit the Body (the core: the prefab capsule, or the core mesh once
    /// a model is bound), the Placeholders (scaled like the capsule, where primitives of unbound parts hang as they
    /// did before M5) and the Rig (scaled by the body size, where parts with meshes hang on the socket anchors of
    /// the core). The root carries the procedural motion of the whole body (D-082), stands the core on the parts it
    /// walks on (D-094) and tilts for the corpse pose.
    /// No game rules live here.
    /// </summary>
    public sealed class DemonFigure
    {
        public const float CapsuleMeshHeight = 2f;
        public const float CapsuleMeshRadius = 0.5f;
        public const string FigureName = "Figure";
        public const string PlaceholdersName = "Placeholders";
        public const string RigName = "Rig";

        private const float MinimumHeight = 0.0001f;
        private const int CapsuleAlongY = 1;
        private const float StanceSettleSeconds = 0.3f;
        private const float RootFadePerRadius = 1.5f;
        private static readonly Vector3 FallbackAnchor = new Vector3(0f, 0.5f, 0f);
        private static readonly Quaternion LyingRotation = Quaternion.Euler(-90f, 0f, 0f);

        private readonly Transform _root;
        private readonly CapsuleCollider? _coreCapsule;
        private readonly Renderer[] _decorations;
        private readonly List<BodyPartView> _standing = new List<BodyPartView>();
        private readonly List<Vector4> _junctions = new List<Vector4>();
        private BodyPartView? _coreView;
        private Material? _coreMaterial;
        private Texture? _fleshSource;
        private float _size = 1f;
        private PartMeshSet? _coreMeshes;
        private Bounds _coreBounds;
        private IReadOnlyList<SocketAnchorDefinition> _anchors = Array.Empty<SocketAnchorDefinition>();
        private float _ownerTintBlend;

        /// <summary>Wires the figure under the view root; transforms the prefab lacks are created, so an older prefab still works.</summary>
        public DemonFigure(Transform root, Transform? figure, Transform body, Transform? placeholders, Transform? rig)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Body = body != null ? body : throw new ArgumentNullException(nameof(body));
            Root = figure != null ? figure : FindOrCreate(root, FigureName);
            if (Body.parent != Root)
            {
                Body.SetParent(Root, false);
            }

            Placeholders = placeholders != null ? placeholders : FindOrCreate(Root, PlaceholdersName);
            Rig = rig != null ? rig : FindOrCreate(Root, RigName);
            _coreCapsule = Body.GetComponent<CapsuleCollider>();

            // Renderers under the body that are no part of their own decorate the capsule (the snout).
            Renderer[] renderers = Body.GetComponentsInChildren<Renderer>(true);
            var decorations = new List<Renderer>(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].GetComponent<BodyPartView>() == null)
                {
                    decorations.Add(renderers[i]);
                }
            }

            _decorations = decorations.ToArray();
        }

        /// <summary>Carries the body motion while the demon lives and tilts for the corpse pose.</summary>
        public Transform Root { get; }

        /// <summary>The core: the prefab capsule or the bound core mesh, with the core part view and its trigger capsule.</summary>
        public Transform Body { get; }

        /// <summary>Capsule-unit space for the primitives of parts without meshes.</summary>
        public Transform Placeholders { get; }

        /// <summary>Body-unit space (scaled by the body size) for parts with meshes, hung on the socket anchors.</summary>
        public Transform Rig { get; }

        /// <summary>
        /// How high the core stands on the parts in the Locomotion socket, in meters: as far as they reach below it,
        /// so their lowest point rests on the ground. Zero while it crawls or once they are lost (D-094).
        /// </summary>
        public float Stance { get; private set; }

        /// <summary>True when the core wears a bound mesh instead of the capsule.</summary>
        public bool HasCoreMesh => _coreMeshes.HasValue;

        /// <summary>Reads the core meshes and the socket anchors from the content, once per bind.</summary>
        public void Prepare(PartVisuals visuals, string coreSpecId, DemonViewSettings settings)
        {
            if (visuals == null)
            {
                throw new ArgumentNullException(nameof(visuals));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _ownerTintBlend = settings.OwnerTintBlend;
            _anchors = visuals.AnchorsFor(coreSpecId);
            _coreMeshes = visuals.MeshesFor(coreSpecId);
            if (_coreMeshes.HasValue)
            {
                PartMeshSet set = _coreMeshes.Value;
                _coreBounds = MeshBounds.Transform(set.Intact.bounds, Matrix4x4.TRS(Vector3.zero, set.Rotation, Vector3.one * set.Scale));
            }
        }

        /// <summary>
        /// Sizes everything to the body: the capsule to the controller shape, the core mesh so its height is the body
        /// size with its base and center on the root, the placeholder space like the capsule, the rig by the size.
        /// </summary>
        public void ApplySize(float sizeMeters, float radiusPerMeter)
        {
            _size = Mathf.Max(sizeMeters, MinimumHeight);
            if (_coreView != null)
            {
                _coreView.SetFleshParent(CoreParent());
            }

            float radius = sizeMeters * radiusPerMeter;
            var capsuleScale = new Vector3(radius / CapsuleMeshRadius, sizeMeters / CapsuleMeshHeight, radius / CapsuleMeshRadius);
            Placeholders.localPosition = Vector3.up * (sizeMeters * 0.5f);
            Placeholders.localScale = capsuleScale;
            Rig.localPosition = Vector3.zero;
            Rig.localScale = Vector3.one * sizeMeters;

            if (_coreMeshes.HasValue)
            {
                PartMeshSet set = _coreMeshes.Value;
                float fit = sizeMeters / Mathf.Max(_coreBounds.size.y, MinimumHeight);
                Body.localScale = Vector3.one * (fit * set.Scale);
                Body.localRotation = set.Rotation;
                Body.localPosition = new Vector3(-_coreBounds.center.x, -_coreBounds.min.y, -_coreBounds.center.z) * fit + set.Offset * sizeMeters;
                if (_coreCapsule != null)
                {
                    // In the body's own units, before its scale and rotation: the trigger hugs the mesh.
                    Bounds local = set.Intact.bounds;
                    _coreCapsule.direction = CapsuleAlongY;
                    _coreCapsule.center = local.center;
                    _coreCapsule.height = local.size.y;
                    _coreCapsule.radius = Mathf.Min(local.size.y * 0.5f, Mathf.Max(local.extents.x, local.extents.z));
                }

                return;
            }

            // The capsule mesh is 2 units tall with radius 0.5; scale it to the controller's shape.
            Body.localScale = capsuleScale;
            Body.localRotation = Quaternion.identity;
            Body.localPosition = Vector3.up * (sizeMeters * 0.5f);
            if (_coreCapsule != null)
            {
                _coreCapsule.direction = CapsuleAlongY;
                _coreCapsule.center = Vector3.zero;
                _coreCapsule.height = CapsuleMeshHeight;
                _coreCapsule.radius = CapsuleMeshRadius;
            }
        }

        /// <summary>Poses the whole figure for this frame: the bob, the tilt and the squash and stretch of the body motion.</summary>
        public void Animate(Vector3 localPosition, Quaternion rotation, Vector3 scale)
        {
            Root.localPosition = localPosition + (Vector3.up * Stance);
            Root.localRotation = rotation;
            Root.localScale = scale;
        }

        /// <summary>Gives the core view its look: the capsule in the owner material, or the core meshes in their own material tinted by the owner.</summary>
        public void InitializeCore(BodyPartView core, DemonView? owner, int partIndex, Material ownerMaterial)
        {
            if (core == null)
            {
                throw new ArgumentNullException(nameof(core));
            }

            if (_coreMeshes.HasValue)
            {
                PartMeshSet set = _coreMeshes.Value;
                core.SetMeshes(set);
                Material material = set.Material != null ? set.Material : ownerMaterial;
                core.Initialize(owner, partIndex, material, set.HasOwnMaterial ? OwnerTint(ownerMaterial) : (Color?)null);
            }
            else
            {
                core.Initialize(owner, partIndex, ownerMaterial, null);
            }

            core.SetMotion(PartMotion.None, 0, false);

            // The core wears junction flesh where parts leave it (D-097); its collars wear its material.
            _coreView = core;
            _coreMaterial = _coreMeshes.HasValue && _coreMeshes.Value.Material != null ? _coreMeshes.Value.Material : ownerMaterial;
            _fleshSource = _coreMaterial.mainTexture;
            _junctions.Clear();
            core.SetFlesh(_fleshSource != null ? _fleshSource : Texture2D.whiteTexture, FleshTint(ownerMaterial), CoreParent(), Vector4.zero, 0f);
            core.SetFleshJunctions(_junctions);
        }

        /// <summary>
        /// Creates and initializes the view of a part: on the rig at its socket anchor when the part has meshes, as
        /// a primitive in the placeholder space otherwise; null when the part is drawn by the body itself.
        /// </summary>
        public List<BodyPartView> CreatePartViews(PartVisuals visuals, BodyPart part, DemonView? owner, Material ownerMaterial, int copyIndex, out bool ownerColored)
        {
            var views = new List<BodyPartView>(2);
            if (visuals == null)
            {
                throw new ArgumentNullException(nameof(visuals));
            }

            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            PartMeshSet? meshes = visuals.MeshesFor(part.Spec.Id);
            PartMotion motion = visuals.MotionFor(part.Spec.Id);
            if (meshes.HasValue)
            {
                if (!SocketAnchors.TryFind(_anchors, part.Spec.Socket, copyIndex, out Vector3 position, out Quaternion rotation))
                {
                    position = FallbackAnchor;
                    rotation = Quaternion.identity;
                }

                // A pair, as legs: two copies of one part that move on their own, the right half and its mirror image (D-099).
                if (meshes.Value.Pairing != PartPairing.None)
                {
                    PartMeshSet side = meshes.Value.Pairing == PartPairing.Split ? meshes.Value.Half() : meshes.Value;
                    var gait = new LegGait();
                    ownerColored = false;
                    for (int i = 0; i < 2; i++)
                    {
                        PartMeshSet placed = i == 0 ? side : side.Mirrored();
                        BodyPartView copy = visuals.CreateMeshPart(part, meshes.Value, Rig, position, rotation, ownerMaterial, out Material copyMaterial, out ownerColored, placed, gait, i);
                        Finish(copy, owner, part, copyMaterial, ownerColored, meshes.Value, motion, i, ownerMaterial);
                        views.Add(copy);
                    }

                    return views;
                }

                BodyPartView view = visuals.CreateMeshPart(part, meshes.Value, Rig, position, rotation, ownerMaterial, out Material material, out ownerColored);
                Finish(view, owner, part, material, ownerColored, meshes.Value, motion, copyIndex, ownerMaterial);
                views.Add(view);
                return views;
            }

            BodyPartView? primitive = visuals.Create(part, Placeholders, ownerMaterial, copyIndex, out Material primitiveMaterial, out ownerColored);
            if (primitive != null)
            {
                primitive.Initialize(owner, part.Index, primitiveMaterial, null);
                primitive.SetMotion(motion, copyIndex, false);
                StandOnIfLocomotion(part, primitive);
                views.Add(primitive);
            }

            return views;
        }

        // Look, motion, stance and collar of a new mesh part view.
        private void Finish(BodyPartView view, DemonView? owner, BodyPart part, Material material, bool ownerColored, in PartMeshSet meshes, PartMotion motion, int copyIndex, Material ownerMaterial)
        {
            view.Initialize(owner, part.Index, material, ownerColored && meshes.HasOwnMaterial ? OwnerTint(ownerMaterial) : (Color?)null);
            view.SetMotion(motion, copyIndex, meshes.Clip != null);
            StandOnIfLocomotion(part, view);
            GrowCollar(view, ownerMaterial);
        }

        /// <summary>
        /// The tint of the junction flesh and the collars: the owner tint on a textured core, the owner color on the
        /// capsule, so the joints match the body they grow from (D-097).
        /// </summary>
        public Color FleshTint(Material ownerMaterial)
        {
            if (ownerMaterial == null)
            {
                throw new ArgumentNullException(nameof(ownerMaterial));
            }

            return _coreMeshes.HasValue && _coreMeshes.Value.HasOwnMaterial ? OwnerTint(ownerMaterial) : ownerMaterial.color;
        }

        /// <summary>Moves the stance toward what the standing parts give now, over the settle time, so the body rises on new legs and drops when it loses them.</summary>
        public void UpdateStance(float deltaTime)
        {
            float target = MeasureStance();
            float rate = Mathf.Max(target, Stance) / StanceSettleSeconds;
            Stance = Mathf.MoveTowards(Stance, target, rate * Mathf.Max(deltaTime, 0f));
        }

        /// <summary>Takes the stance the standing parts give at once and lifts the root to it: a body that spawns, the menu preview.</summary>
        public void SnapStance()
        {
            Stance = MeasureStance();
            Root.localPosition = Vector3.up * Stance;
        }

        /// <summary>Counts a part view among those the core stands on; parts in the Locomotion socket join by themselves.</summary>
        internal void AddStandingPart(BodyPartView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            _standing.Add(view);
        }

        /// <summary>The owner color softened for a textured mesh, so the tier and the player still read without flattening the texture.</summary>
        public Color OwnerTint(Material ownerMaterial)
        {
            if (ownerMaterial == null)
            {
                throw new ArgumentNullException(nameof(ownerMaterial));
            }

            return Color.Lerp(Color.white, ownerMaterial.color, _ownerTintBlend);
        }

        /// <summary>Shows or hides the capsule decorations; a bound core mesh hides them for good.</summary>
        public void SetDecorationsVisible(bool visible)
        {
            bool shown = visible && !HasCoreMesh;
            for (int i = 0; i < _decorations.Length; i++)
            {
                _decorations[i].enabled = shown;
            }
        }

        /// <summary>Lays the figure flat for the corpse pose, then lifts it so its lowest visible point rests on the ground.</summary>
        public void LieFlat()
        {
            Root.localRotation = LyingRotation;
            Root.localPosition = Vector3.zero;
            Root.localScale = Vector3.one;
            Renderer[] renderers = Root.GetComponentsInChildren<Renderer>(false);
            float lowest = float.MaxValue;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    lowest = Mathf.Min(lowest, renderers[i].bounds.min.y);
                }
            }

            if (lowest < float.MaxValue)
            {
                Root.position += Vector3.up * (_root.position.y - lowest);
            }
        }

        // The collar that grows a mesh part out of the core, the flesh at the part's root and on the core around the
        // joint (D-097). Skipped when the core or the part cannot be read, or the part does not leave the body.
        private void GrowCollar(BodyPartView view, Material ownerMaterial)
        {
            PartMeshSet? meshes = view.Meshes;
            Mesh? partMesh = view.CurrentMesh;
            if (meshes == null || meshes.Value.Collar <= 0f || partMesh == null || _coreMaterial == null)
            {
                return;
            }

            CoreSurface? surface = SurfaceOfCore(out string surfaceKey);
            if (surface == null)
            {
                return;
            }

            Transform piece = view.transform;
            var partToRig = Matrix4x4.TRS(piece.localPosition, piece.localRotation, piece.localScale);
            Vector3 pivot = piece.localPosition + (piece.localRotation * (meshes.Value.Pivot * meshes.Value.Scale));
            CollarShape? shape = PartCollars.ShapeFor(surface, surfaceKey, partMesh, partToRig, pivot, meshes.Value.Collar);
            if (shape == null)
            {
                return;
            }

            SkinnedMeshRenderer collar = PartCollars.Create(shape, Rig, piece, _coreMaterial, view.gameObject.layer);
            view.AttachCollar(collar, shape);
            Vector3 top = shape.TopCenter;
            view.SetFlesh(_fleshSource != null ? _fleshSource : Texture2D.whiteTexture, FleshTint(ownerMaterial), Matrix4x4.identity, new Vector4(top.x, top.y, top.z, shape.TopRadius), shape.TopRadius * RootFadePerRadius);
            Vector3 joint = shape.BaseCenter;
            _junctions.Add(new Vector4(joint.x, joint.y, joint.z, shape.BaseRadius));
            if (_coreView != null)
            {
                _coreView.SetFleshJunctions(_junctions);
            }
        }

        // The core mesh in rig space: the bound core or the prefab capsule, placed by the body transform.
        private CoreSurface? SurfaceOfCore(out string key)
        {
            key = string.Empty;
            MeshFilter coreFilter = Body.GetComponent<MeshFilter>();
            Mesh? mesh = _coreMeshes.HasValue ? _coreMeshes.Value.Intact : (coreFilter != null ? coreFilter.sharedMesh : null);
            if (mesh == null)
            {
                return null;
            }

            Matrix4x4 coreToRig = CoreParent() * Matrix4x4.TRS(Body.localPosition, Body.localRotation, Body.localScale);
            return PartCollars.SurfaceOf(mesh, coreToRig, out key);
        }

        // From the space of the figure root, where the body hangs, to rig space in body units.
        private Matrix4x4 CoreParent()
        {
            return Matrix4x4.Scale(Vector3.one / _size);
        }

        private void StandOnIfLocomotion(BodyPart part, BodyPartView view)
        {
            if (part.Spec.Socket == SocketKind.Locomotion)
            {
                _standing.Add(view);
            }
        }

        // The lowest point a standing part reaches in the space of the root at rest; the core stands that far up.
        // Rig and placeholder space are children of the root without rotation, so a height maps by position and scale.
        private float MeasureStance()
        {
            float lowest = 0f;
            for (int i = 0; i < _standing.Count; i++)
            {
                BodyPartView view = _standing[i];
                float? point = view != null ? view.RestLowestPoint() : null;
                if (point.HasValue)
                {
                    Transform space = view!.transform.parent;
                    lowest = Mathf.Min(lowest, space.localPosition.y + (space.localScale.y * point.Value));
                }
            }

            return -lowest;
        }

        private static Transform FindOrCreate(Transform parent, string name)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var created = new GameObject(name);
            created.layer = parent.gameObject.layer;
            created.transform.SetParent(parent, false);
            return created.transform;
        }
    }
}
