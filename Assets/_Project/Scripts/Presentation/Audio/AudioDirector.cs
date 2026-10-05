#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>
    /// Turns the events of one run into sound (GAME_DESIGN, "Audio"; D-084): skill uses, wet impacts, parts torn
    /// off, deaths, eating, mutation, evolution, level and threat cues, footsteps metered from the movement of every
    /// body, the cavern drone with the lava loop on the nearest pool, and the calm or combat music by the player's
    /// state. Sounds of the player play in the head, everyone else's at their body. The run controller creates it,
    /// ticks it once per frame and disposes it with the run; the music player outlives the run.
    /// </summary>
    public sealed class AudioDirector : IFrameUpdatable, IDisposable
    {
        private const float BodyCenterPerMeter = 0.5f;

        private readonly RunState _state;
        private readonly ContentCatalogDefinition _content;
        private readonly AudioCatalogDefinition _catalog;
        private readonly BiomeDefinition _biome;
        private readonly MusicPlayer _music;
        private readonly SoundPlayer _sounds;
        private readonly AmbientPlayer _ambient;
        private readonly FootstepMeter _steps = new FootstepMeter();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly DemonId _player;
        private readonly int _combatWindowTicks;

        public AudioDirector(RunState state, SimulationEvents events, ContentCatalogDefinition content, AudioCatalogDefinition catalog, BiomeDefinition biome, AudioMix mix, MusicPlayer music, DemonId player, Transform root)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _content = content != null ? content : throw new ArgumentNullException(nameof(content));
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            _biome = biome != null ? biome : throw new ArgumentNullException(nameof(biome));
            _music = music != null ? music : throw new ArgumentNullException(nameof(music));
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _player = player;
            _sounds = new SoundPlayer(mix, root);
            _ambient = new AmbientPlayer(mix, root, biome.AmbientLoop, biome.LavaLoop, LavaPositions(state.World));
            _combatWindowTicks = MusicCues.CombatWindowTicks(state.Catalog.Tuning.InCombatSeconds, state.Config.TickSeconds);

            _subscriptions.Add(events.Subscribe<SkillActivated>(OnSkillActivated));
            _subscriptions.Add(events.Subscribe<DamageApplied>(OnDamageApplied));
            _subscriptions.Add(events.Subscribe<PartSevered>(OnPartSevered));
            _subscriptions.Add(events.Subscribe<PartDestroyed>(OnPartDestroyed));
            _subscriptions.Add(events.Subscribe<DemonDied>(OnDemonDied));
            _subscriptions.Add(events.Subscribe<FoodConsumed>(OnFoodConsumed));
            _subscriptions.Add(events.Subscribe<MutationStarted>(OnMutationStarted));
            _subscriptions.Add(events.Subscribe<MutationCompleted>(OnMutationCompleted));
            _subscriptions.Add(events.Subscribe<Evolved>(OnEvolved));
            _subscriptions.Add(events.Subscribe<LevelUp>(OnLevelUp));
            _subscriptions.Add(events.Subscribe<ThreatLevelChanged>(OnThreatLevelChanged));
        }

        /// <inheritdoc />
        public void UpdateFrame()
        {
            float deltaTime = Time.deltaTime;
            IReadOnlyList<Demon> demons = _state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.IsAlive)
                {
                    continue;
                }

                float distance = demon.Velocity.Length() * deltaTime;
                if (_steps.Advance(demon.Id, distance, demon.SizeMeters * _catalog.FootstepStridePerMeter))
                {
                    _sounds.Play(_catalog.Footstep, demon.Position.ToUnity(), AudioVariance.PitchForSize(demon.SizeMeters));
                }
            }

            if (_state.TryGetDemon(_player, out Demon? player))
            {
                _ambient.Update(player.Position.ToUnity());
                MusicCue cue = MusicCues.ForRun(player.IsAlive, player.IsInCombat(_state.Tick, _combatWindowTicks));
                _music.Play(cue == MusicCue.Combat ? _biome.CombatTrack : _biome.CalmTrack);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
            _ambient.Dispose();
            _sounds.Dispose();
        }

        private void OnSkillActivated(SkillActivated evt)
        {
            SkillDefinition? skill = _content.FindSkill(evt.SkillId);
            Play(skill != null ? skill.UseSound : null, evt.Actor);
        }

        private void OnDamageApplied(DamageApplied evt)
        {
            if (evt.Amount <= 0f)
            {
                return;
            }

            // Fire arrives every tick; the cooldown of the burn sound keeps it a sizzle, not a buzz (D-086).
            if (evt.DamageType == DamageType.Fire)
            {
                Play(_catalog.Burn, evt.Target);
            }
            else if (evt.Attacker.IsValid)
            {
                Play(_catalog.WetImpact, evt.Target);
            }
        }

        private void OnPartSevered(PartSevered evt)
        {
            AudioEventDefinition? sound = null;
            if (_state.TryGetDemon(evt.Demon, out Demon? demon) && demon.Body.HasPart(evt.PartIndex))
            {
                BodyPartDefinition? part = _content.FindBodyPart(demon.Body.GetPart(evt.PartIndex).Spec.Id);
                sound = part != null ? part.SeverSound : null;
            }

            Play(sound != null ? sound : _catalog.PartSevered, evt.Demon);
        }

        private void OnPartDestroyed(PartDestroyed evt)
        {
            Play(_catalog.PartDestroyed, evt.Demon);
        }

        private void OnDemonDied(DemonDied evt)
        {
            _steps.Forget(evt.Demon);
            if (evt.Demon == _player)
            {
                _sounds.PlayFlat(_catalog.PlayerDeath);
            }
            else
            {
                Play(_catalog.DemonDeath, evt.Demon);
            }
        }

        private void OnFoodConsumed(FoodConsumed evt)
        {
            Play(_catalog.Eat, evt.Eater);
        }

        private void OnMutationStarted(MutationStarted evt)
        {
            Play(_catalog.MutationStart, evt.Demon);
        }

        private void OnMutationCompleted(MutationCompleted evt)
        {
            Play(_catalog.MutationComplete, evt.Demon);
        }

        private void OnEvolved(Evolved evt)
        {
            Play(_catalog.Evolved, evt.Demon);
        }

        private void OnLevelUp(LevelUp evt)
        {
            if (evt.Demon == _player)
            {
                _sounds.PlayFlat(_catalog.LevelUp);
            }
        }

        private void OnThreatLevelChanged(ThreatLevelChanged evt)
        {
            _sounds.PlayFlat(_catalog.ThreatRise);
        }

        // The player hears their own sounds in the head; everyone else sounds from their body, lower the bigger they are.
        private void Play(AudioEventDefinition? sound, DemonId demon)
        {
            if (sound == null)
            {
                return;
            }

            if (demon == _player)
            {
                _sounds.PlayFlat(sound);
                return;
            }

            if (_state.TryGetDemon(demon, out Demon? body))
            {
                Vector3 center = body.Position.ToUnity() + Vector3.up * (body.SizeMeters * BodyCenterPerMeter);
                _sounds.Play(sound, center, AudioVariance.PitchForSize(body.SizeMeters));
            }
        }

        private static List<Vector3> LavaPositions(WorldLayout? world)
        {
            var positions = new List<Vector3>();
            if (world == null)
            {
                return positions;
            }

            for (int i = 0; i < world.Features.Count; i++)
            {
                if (world.Features[i].Kind == FeatureKind.LavaPool)
                {
                    positions.Add(world.Features[i].Position.ToUnity());
                }
            }

            return positions;
        }
    }
}
