#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Data;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// One body part with a trigger collider on the Demon layer, so the aim rays of skills find the part under the
    /// crosshair (ARCHITECTURE, "Movement, collision and hits"). Shows the damage stage of the part (GAME_DESIGN,
    /// "Visible damage and gore"): a primitive darkens and shrinks, a bound mesh set swaps to its wounded, mangled
    /// or stump mesh (stage 2, D-080). Blood soaks the part where it was hit and dries again (D-068, D-081) through
    /// the skin shader. The procedural motion of the body turns the part around its rest pose (D-082). On death the
    /// part freezes as dead flesh on the Food layer, visible and aimable whatever its stage says.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public sealed class BodyPartView : MonoBehaviour
    {
        private const float WoundedScale = 0.8f;
        private const float MangledScale = 0.65f;
        private const float WoundedDarkening = 0.5f;
        private const float MangledDarkening = 0.35f;
        private const float MeshWoundedDarkening = 0.75f;
        private const float MeshMangledDarkening = 0.55f;
        private const float MangledBloodBlend = 0.45f;
        private const float HighlightBlend = 0.55f;
        private const float WoundRadiusPerExtent = 0.7f;
        private const float CharBlend = 0.8f;
        private const int MaxFleshJunctions = 8;
        private const float SkinBoundsGrowth = 0.5f;
        private const float StumpBloodPerRadius = 2.5f;
        private const float JunctionFade = 0.05f;
        private static readonly Color MangledBlood = new Color(0.45f, 0.04f, 0.04f);
        private static readonly Color CharColor = new Color(0.07f, 0.03f, 0.02f);
        private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.35f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BloodAmountId = Shader.PropertyToID("_BloodAmount");
        private static readonly int WoundCenterId = Shader.PropertyToID("_WoundCenter");
        private static readonly int WoundRadiusId = Shader.PropertyToID("_WoundRadius");
        private static readonly int FleshSourceId = Shader.PropertyToID("_FleshSource");
        private static readonly int FleshTintId = Shader.PropertyToID("_FleshTint");
        private static readonly int FleshFromObjectId = Shader.PropertyToID("_FleshFromObject");
        private static readonly int FleshAllId = Shader.PropertyToID("_FleshAll");
        private static readonly int FleshRootId = Shader.PropertyToID("_FleshRoot");
        private static readonly int FleshRootFadeId = Shader.PropertyToID("_FleshRootFade");
        private static readonly int FleshJunctionsId = Shader.PropertyToID("_FleshJunctions");
        private static readonly int FleshJunctionCountId = Shader.PropertyToID("_FleshJunctionCount");
        private static readonly int FleshJunctionFadeId = Shader.PropertyToID("_FleshJunctionFade");

        private Renderer _renderer = null!;
        private Collider _collider = null!;
        private MeshFilter? _filter;
        private BoxCollider? _box;
        private MaterialPropertyBlock _block = null!;
        private PartMeshSet? _meshes;
        private Color? _ownerTint;
        private Color? _corpseTint;
        private Vector3 _baseScale = Vector3.one;
        private Quaternion _restRotation = Quaternion.identity;
        private Quaternion _animationRotation = Quaternion.identity;
        private Vector3 _woundCenter;
        private float _woundRadius;
        private float _blood;
        private float _burn;
        private float _conditionScale = 1f;
        private float _upgradeScale = 1f;
        private bool _visible = true;
        private bool _drawn = true;
        private bool _lost;
        private bool _corpse;
        private bool _highlighted;
        private bool _hasShown;
        private DamageStage _shown;
        private Mesh? _measuredMesh;
        private float _measuredLowest;
        private bool _hasFlesh;
        private Texture? _fleshSource;
        private Color _fleshTint = Color.white;
        private Matrix4x4 _fleshParent = Matrix4x4.identity;
        private Vector4 _fleshRoot;
        private float _fleshRootFade;
        private Vector4[]? _fleshJunctions;
        private int _fleshJunctionCount;
        private SkinnedMeshRenderer? _collar;
        private SkinnedMeshRenderer? _skin;
        private PartSkeleton? _skeleton;
        private Transform[]? _bones;
        private IChainMotion? _chain;
        private Mesh? _sourceMesh;
        private MaterialPropertyBlock? _collarBlock;
        private CollarShape? _collarShape;

        /// <summary>The body this part belongs to; null before the owner binds, and on a menu preview figure.</summary>
        public DemonView? Owner { get; private set; }

        /// <summary>Index of the part in the simulation body.</summary>
        public int PartIndex { get; private set; }

        /// <summary>The stage the part shows right now.</summary>
        public DamageStage Stage => _shown;

        /// <summary>The mesh the part shows now, before any skinning; null while it shows nothing of its own.</summary>
        internal Mesh? CurrentMesh => _sourceMesh != null ? _sourceMesh : (_filter != null ? _filter.sharedMesh : null);

        /// <summary>True when generated bones move the part (D-098).</summary>
        internal bool HasChain => _chain != null;

        /// <summary>True when the bones pose the whole part, as a leg does, so the body animator leaves the part unturned.</summary>
        internal bool ChainOwnsPose => _chain != null && _chain.OwnsPose;

        /// <summary>The bound mesh set as placed, mirrored for a left copy; null for a primitive.</summary>
        internal PartMeshSet? Meshes => _meshes;

        /// <summary>True when the part is drawn with a bound mesh set rather than a primitive.</summary>
        public bool HasMeshes => _meshes.HasValue;

        /// <summary>True when the part wears an imported material, so owner and corpse colors are tints on it.</summary>
        public bool HasOwnMaterial => _meshes.HasValue && _meshes.Value.HasOwnMaterial;

        /// <summary>How soaked the part is, 0 to 1; the skin shader paints it.</summary>
        public float Blood => _blood;

        /// <summary>How charred the part is from fire, 0 to 1 (D-086).</summary>
        public float Burn => _burn;

        /// <summary>How the body animator moves this part (D-082).</summary>
        public PartMotion Motion { get; private set; }

        /// <summary>Which copy of its kind this part is on the body; the second arm swings against the first.</summary>
        public int CopyIndex { get; private set; }

        /// <summary>True when a hand-made clip drives the part, so the animator leaves it alone.</summary>
        public bool HasClip { get; private set; }

        /// <summary>Applies the look of a stage; cheap to call every frame, it only acts on a change. Frozen on a corpse.</summary>
        public void ShowDamage(DamageStage stage)
        {
            if (_corpse || (_hasShown && stage == _shown))
            {
                return;
            }

            _hasShown = true;
            _shown = stage;
            _lost = stage == DamageStage.Lost;
            if (_meshes.HasValue)
            {
                ApplyMesh(stage);
                _conditionScale = 1f;
            }
            else
            {
                _drawn = !_lost;
                _conditionScale = stage == DamageStage.Wounded ? WoundedScale : stage == DamageStage.Mangled ? MangledScale : 1f;
            }

            ApplyTint();
            _collider.enabled = !_lost;
            ApplyVisibility();
            ApplyPose();
        }

        /// <summary>Turns the part around its rest pose; the animator calls it every frame with the motion of the moment.</summary>
        public void SetAnimation(Quaternion rotation)
        {
            if (_corpse)
            {
                return;
            }

            _animationRotation = rotation;
            ApplyPose();
        }

        /// <summary>Soaks the part by an amount (1 is drenched) around a world point, where the wound now sits.</summary>
        public void AddBlood(float amount, Vector3 worldPoint)
        {
            _woundCenter = transform.InverseTransformPoint(worldPoint);
            _woundRadius = _renderer.localBounds.extents.magnitude * WoundRadiusPerExtent;
            AddBlood(amount);
        }

        /// <summary>Soaks the part by an amount around the wound it already has; a bleeding part seeps this way.</summary>
        public void AddBlood(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            if (_woundRadius <= 0f)
            {
                _woundCenter = _renderer.localBounds.center;
                _woundRadius = _renderer.localBounds.extents.magnitude * WoundRadiusPerExtent;
            }

            _blood = Mathf.Min(1f, _blood + amount);
            ApplyTint();
        }

        /// <summary>Chars the part by an amount (1 is black); lava and fissures call it while the part burns.</summary>
        public void AddBurn(float amount)
        {
            if (amount <= 0f || _corpse)
            {
                return;
            }

            _burn = Mathf.Min(1f, _burn + amount);
            ApplyTint();
        }

        /// <summary>Lets the char fade over the given seconds once the fire is out; call once per frame. A corpse stays charred.</summary>
        public void Cool(float deltaTime, float coolSeconds)
        {
            if (_corpse || _burn <= 0f)
            {
                return;
            }

            _burn = Mathf.Max(0f, _burn - deltaTime / Mathf.Max(coolSeconds, 0.0001f));
            ApplyTint();
        }

        /// <summary>Dries the blood over the given seconds from drenched to clean; call once per frame. A corpse keeps its blood.</summary>
        public void Dry(float deltaTime, float drySeconds)
        {
            if (_corpse || _blood <= 0f)
            {
                return;
            }

            _blood = Mathf.Max(0f, _blood - deltaTime / Mathf.Max(drySeconds, 0.0001f));
            ApplyTint();
        }

        /// <summary>
        /// A loose copy of this part for the ground: the mesh it shows (a bound set gives its most damaged whole
        /// mesh), its material and tint, its world pose and scale, with a solid box collider. The caller adds
        /// physics and the food binding; the copy is on no layer of ours yet.
        /// </summary>
        public GameObject CreateDetachedCopy(Transform parent)
        {
            var copy = new GameObject(name + " (severed)");
            copy.transform.SetParent(parent, false);
            copy.transform.SetPositionAndRotation(transform.position, transform.rotation);
            copy.transform.localScale = transform.lossyScale;
            Mesh? mesh = _meshes.HasValue ? _meshes.Value.MeshFor(DamageStage.Mangled) : (_filter != null ? _filter.sharedMesh : GetComponent<MeshFilter>().sharedMesh);
            copy.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _renderer.sharedMaterial;
            _renderer.GetPropertyBlock(_block);
            renderer.SetPropertyBlock(_block);
            BoxCollider box = copy.AddComponent<BoxCollider>();
            if (mesh != null)
            {
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
            }

            return copy;
        }

        /// <summary>Marks the part the crosshair of the player rests on with a gold tint (D-065); the tint lifts when the aim moves on.</summary>
        internal void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted)
            {
                return;
            }

            _highlighted = highlighted;
            ApplyTint();
        }

        /// <summary>Hands the view its meshes before it is initialized; the stage then picks among them.</summary>
        internal void SetMeshes(PartMeshSet meshes)
        {
            _meshes = meshes;
            _filter = GetComponent<MeshFilter>();
            _box = _collider as BoxCollider;
        }

        /// <summary>
        /// Moves the part with generated bones (D-098): the bones are created under it, the meshes it shows are skinned
        /// to them, and the motion of its kind poses them every frame. Call after <see cref="SetMeshes"/>.
        /// </summary>
        internal void SetSkeleton(PartSkeleton skeleton, PartMotion motion, LegGait? gait = null, int side = 0)
        {
            _skeleton = skeleton ?? throw new ArgumentNullException(nameof(skeleton));
            _skin = GetComponent<SkinnedMeshRenderer>();
            if (_skin == null)
            {
                throw new InvalidOperationException("A part with bones needs a SkinnedMeshRenderer.");
            }

            _bones = skeleton.CreateBones(transform);
            _skin.bones = _bones;
            _skin.rootBone = transform;
            _skin.quality = SkinQuality.Bone2;
            _chain = ChainBones.MotionFor(motion, transform, _bones, skeleton, gait, side);
        }

        /// <summary>Poses the bones for this frame; a corpse, a lost part and a rigid one stay as they are.</summary>
        internal void UpdateChain(in ChainContext context)
        {
            if (_chain != null && !_corpse && _drawn)
            {
                _chain.Update(context);
            }
        }

        /// <summary>Tells the view what it is for the animator: its motion, its copy, whether a clip drives it instead.</summary>
        internal void SetMotion(PartMotion motion, int copyIndex, bool hasClip)
        {
            Motion = motion;
            CopyIndex = copyIndex;
            HasClip = hasClip;
        }

        /// <summary>
        /// Takes the part over: the material it wears, the owner tint for a mesh with its own material (null leaves
        /// the texture alone), the Demon layer, and the intact look.
        /// </summary>
        internal void Initialize(DemonView? owner, int partIndex, Material material, Color? ownerTint)
        {
            Owner = owner;
            PartIndex = partIndex;
            _renderer.sharedMaterial = material;
            _ownerTint = ownerTint;
            gameObject.layer = Layers.Demon;
            _baseScale = transform.localScale;
            _restRotation = transform.localRotation;
            _animationRotation = Quaternion.identity;
            _corpse = false;
            _corpseTint = null;
            _blood = 0f;
            _burn = 0f;
            _woundRadius = 0f;
            _hasShown = false;
            _measuredMesh = null;
            ShowDamage(DamageStage.Intact);
        }

        /// <summary>Called after the owner posed the body, so the healthy scale and the rest rotation are the posed ones.</summary>
        internal void RememberBasePose()
        {
            _baseScale = transform.localScale;
            _restRotation = transform.localRotation;
            _animationRotation = Quaternion.identity;
            _measuredMesh = null;
            ApplyPose();
            if (_hasFlesh)
            {
                ApplyTint();
            }
        }

        /// <summary>
        /// The lowest point of what the part draws, in its rest pose (no swing, no wound shrink) and in its parent's
        /// space; null when it draws nothing or is lost. The figure stands the core on it (D-094). Measured once per mesh.
        /// </summary>
        internal float? RestLowestPoint()
        {
            if (_filter == null)
            {
                _filter = GetComponent<MeshFilter>();
            }

            Mesh? mesh = CurrentMesh;
            if (_lost || !_drawn || mesh == null)
            {
                return null;
            }

            if (mesh != _measuredMesh)
            {
                _measuredLowest = MeshBounds.LowestY(mesh, Matrix4x4.TRS(Vector3.zero, _restRotation, _baseScale * _upgradeScale));
                _measuredMesh = mesh;
            }

            return transform.localPosition.y + _measuredLowest;
        }

        /// <summary>
        /// Grows the part by its upgrade level (D-096), around its pivot where it meets the body, so an upgraded part
        /// is visibly bigger without a model of its own. Cheap to call every frame; it only acts on a change.
        /// </summary>
        internal void ShowUpgrade(int level, float growthPerLevel)
        {
            float scale = 1f + (Mathf.Max(level, 0) * Mathf.Max(growthPerLevel, 0f));
            if (Mathf.Approximately(scale, _upgradeScale))
            {
                return;
            }

            _upgradeScale = scale;
            _measuredMesh = null;
            ApplyPose();
            if (_hasFlesh)
            {
                ApplyTint();
            }
        }

        /// <summary>
        /// Gives the part junction flesh (D-097): the texture whose average colors it, the tint on top, the matrix from
        /// its parent's space to body space, and the root of the part in body space (xyz centre, w radius) that wears the
        /// flesh, fading out over the given distance. A zero root leaves only the junctions, as on the core.
        /// </summary>
        internal void SetFlesh(Texture source, Color tint, Matrix4x4 parentToBody, Vector4 root, float rootFade)
        {
            _hasFlesh = true;
            _fleshSource = source != null ? source : Texture2D.whiteTexture;
            _fleshTint = tint;
            _fleshParent = parentToBody;
            _fleshRoot = root;
            _fleshRootFade = rootFade;
            ApplyTint();
        }

        /// <summary>A new matrix from the parent's space to body space, as when the body grows; the core needs it.</summary>
        internal void SetFleshParent(Matrix4x4 parentToBody)
        {
            _fleshParent = parentToBody;
            if (_hasFlesh)
            {
                ApplyTint();
            }
        }

        /// <summary>A new tint for the junction flesh and the collar, as when the tier color changes or the body dies.</summary>
        internal void SetFleshTint(Color tint)
        {
            _fleshTint = tint;
            if (_hasFlesh)
            {
                ApplyTint();
            }
        }

        /// <summary>The joints on this part where others leave it, in body space (xyz centre, w radius); the core wears flesh around them.</summary>
        internal void SetFleshJunctions(IReadOnlyList<Vector4> junctions)
        {
            if (junctions == null)
            {
                throw new ArgumentNullException(nameof(junctions));
            }

            _fleshJunctions ??= new Vector4[MaxFleshJunctions];
            _fleshJunctionCount = Mathf.Min(junctions.Count, MaxFleshJunctions);
            for (int i = 0; i < MaxFleshJunctions; i++)
            {
                _fleshJunctions[i] = i < _fleshJunctionCount ? junctions[i] : Vector4.zero;
            }

            if (_hasFlesh)
            {
                ApplyTint();
            }
        }

        /// <summary>Takes over the collar that grows this part out of the body; it shows with the part and stays as a stump when the part is lost.</summary>
        internal void AttachCollar(SkinnedMeshRenderer collar, CollarShape shape)
        {
            _collar = collar != null ? collar : throw new ArgumentNullException(nameof(collar));
            _collarShape = shape ?? throw new ArgumentNullException(nameof(shape));
            _collarBlock = new MaterialPropertyBlock();
            ApplyTint();
            ApplyVisibility();
        }

        /// <summary>Hides the part for the first-person camera; a lost part stays hidden either way.</summary>
        internal void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyVisibility();
        }

        /// <summary>
        /// Freezes the part as dead flesh: the corpse material (or its color as a tint on an imported material), the
        /// Food layer, visible with its collider on, so the corpse can be seen and the eat aim can find it even
        /// though the destroyed core counts as lost. The blood it wore stays; the motion stops.
        /// </summary>
        internal void ShowAsCorpse(Material material)
        {
            _corpse = true;
            _lost = false;
            _drawn = true;
            _conditionScale = 1f;
            _animationRotation = Quaternion.identity;
            if (_meshes.HasValue)
            {
                // A lost core has no mesh of its own; the corpse shows the most damaged body mesh there is.
                Mesh? mesh = _meshes.Value.MeshFor(DamageStage.Mangled);
                if (mesh != null)
                {
                    ShowMesh(mesh);
                }
            }

            if (HasOwnMaterial)
            {
                _corpseTint = material.color;
            }
            else
            {
                _renderer.sharedMaterial = material;
                _corpseTint = null;
            }

            // A body that burned to death stays charred.
            if (_burn > 0f)
            {
                _corpseTint = Color.Lerp(_corpseTint ?? material.color, CharColor, _burn * CharBlend);
            }

            ApplyTint();
            gameObject.layer = Layers.Food;
            _collider.enabled = true;
            ApplyVisibility();
            ApplyPose();
        }

        /// <summary>
        /// Recolors for a new owner color, as when a demon grows a tier: the palette material is swapped, an imported
        /// material gets the new tint. The look of the current stage stays.
        /// </summary>
        internal void ApplyOwner(Material ownerMaterial, Color ownerTint)
        {
            if (_corpse)
            {
                return;
            }

            if (HasOwnMaterial)
            {
                _ownerTint = ownerTint;
            }
            else
            {
                _renderer.sharedMaterial = ownerMaterial;
            }

            _hasShown = false;
            ShowDamage(_shown);
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _block = new MaterialPropertyBlock();
        }

        // The mesh of the stage; a lost part without a stump draws nothing. The box trigger follows the mesh.
        private void ApplyMesh(DamageStage stage)
        {
            Mesh? mesh = _meshes!.Value.MeshFor(stage);
            _drawn = mesh != null;
            if (mesh != null)
            {
                ShowMesh(mesh);
            }
        }

        private void ShowMesh(Mesh mesh)
        {
            _sourceMesh = mesh;
            if (_skin != null && _skeleton != null)
            {
                Mesh? skinned = _skeleton.SkinnedCopyOf(mesh);
                _skin.sharedMesh = skinned != null ? skinned : mesh;
                Bounds bounds = mesh.bounds;
                bounds.Expand(bounds.size.magnitude * SkinBoundsGrowth);
                _skin.localBounds = bounds;
            }
            else if (_filter != null)
            {
                _filter.sharedMesh = mesh;
            }

            if (_box != null)
            {
                Bounds bounds = mesh.bounds;
                _box.center = bounds.center;
                _box.size = bounds.size;
            }
        }

        // Wounded parts darken, mangled ones darken further and redden, the aimed part glows; a corpse keeps its dead
        // flesh color whatever the aim does. The blood goes to the skin shader as a per-renderer value. Untouched
        // textures without blood get no block at all.
        private void ApplyTint()
        {
            _block.Clear();
            Color? tint = _corpse ? _corpseTint : LiveTint();
            if (tint.HasValue)
            {
                Color color = tint.Value;
                color.a = 1f;
                _block.SetColor(BaseColorId, color);
            }

            if (_blood > 0f)
            {
                _block.SetFloat(BloodAmountId, _blood);
                _block.SetVector(WoundCenterId, _woundCenter);
                _block.SetFloat(WoundRadiusId, _woundRadius);
            }

            if (_hasFlesh)
            {
                WriteFlesh(_block);
            }

            _renderer.SetPropertyBlock(_block);
            ApplyCollarTint();
        }

        private void WriteFlesh(MaterialPropertyBlock block)
        {
            Matrix4x4 rest = Matrix4x4.TRS(transform.localPosition, _restRotation, _baseScale * _upgradeScale);
            block.SetTexture(FleshSourceId, _fleshSource != null ? _fleshSource : Texture2D.whiteTexture);
            block.SetColor(FleshTintId, _fleshTint);
            block.SetMatrix(FleshFromObjectId, _fleshParent * rest);
            block.SetVector(FleshRootId, _fleshRoot);
            block.SetFloat(FleshRootFadeId, _fleshRootFade);
            if (_fleshJunctions != null)
            {
                block.SetVectorArray(FleshJunctionsId, _fleshJunctions);
                block.SetFloat(FleshJunctionCountId, _fleshJunctionCount);
                block.SetFloat(FleshJunctionFadeId, JunctionFade);
            }
        }

        // The collar is junction flesh all over, in body space; once the part is gone its cap is a bleeding stump.
        private void ApplyCollarTint()
        {
            if (_collar == null || _collarBlock == null || _collarShape == null)
            {
                return;
            }

            _collarBlock.Clear();
            _collarBlock.SetTexture(FleshSourceId, _fleshSource != null ? _fleshSource : Texture2D.whiteTexture);
            _collarBlock.SetColor(FleshTintId, _fleshTint);
            _collarBlock.SetMatrix(FleshFromObjectId, Matrix4x4.identity);
            _collarBlock.SetFloat(FleshAllId, 1f);
            if (_lost)
            {
                _collarBlock.SetFloat(BloodAmountId, 1f);
                _collarBlock.SetVector(WoundCenterId, _collarShape.TopCenter);
                _collarBlock.SetFloat(WoundRadiusId, _collarShape.TopRadius * StumpBloodPerRadius);
            }

            _collar.SetPropertyBlock(_collarBlock);
        }

        private Color? LiveTint()
        {
            bool damaged = _shown == DamageStage.Wounded || _shown == DamageStage.Mangled;
            if (!_ownerTint.HasValue && !damaged && !_highlighted && _burn <= 0f)
            {
                return null;
            }

            Color color = _ownerTint ?? _renderer.sharedMaterial.color;
            if (_shown == DamageStage.Wounded)
            {
                color *= _meshes.HasValue ? MeshWoundedDarkening : WoundedDarkening;
            }
            else if (_shown == DamageStage.Mangled)
            {
                color *= _meshes.HasValue ? MeshMangledDarkening : MangledDarkening;
                color = Color.Lerp(color, MangledBlood, MangledBloodBlend);
            }

            if (_burn > 0f)
            {
                color = Color.Lerp(color, CharColor, _burn * CharBlend);
            }

            if (_highlighted)
            {
                color = Color.Lerp(color, HighlightColor, HighlightBlend);
            }

            return color;
        }

        private void ApplyVisibility()
        {
            _renderer.enabled = _visible && _drawn;
            if (_collar != null)
            {
                _collar.enabled = _visible;
            }
        }

        // The motion turns the part about the axes of the body at the point where the part meets it, whichever way
        // its model had to be turned to fit; turned about the model's own axes, a swing went wherever the import left them.
        private void ApplyPose()
        {
            transform.localScale = _baseScale * (_conditionScale * _upgradeScale);
            transform.localRotation = _animationRotation * _restRotation;
        }
    }
}
