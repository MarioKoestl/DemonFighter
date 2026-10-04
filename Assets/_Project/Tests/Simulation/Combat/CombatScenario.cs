#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Combat;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;

namespace DemonFighter.Simulation.Tests.Combat
{
    /// <summary>
    /// A run with an attacker facing north and a target two meters north of it, plus the event capture combat and
    /// skill tests read.
    /// </summary>
    internal sealed class CombatScenario
    {
        public CombatScenario(DemonSpec? attackerSpec = null)
        {
            State = new RunStateBuilder().Build();
            Events = new SimulationEvents();
            Ticker = new SimulationTicker(State, Events);
            Attacker = new DemonBuilder().WithSpec(attackerSpec ?? TestContent.Blob).At(0f, 0f).FacingYaw(0f).SpawnInto(State);
            Target = new DemonBuilder().At(0f, 2f).SpawnInto(State);
            Events.Subscribe<DamageApplied>(Damage.Add);
            Events.Subscribe<PartWounded>(Wounded.Add);
            Events.Subscribe<PartSevered>(Severed.Add);
            Events.Subscribe<PartDestroyed>(Destroyed.Add);
            Events.Subscribe<DemonDied>(Deaths.Add);
            Events.Subscribe<FoodSpawned>(FoodSpawns.Add);
            Events.Subscribe<CommandRejected>(Rejections.Add);
            Events.Subscribe<SkillActivated>(Activations.Add);
            Events.Subscribe<SkillXpGained>(SkillXp.Add);
            Events.Subscribe<SkillLevelUp>(SkillLevelUps.Add);
            Events.Subscribe<FoodConsumed>(Consumed.Add);
            Events.Subscribe<FoodRemoved>(Removed.Add);
            Events.Subscribe<XpGained>(XpGains.Add);
            Events.Subscribe<LevelUp>(LevelUps.Add);
            Events.Subscribe<StatPointSpent>(StatSpends.Add);
            Events.Subscribe<DemonHeld>(Held.Add);
        }

        public RunState State { get; }

        public SimulationEvents Events { get; }

        public SimulationTicker Ticker { get; }

        public Demon Attacker { get; }

        public Demon Target { get; }

        public List<DamageApplied> Damage { get; } = new List<DamageApplied>();

        public List<PartWounded> Wounded { get; } = new List<PartWounded>();

        public List<PartSevered> Severed { get; } = new List<PartSevered>();

        public List<PartDestroyed> Destroyed { get; } = new List<PartDestroyed>();

        public List<DemonDied> Deaths { get; } = new List<DemonDied>();

        public List<FoodSpawned> FoodSpawns { get; } = new List<FoodSpawned>();

        public List<CommandRejected> Rejections { get; } = new List<CommandRejected>();

        public List<SkillActivated> Activations { get; } = new List<SkillActivated>();

        public List<SkillXpGained> SkillXp { get; } = new List<SkillXpGained>();

        public List<SkillLevelUp> SkillLevelUps { get; } = new List<SkillLevelUp>();

        public List<FoodConsumed> Consumed { get; } = new List<FoodConsumed>();

        public List<FoodRemoved> Removed { get; } = new List<FoodRemoved>();

        public List<XpGained> XpGains { get; } = new List<XpGained>();

        public List<LevelUp> LevelUps { get; } = new List<LevelUp>();

        public List<StatPointSpent> StatSpends { get; } = new List<StatPointSpent>();

        public List<DemonHeld> Held { get; } = new List<DemonHeld>();

        public DamageSystem DamageSystem => Ticker.Damage;

        /// <summary>The damage events one attacker caused; bleeding reports with no attacker are left out.</summary>
        public List<DamageApplied> DamageFrom(DemonId attacker)
        {
            return Damage.FindAll(d => d.Attacker == attacker);
        }

        /// <summary>Applies a skill hit on the target's core directly, bypassing commands.</summary>
        public void HitCore(SkillSpec skill, int skillLevel = 1)
        {
            DamageSystem.ApplyHit(Attacker, Target, Target.Body.Core, skill, skillLevel);
            Events.Flush();
        }

        /// <summary>Submits a command for the next tick.</summary>
        public void Submit<TCommand>(in TCommand command)
            where TCommand : struct, ICommand
        {
            Ticker.Commands.Submit(in command);
        }

        /// <summary>Starts Bite for the attacker and ticks into its active window.</summary>
        public SkillActivated StartBiteAndReachActiveWindow()
        {
            return StartSkillAndReachActiveWindow(TestContent.BiteId);
        }

        /// <summary>Starts any skill for the attacker and ticks into its active window.</summary>
        public SkillActivated StartSkillAndReachActiveWindow(string skillId)
        {
            Submit(new UseSkillCommand(Attacker.Id, skillId));
            Tick(1);
            SkillActivated activation = Activations[Activations.Count - 1];
            while (State.Tick < activation.ActiveFromTick)
            {
                Tick(1);
            }

            return activation;
        }

        public void Tick(int times)
        {
            for (int i = 0; i < times; i++)
            {
                Ticker.Tick();
            }
        }
    }
}
