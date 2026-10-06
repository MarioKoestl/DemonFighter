#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// The real meshes of a body part (ASSET_PIPELINE, "Modular body parts"), nested in the visual block of a part
    /// asset: one mesh per damage state, the material they were imported with, and the knobs that fit a generated
    /// mesh to the body without re-exporting it. Empty until the art binder finds a model for the part; the view
    /// draws the placeholder primitive until then. Scale, offset and rotation are Inspector tuning on top of the
    /// import fix, the turn and unit scale the model file carries on its own object, which the binder reads on every
    /// bind (D-088). The binder sets the knobs to neutral on a first bind and never overwrites them afterwards.
    /// </summary>
    [Serializable]
    public sealed class PartMeshSetDefinition
    {
        [SerializeField] private Mesh? _intact;
        [SerializeField] private Mesh? _wounded;
        [SerializeField] private Mesh? _mangled;
        [SerializeField] private Mesh? _stump;
        [SerializeField] private Material? _material;
        [SerializeField] private AnimationClip? _clip;
        [SerializeField] private float _scale = 1f;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private Vector3 _euler;
        [SerializeField, Range(0f, 2f)] private float _collar = 1f;
        [SerializeField] private PartPairing _pairing = PartPairing.None;

        // The import fix (D-088), read from the file on every bind; not tuning, so not in the Inspector. The turn and
        // unit scale of the model file's own object (a Meshy or Blender export stores its upright turn and centimeter
        // scale there), the size that makes the model's longest side one unit, and the point of the raw mesh at the
        // bottom center of the model as it stood in the tool, where the part meets the body.
        [SerializeField, HideInInspector] private float _importScale = 1f;
        [SerializeField, HideInInspector] private Quaternion _importRotation = Quaternion.identity;
        [SerializeField, HideInInspector] private Vector3 _importPivot;
        [SerializeField, HideInInspector] private int _importVersion;

        /// <summary>True once a model is bound; the intact mesh is the one state every part must have.</summary>
        public bool HasMeshes => _intact != null;

        public Mesh? Intact => _intact;

        public Mesh? Wounded => _wounded;

        public Mesh? Mangled => _mangled;

        /// <summary>The body-side remainder shown after the part is lost; null for parts that vanish.</summary>
        public Mesh? Stump => _stump;

        /// <summary>The imported material; null means the palette role of the part decides, as for a primitive.</summary>
        public Material? Material => _material;

        /// <summary>A hand-made legacy clip the part plays in a loop instead of its procedural motion (D-082); null for most parts.</summary>
        public AnimationClip? Clip => _clip;

        /// <summary>
        /// The length of the part's longest side in body heights: the import fix makes every model one unit long on its
        /// longest side, so 0.55 makes an arm 0.55 body heights long whatever size the tool gave it. The view multiplies
        /// by the body size; the core is sized to the body anyway.
        /// </summary>
        public float Scale => _scale;

        /// <summary>
        /// Shift from the socket anchor in body units (1 is the body height), turned with the anchor: +Z pushes the part
        /// out of the body, +Y moves it up (D-088). Eyes sit above the jaws this way. The core has no anchor; its
        /// offset is in body space.
        /// </summary>
        public Vector3 Offset => _offset;

        /// <summary>Extra rotation in degrees on top of the file's own turn; 0 shows the model upright as the tool that made it showed it.</summary>
        public Vector3 Euler => _euler;

        /// <summary>
        /// Size of the flesh collar that grows the part out of the body (D-097): 1 is the default sleeve where the part
        /// leaves the blob, 2 a thick one, 0 leaves the joint bare, as for a hide lying on the body.
        /// </summary>
        public float Collar => _collar;

        /// <summary>
        /// Whether the part is drawn as a left and a right copy that move on their own (D-099): Split for a model holding
        /// both legs, Mirror for a model holding one, None for everything else.
        /// </summary>
        public PartPairing Pairing => _pairing;

        /// <summary>The file's unit scale times the size that makes the model's longest side one unit; the Scale knob multiplies it.</summary>
        public float ImportScale => _importScale > 0f ? _importScale : 1f;

        /// <summary>The point of the raw mesh that sits on the socket: the bottom center of the model as it stood in the tool.</summary>
        public Vector3 ImportPivot => _importPivot;

        /// <summary>The turn the model file carries on its object, such as Blender's upright turn; the Euler knob turns on top of it.</summary>
        public Quaternion ImportRotation => Quaternion.Dot(_importRotation, _importRotation) > 0.5f ? _importRotation : Quaternion.identity;

        /// <summary>False for a set bound before the binder read the file's own turn and scale (D-088).</summary>
        internal bool HasImportFix => _importVersion > 0;

        /// <summary>Points the set at the meshes a model delivered; the binder calls it on every bind.</summary>
        internal void SetMeshes(Mesh? intact, Mesh? wounded, Mesh? mangled, Mesh? stump, Material? material)
        {
            _intact = intact;
            _wounded = wounded;
            _mangled = mangled;
            _stump = stump;
            _material = material;
        }

        /// <summary>Stores the import fix of the model; the binder calls it on every bind.</summary>
        internal void SetImportFix(float scale, Quaternion rotation, Vector3 pivot)
        {
            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "An import scale must be positive.");
            }

            _importScale = scale;
            _importRotation = rotation;
            _importPivot = pivot;
            _importVersion = 2;
        }

        /// <summary>Sets the fit knobs; the generator seeds them once and the binder only on a first bind.</summary>
        internal void SetPairing(PartPairing pairing)
        {
            _pairing = pairing;
        }

        internal void SetCollar(float collar)
        {
            _collar = Mathf.Clamp(collar, 0f, 2f);
        }

        internal void SetFit(float scale, Vector3 offset, Vector3 euler)
        {
            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "A mesh scale must be positive.");
            }

            _scale = scale;
            _offset = offset;
            _euler = euler;
        }
    }
}
