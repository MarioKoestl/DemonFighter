#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using UnityEngine;

namespace DemonFighter.UI
{
    /// <summary>
    /// The damage numbers over the targets of the player's hits and over the player itself (D-092): each rises over the
    /// head and fades within a second. Plain bookkeeping, so the rules are testable without a scene; the layer draws
    /// them. Damage the player takes shows in red; its burning and bleeding, which come every tick, show as one number
    /// every half second. Bleeding and fire on others have no attacker and stay hidden.
    /// </summary>
    public sealed class FloatingDamage
    {
        /// <summary>Seconds a number stays on screen.</summary>
        public const float LifeSeconds = 1f;

        /// <summary>How far a number rises over its life, per meter of the target's size.</summary>
        public const float RisePerMeter = 0.6f;

        /// <summary>Seconds over which the player's burning and bleeding add up into one number.</summary>
        public const float OverTimeSeconds = 0.5f;

        /// <summary>The color of damage the player takes.</summary>
        public static readonly Color TakenColor = new Color(1f, 0.22f, 0.18f);

        // Numbers of quick hits on one target fan out sideways, in screen pixels, instead of covering each other.
        private static readonly float[] SideOffsets = { 0f, 26f, -26f, 13f, -13f };

        private readonly List<Number> _numbers = new List<Number>();
        private int _added;
        private float _overTimeAmount;
        private float _overTimeAge = -1f;
        private DemonId _overTimeTarget = DemonId.None;
        private DamageType _overTimeType;
        private Vector3 _overTimeAnchor;
        private float _overTimeSize;
        private bool _overTimeBlocked;

        /// <summary>The numbers on screen, oldest first.</summary>
        public IReadOnlyList<Number> Numbers => _numbers;

        /// <summary>Takes a hit the player landed or took; the anchor is where the target stood.</summary>
        public bool Add(in DamageApplied hit, DemonId player, Vector3 anchor, float targetSize)
        {
            return Add(hit.Target, hit.Amount, hit.DamageType, hit.Attacker, player, anchor, targetSize, blocked: false);
        }

        /// <summary>Takes damage test mode kept off the player (D-089): shown like damage taken, in brackets.</summary>
        public bool Add(in DamageBlocked hit, DemonId player, Vector3 anchor, float targetSize)
        {
            return hit.Target == player && Add(hit.Target, hit.Amount, hit.DamageType, hit.Attacker, player, anchor, targetSize, blocked: true);
        }

        private bool Add(DemonId target, float amount, DamageType type, DemonId attacker, DemonId player, Vector3 anchor, float targetSize, bool blocked)
        {
            bool taken = target == player;
            bool dealt = attacker.IsValid && attacker == player && !taken;
            if (amount <= 0f || (!taken && !dealt))
            {
                return false;
            }

            if (taken && !attacker.IsValid)
            {
                if (_overTimeAge < 0f)
                {
                    _overTimeAge = 0f;
                    _overTimeAmount = 0f;
                }

                _overTimeAmount += amount;
                _overTimeTarget = target;
                _overTimeType = type;
                _overTimeAnchor = anchor;
                _overTimeSize = targetSize;
                _overTimeBlocked = blocked;
                return true;
            }

            Push(target, amount, type, anchor, targetSize, taken, blocked);
            return true;
        }

        /// <summary>Ages every number and drops the ones that have faded out.</summary>
        public void Advance(float seconds)
        {
            for (int i = _numbers.Count - 1; i >= 0; i--)
            {
                Number number = _numbers[i].Older(seconds);
                if (number.Age >= LifeSeconds)
                {
                    _numbers.RemoveAt(i);
                }
                else
                {
                    _numbers[i] = number;
                }
            }

            if (_overTimeAge >= 0f)
            {
                _overTimeAge += seconds;
                if (_overTimeAge >= OverTimeSeconds)
                {
                    Push(_overTimeTarget, _overTimeAmount, _overTimeType, _overTimeAnchor, _overTimeSize, taken: true, _overTimeBlocked);
                    _overTimeAge = -1f;
                }
            }
        }

        /// <summary>Forgets every number, as when a new run starts.</summary>
        public void Clear()
        {
            _numbers.Clear();
            _added = 0;
            _overTimeAge = -1f;
        }

        /// <summary>How far a number has risen, 0 at the hit and 1 at the end of its life; fast first, then slowing.</summary>
        public static float Rise(float age)
        {
            float t = Mathf.Clamp01(age / LifeSeconds);
            return 1f - ((1f - t) * (1f - t));
        }

        /// <summary>Opacity: full for the first half of its life, then fading to nothing.</summary>
        public static float Alpha(float age)
        {
            float t = Mathf.Clamp01(age / LifeSeconds);
            return t < 0.5f ? 1f : 1f - ((t - 0.5f) * 2f);
        }

        /// <summary>The number as shown: whole points, at least 1 for any hit that did damage; in brackets when test mode blocked it.</summary>
        public static string Text(float amount, bool blocked = false)
        {
            string points = Math.Max(1, Mathf.RoundToInt(amount)).ToString(CultureInfo.InvariantCulture);
            return blocked ? "(" + points + ")" : points;
        }

        /// <summary>The color of a damage type, so a bite, a claw and a blow read apart at a glance.</summary>
        public static Color ColorFor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Cut:
                    return new Color(1f, 0.62f, 0.25f);
                case DamageType.Blunt:
                    return new Color(0.82f, 0.88f, 1f);
                case DamageType.Fire:
                    return new Color(1f, 0.42f, 0.1f);
                default:
                    return new Color(1f, 0.95f, 0.72f);
            }
        }

        private void Push(DemonId target, float amount, DamageType type, Vector3 anchor, float targetSize, bool taken, bool blocked)
        {
            _numbers.Add(new Number(target, amount, type, anchor, Mathf.Max(targetSize, 0.1f), SideOffsets[_added % SideOffsets.Length], taken, blocked));
            _added++;
        }

        /// <summary>One number on screen.</summary>
        public readonly struct Number
        {
            public Number(DemonId target, float amount, DamageType type, Vector3 anchor, float targetSize, float side, bool taken, bool blocked = false)
                : this(target, amount, type, anchor, targetSize, side, taken, blocked, 0f)
            {
            }

            private Number(DemonId target, float amount, DamageType type, Vector3 anchor, float targetSize, float side, bool taken, bool blocked, float age)
            {
                Taken = taken;
                Blocked = blocked;
                Target = target;
                Amount = amount;
                Type = type;
                Anchor = anchor;
                TargetSize = targetSize;
                Side = side;
                Age = age;
            }

            public DemonId Target { get; }

            public float Amount { get; }

            public DamageType Type { get; }

            /// <summary>Where the target's feet were at the hit, in world space; the number hangs over its head.</summary>
            public Vector3 Anchor { get; }

            public float TargetSize { get; }

            /// <summary>Sideways offset in screen pixels.</summary>
            public float Side { get; }

            public float Age { get; }

            /// <summary>True for damage the player took, drawn in red.</summary>
            public bool Taken { get; }

            /// <summary>True for damage test mode kept off the player, shown in brackets.</summary>
            public bool Blocked { get; }

            /// <summary>The world point the number shows at now: over the head, risen with its age.</summary>
            public Vector3 WorldPosition(Vector3 feet)
            {
                return feet + (Vector3.up * (TargetSize * (1.05f + (RisePerMeter * Rise(Age)))));
            }

            internal Number Older(float seconds)
            {
                return new Number(Target, Amount, Type, Anchor, TargetSize, Side, Taken, Blocked, Age + seconds);
            }
        }
    }
}
