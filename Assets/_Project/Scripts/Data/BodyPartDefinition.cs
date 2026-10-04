#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// A body part kind as an asset, converted to an immutable <see cref="BodyPartSpec"/> at load. Granted skills are
    /// asset references, so a broken link shows in the Inspector instead of at run start.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Body Part", fileName = "BP_NewPart")]
    public sealed class BodyPartDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "part.new";
        [SerializeField] private string _displayName = "New Part";
        [SerializeField] private SocketKind _socket = SocketKind.Limb;

        [Header("Durability")]
        [SerializeField] private float _maxHp = 20f;
        [SerializeField] private DefenseType _defense = DefenseType.None;
        [SerializeField] private PartFate _fate = PartFate.Severed;

        [Header("Gameplay")]
        [SerializeField] private SkillDefinition[] _grantedSkills = Array.Empty<SkillDefinition>();
        [SerializeField] private float _biomassValue = 5f;

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>Builds the immutable spec; throws for invalid content or a missing skill reference.</summary>
        public BodyPartSpec ToSpec()
        {
            var skillIds = new string[_grantedSkills.Length];
            for (int i = 0; i < _grantedSkills.Length; i++)
            {
                if (_grantedSkills[i] == null)
                {
                    throw new ContentException("Body part " + _id + " has an empty granted skill slot.");
                }

                skillIds[i] = _grantedSkills[i].Id;
            }

            var spec = new BodyPartSpec
            {
                Id = _id,
                Name = _displayName,
                Socket = _socket,
                MaxHp = _maxHp,
                Defense = _defense,
                Fate = _fate,
                GrantedSkillIds = skillIds,
                BiomassValue = _biomassValue,
            };
            spec.Validate();
            return spec;
        }

        internal void Configure(BodyPartSpec spec, SkillDefinition[] grantedSkills)
        {
            _id = spec.Id;
            _displayName = spec.Name;
            _socket = spec.Socket;
            _maxHp = spec.MaxHp;
            _defense = spec.Defense;
            _fate = spec.Fate;
            _grantedSkills = grantedSkills;
            _biomassValue = spec.BiomassValue;
        }

        private void OnValidate()
        {
            try
            {
                ToSpec();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Content, "Body part asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
