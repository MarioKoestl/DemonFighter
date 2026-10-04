#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// How a body part is drawn in the placeholder stage, nested in a body part asset: a primitive, a palette
    /// material and where it sits on the body capsule, in capsule mesh units (two tall, radius a half). The second
    /// copy of a part can be mirrored, so two arms hang on both sides.
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

        public PartVisualKind Kind => _kind;

        public PartMaterialRole Material => _material;

        /// <summary>Center of the primitive in capsule mesh units, X right, Y up, Z forward.</summary>
        public Vector3 LocalPosition => _localPosition;

        public Vector3 LocalScale => _localScale;

        public Vector3 LocalEuler => _localEuler;

        /// <summary>True when the second copy of the part mirrors across the X axis.</summary>
        public bool MirrorSecondCopy => _mirrorSecondCopy;

        internal void Configure(PartVisualKind kind, PartMaterialRole material, Vector3 position, Vector3 scale, Vector3 euler, bool mirrorSecondCopy)
        {
            _kind = kind;
            _material = material;
            _localPosition = position;
            _localScale = scale;
            _localEuler = euler;
            _mirrorSecondCopy = mirrorSecondCopy;
        }
    }
}
