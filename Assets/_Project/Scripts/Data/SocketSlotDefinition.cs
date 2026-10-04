#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>Inspector-editable socket a part exposes, nested in a body part asset (the core lists its five).</summary>
    [Serializable]
    public sealed class SocketSlotDefinition
    {
        [SerializeField] private SocketKind _kind = SocketKind.Limb;
        [SerializeField] private int _capacity = 1;

        public SocketSlot ToSpec()
        {
            return new SocketSlot(_kind, _capacity);
        }

        internal void Configure(SocketSlot spec)
        {
            _kind = spec.Kind;
            _capacity = spec.Capacity;
        }
    }
}
