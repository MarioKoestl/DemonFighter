#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// Where a socket sits on a part that exposes sockets, in body units (ASSET_PIPELINE, "Modular body parts"):
    /// 1 is the body height, X right, Y up, Z forward, the origin at the base of the body. A part plugged in here
    /// has its pivot at the anchor and its +Z along the anchor rotation, away from the body. The art binder fills
    /// these from the Socket_ transforms of a core model; the generator seeds defaults for the placeholder capsule.
    /// </summary>
    [Serializable]
    public sealed class SocketAnchorDefinition
    {
        [SerializeField] private SocketKind _kind = SocketKind.Head;
        [SerializeField] private Vector3 _position = new Vector3(0f, 0.6f, 0.25f);
        [SerializeField] private Vector3 _euler;

        public SocketKind Kind => _kind;

        /// <summary>Anchor position in body units, origin at the base of the body.</summary>
        public Vector3 Position => _position;

        /// <summary>Anchor rotation in degrees; a plugged-in part points its +Z along it.</summary>
        public Vector3 Euler => _euler;

        internal void Configure(SocketKind kind, Vector3 position, Vector3 euler)
        {
            _kind = kind;
            _position = position;
            _euler = euler;
        }
    }
}
