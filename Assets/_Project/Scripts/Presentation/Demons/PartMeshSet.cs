#nullable enable
using System;
using DemonFighter.Data;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The runtime view of a bound mesh set: the meshes per damage stage with their fallbacks, the imported material,
    /// the optional clip and the fit knobs, read once from the part asset when a view is created.
    /// </summary>
    public readonly struct PartMeshSet
    {
        private readonly Mesh _intact;
        private readonly Mesh? _wounded;
        private readonly Mesh? _mangled;
        private readonly Mesh? _stump;

        public PartMeshSet(Mesh intact, Mesh? wounded, Mesh? mangled, Mesh? stump, Material? material, AnimationClip? clip, float scale, Vector3 offset, Quaternion rotation, Vector3 pivot = default, float collar = 0f, PartPairing pairing = PartPairing.None, Quaternion importRotation = default)
        {
            if (intact == null)
            {
                throw new ArgumentNullException(nameof(intact));
            }

            _intact = intact;
            _wounded = wounded;
            _mangled = mangled;
            _stump = stump;
            Material = material;
            Clip = clip;
            Scale = scale > 0f ? scale : 1f;
            Offset = offset;
            Rotation = rotation;
            Pivot = pivot;
            Collar = Mathf.Max(collar, 0f);
            Pairing = pairing;

            // The model's own up and right in mesh units: the import turn makes up and right of the model as the tool showed it.
            Quaternion import = Quaternion.Dot(importRotation, importRotation) > 0.5f ? importRotation : Quaternion.identity;
            UpAxis = Quaternion.Inverse(import) * Vector3.up;
            SideAxis = Quaternion.Inverse(import) * Vector3.right;
        }

        private PartMeshSet(PartMeshSet source, Mesh intact, Mesh? wounded, Mesh? mangled, Mesh? stump, Vector3 offset, Quaternion rotation, Vector3 pivot, Vector3 upAxis, Vector3 sideAxis)
        {
            _intact = intact;
            _wounded = wounded;
            _mangled = mangled;
            _stump = stump;
            Material = source.Material;
            Clip = source.Clip;
            Scale = source.Scale;
            Offset = offset;
            Rotation = rotation;
            Pivot = pivot;
            Collar = source.Collar;
            Pairing = source.Pairing;
            UpAxis = upAxis;
            SideAxis = sideAxis;
        }

        /// <summary>The mesh of an unhurt part; the one every set has.</summary>
        public Mesh Intact => _intact;

        /// <summary>The imported material, or null when the palette role of the part decides.</summary>
        public Material? Material { get; }

        /// <summary>A hand-made legacy clip that drives the part instead of the procedural motion (D-082), or null.</summary>
        public AnimationClip? Clip { get; }

        /// <summary>True when the part wears its imported material and owner colors become a tint on it.</summary>
        public bool HasOwnMaterial => Material != null;

        /// <summary>True when a lost part leaves a stump on the body instead of vanishing.</summary>
        public bool HasStump => _stump != null;

        /// <summary>Uniform scale fitting the mesh to a one meter body.</summary>
        public float Scale { get; }

        /// <summary>Shift from the socket anchor in body units.</summary>
        public Vector3 Offset { get; }

        /// <summary>Extra rotation after the anchor.</summary>
        public Quaternion Rotation { get; }

        /// <summary>The point of the mesh, in its own units, that sits on the socket anchor (D-088).</summary>
        public Vector3 Pivot { get; }

        /// <summary>Size of the flesh collar around the joint (D-097); 0 for none.</summary>
        public float Collar { get; }

        /// <summary>Whether the part is drawn as a left and a right copy that move on their own (D-099).</summary>
        public PartPairing Pairing { get; }

        /// <summary>Up of the model as the tool showed it, in mesh units; legs stand along it.</summary>
        public Vector3 UpAxis { get; }

        /// <summary>Right of the model as the tool showed it, in mesh units; a pair model is cut across it.</summary>
        public Vector3 SideAxis { get; }

        /// <summary>
        /// The set for a copy on the left flank (D-088): every mesh mirrored across its own X axis and the fit mirrored
        /// with it, so the left arm is the mirror image of the right one in shape and in pose.
        /// </summary>
        public PartMeshSet Mirrored()
        {
            return new PartMeshSet(
                this,
                MirroredMeshes.For(_intact),
                _wounded != null ? MirroredMeshes.For(_wounded) : null,
                _mangled != null ? MirroredMeshes.For(_mangled) : null,
                _stump != null ? MirroredMeshes.For(_stump) : null,
                SocketAnchors.MirrorOffset(Offset),
                SocketAnchors.MirrorRotation(Rotation),
                new Vector3(-Pivot.x, Pivot.y, Pivot.z),
                new Vector3(-UpAxis.x, UpAxis.y, UpAxis.z),
                new Vector3(-SideAxis.x, SideAxis.y, SideAxis.z));
        }

        /// <summary>
        /// The right half of a model that holds a pair (D-099): every mesh cut through the pivot across the model's
        /// right, the fit unchanged, so the half sits exactly where it sat in the pair; its mirror image is the other.
        /// </summary>
        public PartMeshSet Half()
        {
            return new PartMeshSet(
                this,
                HalfMeshes.For(_intact, Pivot, SideAxis),
                _wounded != null ? HalfMeshes.For(_wounded, Pivot, SideAxis) : null,
                _mangled != null ? HalfMeshes.For(_mangled, Pivot, SideAxis) : null,
                _stump != null ? HalfMeshes.For(_stump, Pivot, SideAxis) : null,
                Offset,
                Rotation,
                Pivot,
                UpAxis,
                SideAxis);
        }

        /// <summary>
        /// The mesh for a stage: a missing wounded mesh falls back to the intact one, a missing mangled one to the
        /// wounded one, and a lost part shows its stump or nothing.
        /// </summary>
        public Mesh? MeshFor(DamageStage stage)
        {
            switch (stage)
            {
                case DamageStage.Lost:
                    return _stump != null ? _stump : null;
                case DamageStage.Mangled:
                    return Or(_mangled, Or(_wounded, _intact));
                case DamageStage.Wounded:
                    return Or(_wounded, _intact);
                default:
                    return _intact;
            }
        }

        /// <summary>Reads the set of a part asset; false while no model is bound.</summary>
        public static bool TryFrom(PartMeshSetDefinition? definition, out PartMeshSet set)
        {
            if (definition == null || !definition.HasMeshes)
            {
                set = default;
                return false;
            }

            // The knobs apply on top of the turn and unit scale the model file carries (D-088).
            Quaternion rotation = Quaternion.Euler(definition.Euler) * definition.ImportRotation;
            set = new PartMeshSet(definition.Intact!, definition.Wounded, definition.Mangled, definition.Stump, definition.Material, definition.Clip, definition.Scale * definition.ImportScale, definition.Offset, rotation, definition.ImportPivot, definition.Collar, definition.Pairing, definition.ImportRotation);
            return true;
        }

        // Unity's destroyed and unassigned objects are not C# null, so the fallback chain compares the Unity way.
        private static Mesh Or(Mesh? first, Mesh second)
        {
            return first != null ? first : second;
        }
    }
}
