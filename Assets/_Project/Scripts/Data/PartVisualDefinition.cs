#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// How a body part is drawn, nested in a body part asset. In the placeholder stage a primitive with a palette
    /// material sits on the body capsule where the local values say, in capsule mesh units (two tall, radius a
    /// half); the second copy of a part can be mirrored, so two arms hang on both sides. Once the art binder finds
    /// a model for the part, the mesh set replaces the primitive and the part hangs on a socket anchor of the core
    /// instead (ASSET_PIPELINE, "Modular body parts"). Only the core carries anchors.
    /// </summary>
    [Serializable]
    public sealed class PartVisualDefinition
    {
        [SerializeField] private PartVisualKind _kind = PartVisualKind.Capsule;
        [SerializeField] private PartMaterialRole _material = PartMaterialRole.Owner;
        [SerializeField] private Vector3 _localPosition = new Vector3(0.75f, 0f, 0f);
        [SerializeField] private Vector3 _localScale = new Vector3(0.3f, 0.5f, 0.3f);
        [SerializeField] private Vector3 _localEuler;
        [SerializeField] private bool _mirrorSecondCopy;
        [SerializeField] private PartMotion _motion = PartMotion.None;
        [SerializeField] private PartMeshSetDefinition _meshes = new PartMeshSetDefinition();
        [SerializeField] private SocketAnchorDefinition[] _anchors = Array.Empty<SocketAnchorDefinition>();

        public PartVisualKind Kind => _kind;

        /// <summary>The palette material of the primitive; also the fallback of a mesh set without a material of its own.</summary>
        public PartMaterialRole Material => _material;

        /// <summary>Center of the primitive in capsule mesh units, X right, Y up, Z forward.</summary>
        public Vector3 LocalPosition => _localPosition;

        public Vector3 LocalScale => _localScale;

        public Vector3 LocalEuler => _localEuler;

        /// <summary>True when the second copy of the part mirrors across the X axis.</summary>
        public bool MirrorSecondCopy => _mirrorSecondCopy;

        /// <summary>How the view moves the part procedurally (D-082); a clip on the mesh set wins over it.</summary>
        public PartMotion Motion => _motion;

        /// <summary>The real meshes once a model is bound (M5); empty in the placeholder stage.</summary>
        public PartMeshSetDefinition Meshes => _meshes;

        /// <summary>Where other parts plug into this one, in body units; empty for everything but the core.</summary>
        public IReadOnlyList<SocketAnchorDefinition> Anchors => _anchors;

        internal void Configure(PartVisualKind kind, PartMaterialRole material, Vector3 position, Vector3 scale, Vector3 euler, bool mirrorSecondCopy)
        {
            _kind = kind;
            _material = material;
            _localPosition = position;
            _localScale = scale;
            _localEuler = euler;
            _mirrorSecondCopy = mirrorSecondCopy;
        }

        internal void SetMotion(PartMotion motion)
        {
            _motion = motion;
        }

        internal void SetAnchors(SocketAnchorDefinition[] anchors)
        {
            _anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
        }
    }
}
