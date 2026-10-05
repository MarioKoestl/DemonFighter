#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class ShadowBudgetTests
    {
        private static readonly Vector3[] Lights =
        {
            new Vector3(10f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(-5f, 0f, 0f),
            new Vector3(0f, 0f, 2f),
            new Vector3(30f, 0f, 30f),
        };

        [Test]
        public void SelectNearest_PicksTheNearestFirst()
        {
            var result = new int[3];

            int count = ShadowBudget.SelectNearest(Lights, Vector3.zero, result);

            count.Should().Be(3);
            result.Should().Equal(1, 3, 2);
        }

        [Test]
        public void SelectNearest_WithFewerLightsThanSlots_FillsWhatThereIs()
        {
            var result = new int[8];

            int count = ShadowBudget.SelectNearest(Lights, Vector3.zero, result);

            count.Should().Be(5);
            result[0].Should().Be(1);
            result[4].Should().Be(4);
        }

        [Test]
        public void SelectNearest_WithoutSlots_SelectsNothing()
        {
            int count = ShadowBudget.SelectNearest(Lights, Vector3.zero, new int[0]);

            count.Should().Be(0);
        }

        [Test]
        public void SelectNearest_FollowsTheOrigin()
        {
            var result = new int[1];

            ShadowBudget.SelectNearest(Lights, new Vector3(28f, 0f, 28f), result);

            result[0].Should().Be(4);
        }
    }
}
