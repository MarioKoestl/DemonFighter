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

        public PartMeshSet(Mesh intact, Mesh? wounded, Mesh? mangled, Mesh? stump, Material? material, AnimationClip? clip, float scale, Vector3 offset, Quaternion rotation, Vector3 pivot = default)
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

        /// <summary>
        /// The set for a copy on the left flank (D-088): every mesh mirrored across its own X axis and the fit mirrored
        /// with it, so the left arm is the mirror image of the right one in shape and in pose.
        /// </summary>
        public PartMeshSet Mirrored()
        {
            return new PartMeshSet(
                MirroredMeshes.For(_intact),
                _wounded != null ? MirroredMeshes.For(_wounded) : null,
                _mangled != null ? MirroredMeshes.For(_mangled) : null,
                _stump != null ? MirroredMeshes.For(_stump) : null,
                Material,
                Clip,
                Scale,
                SocketAnchors.MirrorOffset(Offset),
                SocketAnchors.MirrorRotation(Rotation),
                new Vector3(-Pivot.x, Pivot.y, Pivot.z));
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
            set = new PartMeshSet(definition.Intact!, definition.Wounded, definition.Mangled, definition.Stump, definition.Material, definition.Clip, definition.Scale * definition.ImportScale, definition.Offset, rotation, definition.ImportPivot);
            return true;
        }

        // Unity's destroyed and unassigned objects are not C# null, so the fallback chain compares the Unity way.
        private static Mesh Or(Mesh? first, Mesh second)
        {
            return first != null ? first : second;
        }
    }
}
