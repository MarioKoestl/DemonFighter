#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>Inspector-editable damage bonus a part gives one skill per level, nested in a body part asset (Jaws for Bite).</summary>
    [Serializable]
    public sealed class SkillBonusDefinition
    {
        [SerializeField] private SkillDefinition _skill = null!;
        [SerializeField] private float _damagePerLevel = 0.25f;

        public SkillBonus ToSpec(string partId)
        {
            if (_skill == null)
            {
                throw new ContentException("Body part " + partId + " has a skill bonus without a skill.");
            }

            return new SkillBonus(_skill.Id, _damagePerLevel);
        }

        internal void Configure(SkillDefinition skill, float damagePerLevel)
        {
            _skill = skill;
            _damagePerLevel = damagePerLevel;
        }
    }
}
