#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// A body part kind as an asset, converted to an immutable <see cref="BodyPartSpec"/> at load. Granted skills,
    /// skill bonuses and required parts are asset references, so a broken link shows in the Inspector instead of at
    /// run start. The visual block says how the placeholder stage draws the part on the body.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Body Part", fileName = "BP_NewPart")]
    public sealed class BodyPartDefinition : ScriptableObject
    {
        private const int CurrentContentVersion = 3;

        [Header("Identity")]
        [SerializeField] private string _id = "part.new";
        [SerializeField] private string _displayName = "New Part";
        [SerializeField] private SocketKind _socket = SocketKind.Limb;

        [Header("Durability")]
        [SerializeField] private float _maxHp = 20f;
        [SerializeField] private DefenseType _defense = DefenseType.None;
        [SerializeField] private PartFate _fate = PartFate.Severed;
        [SerializeField] private int _maxUpgrade = 5;

        [Header("Gameplay")]
        [SerializeField] private SkillDefinition[] _grantedSkills = Array.Empty<SkillDefinition>();
        [SerializeField] private StatValueDefinition[] _statBonusesPerLevel = Array.Empty<StatValueDefinition>();
        [SerializeField] private SkillBonusDefinition[] _skillDamageBonusesPerLevel = Array.Empty<SkillBonusDefinition>();
        [SerializeField] private float _moveSpeedBonus;
        [SerializeField] private float _perceptionBonus;
        [SerializeField] private float _returnDamageFraction;
        [SerializeField] private SocketSlotDefinition[] _sockets = Array.Empty<SocketSlotDefinition>();
        [SerializeField] private float _biomassValue = 5f;

        [Header("Mutation")]
        [SerializeField] private float _biomassCost = 30f;
        [SerializeField] private int _minLevel = 1;
        [SerializeField] private int _repeatMinLevel = 1;
        [SerializeField] private BodyPartDefinition[] _requiredParts = Array.Empty<BodyPartDefinition>();
        [SerializeField] private bool _requiresUnlock;

        [Header("Placeholder visual")]
        [SerializeField] private PartVisualDefinition _visual = new PartVisualDefinition();

        [SerializeField, HideInInspector] private int _contentVersion;

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>How the placeholder stage draws this part.</summary>
        public PartVisualDefinition Visual => _visual;

        /// <summary>True for an asset created before M3 that still lacks the M3 fields.</summary>
        internal bool NeedsM3Defaults => _contentVersion < CurrentContentVersion;

        /// <summary>Builds the immutable spec; throws for invalid content or a missing reference.</summary>
        public BodyPartSpec ToSpec()
        {
            var stats = new StatValue[_statBonusesPerLevel.Length];
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] = _statBonusesPerLevel[i].ToSpec();
            }

            var bonuses = new SkillBonus[_skillDamageBonusesPerLevel.Length];
            for (int i = 0; i < bonuses.Length; i++)
            {
                bonuses[i] = _skillDamageBonusesPerLevel[i].ToSpec(_id);
            }

            var sockets = new SocketSlot[_sockets.Length];
            for (int i = 0; i < sockets.Length; i++)
            {
                sockets[i] = _sockets[i].ToSpec();
            }

            var required = new string[_requiredParts.Length];
            for (int i = 0; i < required.Length; i++)
            {
                if (_requiredParts[i] == null)
                {
                    throw new ContentException("Body part " + _id + " has an empty required part slot.");
                }

                required[i] = _requiredParts[i].Id;
            }

            var spec = new BodyPartSpec
            {
                Id = _id,
                Name = _displayName,
                Socket = _socket,
                MaxHp = _maxHp,
                Defense = _defense,
                Fate = _fate,
                MaxUpgrade = _maxUpgrade,
                GrantedSkillIds = SkillIds(),
                StatBonusesPerLevel = stats,
                SkillDamageBonusesPerLevel = bonuses,
                MoveSpeedBonus = _moveSpeedBonus,
                PerceptionBonus = _perceptionBonus,
                ReturnDamageFraction = _returnDamageFraction,
                Sockets = sockets,
                BiomassValue = _biomassValue,
                BiomassCost = _biomassCost,
                MinLevel = _minLevel,
                RepeatMinLevel = _repeatMinLevel,
                RequiredPartIds = required,
                RequiresUnlock = _requiresUnlock,
            };
            spec.Validate();
            return spec;
        }

        /// <summary>Fills a new asset from a spec; the generator uses it once, afterwards the asset is the truth.</summary>
        internal void Configure(BodyPartSpec spec, SkillDefinition[] grantedSkills, SkillDefinition[] bonusSkills, BodyPartDefinition[] requiredParts)
        {
            _id = spec.Id;
            _displayName = spec.Name;
            _socket = spec.Socket;
            _maxHp = spec.MaxHp;
            _defense = spec.Defense;
            _fate = spec.Fate;
            _grantedSkills = grantedSkills;
            _biomassValue = spec.BiomassValue;
            ApplyM3Defaults(spec, bonusSkills, requiredParts);
        }

        /// <summary>Gives an M2 asset the fields M3 added without touching the numbers it already had.</summary>
        internal void ApplyM3Defaults(BodyPartSpec spec, SkillDefinition[] bonusSkills, BodyPartDefinition[] requiredParts)
        {
            _maxUpgrade = spec.MaxUpgrade;
            _statBonusesPerLevel = new StatValueDefinition[spec.StatBonusesPerLevel.Count];
            for (int i = 0; i < _statBonusesPerLevel.Length; i++)
            {
                _statBonusesPerLevel[i] = new StatValueDefinition();
                _statBonusesPerLevel[i].Configure(spec.StatBonusesPerLevel[i]);
            }

            _skillDamageBonusesPerLevel = new SkillBonusDefinition[spec.SkillDamageBonusesPerLevel.Count];
            for (int i = 0; i < _skillDamageBonusesPerLevel.Length; i++)
            {
                _skillDamageBonusesPerLevel[i] = new SkillBonusDefinition();
                _skillDamageBonusesPerLevel[i].Configure(bonusSkills[i], spec.SkillDamageBonusesPerLevel[i].DamagePerLevel);
            }

            _moveSpeedBonus = spec.MoveSpeedBonus;
            _perceptionBonus = spec.PerceptionBonus;
            _returnDamageFraction = spec.ReturnDamageFraction;
            _sockets = new SocketSlotDefinition[spec.Sockets.Count];
            for (int i = 0; i < _sockets.Length; i++)
            {
                _sockets[i] = new SocketSlotDefinition();
                _sockets[i].Configure(spec.Sockets[i]);
            }

            _biomassCost = spec.BiomassCost;
            _minLevel = spec.MinLevel;
            _repeatMinLevel = spec.RepeatMinLevel;
            _requiredParts = requiredParts;
            _requiresUnlock = spec.RequiresUnlock;
            _contentVersion = CurrentContentVersion;
        }

        internal void ConfigureVisual(PartVisualKind kind, PartMaterialRole material, Vector3 position, Vector3 scale, Vector3 euler, bool mirrorSecondCopy)
        {
            _visual.Configure(kind, material, position, scale, euler, mirrorSecondCopy);
        }

        private string[] SkillIds()
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

            return skillIds;
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
