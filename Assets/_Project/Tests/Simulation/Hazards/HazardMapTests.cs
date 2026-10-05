#nullable enable
using System.Linq;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Hazards
{
    public sealed class HazardMapTests
    {
        private const float Tolerance = 0.01f;
        private const float Lookahead = 4f;
        private const float Margin = 1.5f;

        private static readonly HazardMap Map = new HazardMap(new[]
        {
            HazardZone.Disc(HazardKind.Lava, Vector2.Zero, 8f),
            HazardZone.Rectangle(HazardKind.Fissure, new Vector2(6f, 0f), 0f, 2f, 16f),
        });

        [Test]
        public void From_GeneratedWorld_HasAZoneForEveryLavaPoolAndFissure()
        {
            RunState state = new RunStateBuilder().Build();
            WorldLayout world = new CavernWorldGenerator().Generate(state.Seed, BiomeSpec.AshCavern);

            HazardMap map = HazardMap.From(world);

            int lava = world.Features.Count(f => f.Kind == FeatureKind.LavaPool);
            int fissures = world.Features.Count(f => f.Kind == FeatureKind.Fissure);
            map.Zones.Count(z => z.Kind == HazardKind.Lava).Should().Be(lava);
            map.Zones.Count(z => z.Kind == HazardKind.Fissure).Should().Be(fissures);
            lava.Should().BePositive();
        }

        [Test]
        public void AttachWorld_BuildsTheMap()
        {
            RunState state = new RunStateBuilder().Build();
            state.Hazards.Zones.Should().BeEmpty();

            state.AttachWorld(HazardWorlds.Flat(state.Seed));

            state.Hazards.Zones.Should().HaveCount(2);
            state.Hazards.KindAt(HazardWorlds.LavaCenter).Should().Be(HazardKind.Lava);
            state.Hazards.KindAt(HazardWorlds.FissureCenter).Should().Be(HazardKind.Fissure);
        }

        [Test]
        public void KindAt_WhereLavaAndFissureOverlap_IsLava()
        {
            Map.KindAt(new Vector3(6f, 0f, 0f)).Should().Be(HazardKind.Lava);
            Map.KindAt(new Vector3(6.5f, 0f, 7f)).Should().Be(HazardKind.Fissure);
            Map.KindAt(new Vector3(30f, 0f, 30f)).Should().Be(HazardKind.None);
        }

        [Test]
        public void Steer_HeadingStraightAtLava_BendsAroundIt()
        {
            var position = new Vector3(-11f, 0f, 0f);
            var wanted = new Vector2(1f, 0f);

            Vector2 steered = Map.Steer(position, wanted, Lookahead, Margin);

            steered.Length().Should().BeApproximately(1f, Tolerance);
            steered.X.Should().BeLessThan(0.5f, "most of the heading into the pool is gone");
            System.MathF.Abs(steered.Y).Should().BeGreaterThan(0.8f, "it slides along the edge");
        }

        [Test]
        public void Steer_HeadingAwayFromLava_KeepsTheHeading()
        {
            var position = new Vector3(-10f, 0f, 0f);
            var wanted = new Vector2(-1f, 0f);

            Map.Steer(position, wanted, Lookahead, Margin).Should().Be(wanted);
        }

        [Test]
        public void Steer_FarFromEverything_KeepsTheHeading()
        {
            var wanted = Vector2.Normalize(new Vector2(1f, 1f));

            Vector2 steered = Map.Steer(new Vector3(-60f, 0f, -60f), wanted, Lookahead, Margin);

            steered.X.Should().BeApproximately(wanted.X, Tolerance);
            steered.Y.Should().BeApproximately(wanted.Y, Tolerance);
        }

        [Test]
        public void Steer_InsideLava_WalksOutWhateverTheHeading()
        {
            var position = new Vector3(-6f, 0f, 0f);

            Vector2 steered = Map.Steer(position, new Vector2(1f, 0f), Lookahead, Margin);

            steered.X.Should().BeApproximately(-1f, Tolerance);
        }

        [Test]
        public void Steer_StandingStill_StaysStill()
        {
            Map.Steer(new Vector3(-9f, 0f, 0f), Vector2.Zero, Lookahead, Margin).Should().Be(Vector2.Zero);
        }

        [Test]
        public void PushOut_GoalInsideLava_MovesToTheMargin()
        {
            Vector3 pushed = Map.PushOut(new Vector3(0f, 2f, -4f), Margin);

            new Vector2(pushed.X, pushed.Z).Length().Should().BeGreaterThanOrEqualTo(8f + Margin - Tolerance);
            pushed.Y.Should().Be(2f);
            Map.KindAt(pushed).Should().Be(HazardKind.None);
        }

        [Test]
        public void PushOut_GoalOutside_StaysWhereItIs()
        {
            var goal = new Vector3(-40f, 0f, 20f);

            Map.PushOut(goal, Margin).Should().Be(goal);
        }

        [Test]
        public void Empty_HasNoHazardsAndSteersNothing()
        {
            var wanted = new Vector2(0f, 1f);

            HazardMap.Empty.KindAt(Vector3.Zero).Should().Be(HazardKind.None);
            HazardMap.Empty.Steer(Vector3.Zero, wanted, Lookahead, Margin).Should().Be(wanted);
        }
    }
}
