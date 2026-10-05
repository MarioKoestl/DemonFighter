#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Presentation.Demons;
using DemonFighter.Presentation.Food;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Skills;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// The Unity side of combat for one run. Detects the hits of active skills (the part under the crosshair for the
    /// player, otherwise whatever stands in front within reach, which is all AI uses) and reports them for the
    /// simulation to judge (ARCHITECTURE, "Movement, collision and hits"), and turns combat events into gore
    /// (GAME_DESIGN, "Visible damage and gore", stage 2): blood on the hit part and on the ground, parts that fall off
    /// as the meshes they were, viscera bursts, corpses with pools spreading under them. The run controller creates
    /// it, hands it the views and disposes it with the run.
    /// </summary>
    public sealed class CombatPresenter : ICommandSource, IFrameUpdatable, IDisposable
    {
        private const float AimRayExtraMeters = 1.5f;
        private const float AimedFoodExtraMeters = 3f;
        private const float FocusRangePerMeter = 8f;
        private const float CrosshairSweepRadiusPerMeter = 0.1f;
        private const float FrontVolumeCenterPerReach = 0.5f;
        private const float FrontVolumeRadiusPerReach = 0.6f;
        private const float SplashOffsetPerMeter = 0.3f;
        private const float SeverBodyBlood = 0.6f;
        private const float DestroyBodyBlood = 0.4f;
        private const float DeathSplashSizeFactor = 1.5f;
        private const float HitWobblePerHpFraction = 2f;
        private const int DeathSplashes = 3;

        private static readonly Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0f);

        private readonly RunState _state;
        private readonly PlaceholderPalette _palette;
        private readonly GoreSettings _gore;
        private readonly DemonId _player;
        private readonly Transform _root;
        private readonly BloodDecalPool _blood;
        private readonly VisceraPool _viscera;
        private readonly ParticleSystem? _sparks;
        private float _sparkCarry;
        private readonly Dictionary<DemonId, DemonView> _views = new Dictionary<DemonId, DemonView>();
        private readonly Dictionary<FoodId, FoodView> _foodViews = new Dictionary<FoodId, FoodView>();
        private readonly Dictionary<DemonId, PendingUse> _pendingUses = new Dictionary<DemonId, PendingUse>();
        private readonly Dictionary<DemonId, Vector3> _lastHitPoints = new Dictionary<DemonId, Vector3>();
        private readonly List<DemonId> _finishedUses = new List<DemonId>();
        private readonly List<ReportHitCommand> _hitReports = new List<ReportHitCommand>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Collider[] _colliders = new Collider[32];
        private Camera? _camera;
        private BodyPartView? _focusedPart;

        public CombatPresenter(RunState state, SimulationEvents events, PlaceholderPalette palette, GoreSettings gore, DemonId player, Transform root)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _palette = palette != null ? palette : throw new ArgumentNullException(nameof(palette));
            _gore = gore != null ? gore : throw new ArgumentNullException(nameof(gore));
            _player = player;
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _blood = new BloodDecalPool(gore, palette.BloodSplats, palette.BloodPool, root);
            _viscera = new VisceraPool(gore, palette.Viscera, root);
            // A palette from before D-086 has no ember material yet; the fire then burns without sparks until the generator ran.
            _sparks = palette.Embers != null ? EmberEffects.CreateSparks(palette.Embers, root) : null;
            _subscriptions.Add(events.Subscribe<SkillActivated>(OnSkillActivated));
            _subscriptions.Add(events.Subscribe<DamageApplied>(OnDamageApplied));
            _subscriptions.Add(events.Subscribe<PartSevered>(OnPartSevered));
            _subscriptions.Add(events.Subscribe<PartDestroyed>(OnPartDestroyed));
            _subscriptions.Add(events.Subscribe<DemonDied>(OnDemonDied));
            _subscriptions.Add(events.Subscribe<FoodRemoved>(OnFoodRemoved));
            _subscriptions.Add(events.Subscribe<MutationStarted>(OnMutationStarted));
            _subscriptions.Add(events.Subscribe<Evolved>(OnEvolved));
        }

        /// <summary>The food under the crosshair of the player and within eat reach, or None; HUD prompt and Eat key use it.</summary>
        public FoodId AimedFood { get; private set; }

        /// <summary>The demon under the crosshair of the player within the aim range, or None (D-065).</summary>
        public DemonId FocusedDemon { get; private set; }

        /// <summary>Index of the body part under the crosshair; -1 when none.</summary>
        public int FocusedPartIndex { get; private set; } = -1;

        /// <summary>True when the primary skill of the player reaches the focused demon from where it stands.</summary>
        public bool FocusInReach { get; private set; }

        /// <summary>The demon the Analyze key locked for the HUD, or None; the lock drops when it dies (D-066).</summary>
        public DemonId LockedDemon { get; private set; }

        /// <summary>Locks the demon under the crosshair for analysis, or releases the current lock.</summary>
        public void ToggleLock()
        {
            LockedDemon = LockedDemon.IsValid ? DemonId.None : FocusedDemon;
        }

        /// <summary>Makes a spawned body known, so events for its demon reach it.</summary>
        public void Register(DemonView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (view.Demon == null)
            {
                throw new ArgumentException("Bind the view before registering it.", nameof(view));
            }

            _views[view.Demon.Id] = view;
        }

        /// <summary>
        /// After a resume (D-074): dead demons whose corpse still lies there become corpses again, dead demons whose
        /// corpse was eaten lose their body, and severed parts lie where they fell. Call once after every view exists.
        /// </summary>
        public void RestoreWorld()
        {
            var corpses = new Dictionary<DemonId, FoodItem>();
            IReadOnlyList<FoodItem> food = _state.Food;
            for (int i = 0; i < food.Count; i++)
            {
                if (food[i].Kind == FoodKind.Corpse)
                {
                    corpses[food[i].Source] = food[i];
                }
            }

            IReadOnlyList<Demon> demons = _state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.IsAlive || !_views.TryGetValue(demon.Id, out DemonView? view))
                {
                    continue;
                }

                if (corpses.TryGetValue(demon.Id, out FoodItem? corpse))
                {
                    view.BecomeCorpse(_palette.Corpse);
                    FoodView foodView = view.gameObject.AddComponent<FoodView>();
                    foodView.Bind(corpse, null);
                    _foodViews[corpse.Id] = foodView;
                }
                else
                {
                    _views.Remove(demon.Id);
                    Object.Destroy(view.gameObject);
                }
            }

            for (int i = 0; i < food.Count; i++)
            {
                FoodItem item = food[i];
                if (item.Kind == FoodKind.Corpse || _foodViews.ContainsKey(item.Id))
                {
                    continue;
                }

                float size = _state.TryGetDemon(item.Source, out Demon? source) ? source.SizeMeters : 1f;
                CreateSeveredPiece(item, item.Position.ToUnity() + Vector3.up * (size * _gore.SeveredFallbackSizePerMeter * 0.5f), size, launch: false, source: null);
            }
        }

        /// <inheritdoc />
        public void UpdateFrame()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            ScanActiveSkills(_state.Tick);
            AimedFood = FindAimedFood();
            UpdateFocus();
            _blood.Update(Time.time);
            _viscera.Update(Time.deltaTime, Time.time);
        }

        /// <inheritdoc />
        public void SubmitCommands(CommandQueue commands)
        {
            for (int i = 0; i < _hitReports.Count; i++)
            {
                commands.Submit(_hitReports[i]);
            }

            _hitReports.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
            if (_focusedPart != null)
            {
                _focusedPart.SetHighlighted(false);
                _focusedPart = null;
            }

            _blood.Destroy();
            _viscera.Destroy();
            EmberEffects.Destroy(_sparks);
        }

        // The part under the crosshair within the aim range glows and is reported to the HUD every frame, with whether
        // the primary attack would land from here; a locked demon stays locked until it dies or the key releases it.
        private void UpdateFocus()
        {
            BodyPartView? part = null;
            bool inReach = false;
            if (_camera != null && _views.TryGetValue(_player, out DemonView? playerView) && playerView.Demon != null && playerView.Demon.IsAlive && !playerView.IsCorpse)
            {
                Demon demon = playerView.Demon;
                float range = demon.SizeMeters * FocusRangePerMeter * demon.ReachMultiplier;
                Ray crosshair = _camera.ViewportPointToRay(ViewportCenter);
                float fromCamera = range + Vector3.Distance(_camera.transform.position, playerView.transform.position);
                if (Sweep(playerView, crosshair, demon.SizeMeters * CrosshairSweepRadiusPerMeter, fromCamera, out BodyPartView hit, out _) && hit.Owner != null && hit.Owner.Demon != null)
                {
                    part = hit;
                    SkillInstance? primary = SkillSlots.Find(demon, SkillSlot.Primary);
                    Vector3 toTarget = hit.Owner.transform.position - playerView.transform.position;
                    toTarget.y = 0f;
                    inReach = primary != null && toTarget.magnitude <= SkillReach.Meters(demon, hit.Owner.Demon, primary);
                }
            }

            if (part != _focusedPart)
            {
                if (_focusedPart != null)
                {
                    _focusedPart.SetHighlighted(false);
                }

                if (part != null)
                {
                    part.SetHighlighted(true);
                }

                _focusedPart = part;
            }

            FocusedDemon = part != null && part.Owner != null && part.Owner.Demon != null ? part.Owner.Demon.Id : DemonId.None;
            FocusedPartIndex = part != null ? part.PartIndex : -1;
            FocusInReach = inReach;
            if (LockedDemon.IsValid && (!_state.TryGetDemon(LockedDemon, out Demon? locked) || !locked.IsAlive))
            {
                LockedDemon = DemonId.None;
            }
        }

        // One report per use, in the active window; the simulation checks reach and arc again and may still refuse.
        private void ScanActiveSkills(long tick)
        {
            foreach (KeyValuePair<DemonId, PendingUse> entry in _pendingUses)
            {
                PendingUse use = entry.Value;
                if (use.Reported || tick > use.ActiveUntilTick)
                {
                    _finishedUses.Add(entry.Key);
                    continue;
                }

                if (tick < use.ActiveFromTick)
                {
                    continue;
                }

                if (!_views.TryGetValue(entry.Key, out DemonView? attacker) || attacker.Demon == null || !attacker.Demon.IsAlive)
                {
                    _finishedUses.Add(entry.Key);
                    continue;
                }

                if (TryFindHit(attacker, use.Skill, out BodyPartView? part, out Vector3 point) && part.Owner != null && part.Owner.Demon != null)
                {
                    use.Reported = true;
                    _lastHitPoints[part.Owner.Demon.Id] = point;
                    _hitReports.Add(new ReportHitCommand(entry.Key, part.Owner.Demon.Id, part.PartIndex));
                }
            }

            for (int i = 0; i < _finishedUses.Count; i++)
            {
                _pendingUses.Remove(_finishedUses[i]);
            }

            _finishedUses.Clear();
        }

        // The player gets the part under the crosshair first, so aiming picks the part (D-027); when the crosshair
        // rests on nothing, the bite still lands on whatever stands in front within reach, like it does for AI.
        private bool TryFindHit(DemonView attacker, SkillSpec skill, out BodyPartView part, out Vector3 point)
        {
            Demon demon = attacker.Demon!;
            float size = demon.SizeMeters;
            float reach = skill.ReachPerMeter * size * demon.ReachMultiplier + AimRayExtraMeters;
            if (demon.Id == _player && _camera != null)
            {
                Ray crosshair = _camera.ViewportPointToRay(ViewportCenter);
                float fromCamera = reach + Vector3.Distance(_camera.transform.position, attacker.transform.position);
                if (Sweep(attacker, crosshair, size * CrosshairSweepRadiusPerMeter, fromCamera, out part, out point))
                {
                    return true;
                }
            }

            return OverlapFront(attacker, reach, out part, out point);
        }

        private bool Sweep(DemonView attacker, Ray ray, float radius, float distance, out BodyPartView part, out Vector3 point)
        {
            int count = Physics.SphereCastNonAlloc(ray, radius, _hits, distance, Layers.DemonMask, QueryTriggerInteraction.Collide);
            BodyPartView? nearest = null;
            float nearestDistance = float.MaxValue;
            Vector3 nearestPoint = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                var candidate = hit.collider.GetComponent<BodyPartView>();
                if (!IsValidTarget(candidate, attacker) || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = hit.distance;
                nearestPoint = hit.distance > 0f ? hit.point : candidate!.transform.position;
            }

            part = nearest!;
            point = nearestPoint;
            return nearest != null;
        }

        // Everything in front within reach: a sphere ahead of the body center, so a 15 m elder still reaches a blob at
        // its feet and a blob bites what stands beside its snout. The simulation checks the real reach and arc.
        private bool OverlapFront(DemonView attacker, float reach, out BodyPartView part, out Vector3 point)
        {
            Vector3 forward = attacker.transform.forward;
            Vector3 origin = attacker.transform.position + Vector3.up * (attacker.Demon!.SizeMeters * 0.5f);
            Vector3 center = origin + forward * (reach * FrontVolumeCenterPerReach);
            int count = Physics.OverlapSphereNonAlloc(center, reach * FrontVolumeRadiusPerReach, _colliders, Layers.DemonMask, QueryTriggerInteraction.Collide);
            BodyPartView? nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = _colliders[i].GetComponent<BodyPartView>();
                if (!IsValidTarget(candidate, attacker))
                {
                    continue;
                }

                Vector3 toPart = candidate!.transform.position - origin;
                float distance = toPart.magnitude;
                if (Vector3.Dot(forward, toPart) <= 0f || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            part = nearest!;
            point = nearest != null ? nearest.GetComponent<Collider>().ClosestPoint(origin) : Vector3.zero;
            return nearest != null;
        }

        private static bool IsValidTarget(BodyPartView? candidate, DemonView attacker)
        {
            return candidate != null
                && candidate.Owner != null
                && candidate.Owner != attacker
                && !candidate.Owner.IsCorpse
                && candidate.Owner.Demon != null
                && candidate.Owner.Demon.IsAlive;
        }

        private FoodId FindAimedFood()
        {
            if (_camera == null || !_views.TryGetValue(_player, out DemonView? playerView) || playerView.Demon == null)
            {
                return FoodId.None;
            }

            float reach = _state.Catalog.Tuning.EatReachPerMeter * playerView.Demon.SizeMeters * playerView.Demon.ReachMultiplier;
            float maxDistance = reach + AimedFoodExtraMeters + Vector3.Distance(_camera.transform.position, playerView.transform.position);
            Ray ray = _camera.ViewportPointToRay(ViewportCenter);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, Layers.FoodMask, QueryTriggerInteraction.Collide))
            {
                return FoodId.None;
            }

            var food = hit.collider.GetComponentInParent<FoodView>();
            if (food == null || food.Food == null)
            {
                return FoodId.None;
            }

            // Within eat reach on the ground plane, the same check the simulation makes.
            Vector3 toFood = food.transform.position - playerView.transform.position;
            toFood.y = 0f;
            return toFood.magnitude <= reach ? food.Food.Id : FoodId.None;
        }

        private void OnSkillActivated(SkillActivated evt)
        {
            if (!_views.TryGetValue(evt.Actor, out DemonView? view))
            {
                return;
            }

            SkillSpec skill = _state.Catalog.GetSkill(evt.SkillId);
            view.PlayAttack(evt.SkillId, skill.WindupSeconds, skill.ActiveSeconds, skill.RecoverySeconds);
            _pendingUses[evt.Actor] = new PendingUse(skill, evt.ActiveFromTick, evt.ActiveUntilTick);
        }

        // A hit soaks the part where it landed and splashes the ground; bleeding seeps on the part and drips a trail.
        private void OnDamageApplied(DamageApplied evt)
        {
            if (!_views.TryGetValue(evt.Target, out DemonView? view) || view.Demon == null || evt.Amount <= 0f)
            {
                return;
            }

            float size = view.Demon.SizeMeters;
            BodyPartView? part = view.FindPart(evt.PartIndex);
            if (evt.DamageType == DamageType.Fire)
            {
                Burn(view, part, evt, size);
                return;
            }

            if (evt.Attacker.IsValid)
            {
                Vector3 fallback = part != null ? part.transform.position : view.transform.position;
                Vector3 point = _lastHitPoints.TryGetValue(evt.Target, out Vector3 hitPoint) ? hitPoint : fallback;
                _lastHitPoints.Remove(evt.Target);
                if (part != null && view.Demon.Body.HasPart(evt.PartIndex))
                {
                    BodyPart target = view.Demon.Body.GetPart(evt.PartIndex);
                    float fraction = evt.Amount / Mathf.Max(target.MaxHp, 0.01f);
                    part.AddBlood(fraction * _gore.BloodPerHpFraction, point);
                    view.PlayHit(fraction * HitWobblePerHpFraction);
                }

                _blood.SplashGround(view.transform.position + RandomOffset(size), size);
                return;
            }

            if (part != null)
            {
                part.AddBlood(_gore.BleedBloodPerSecond * _state.Config.TickSeconds);
            }

            if (_state.Tick % _gore.BleedDripEveryTicks == 0)
            {
                _blood.SplashGround(view.transform.position + RandomOffset(size), size * 0.5f);
            }
        }

        // Fire chars the part it eats and throws sparks off the body; no blood, the heat seals the wound (D-086).
        private void Burn(DemonView view, BodyPartView? part, DamageApplied evt, float size)
        {
            if (part != null && view.Demon != null && view.Demon.Body.HasPart(evt.PartIndex))
            {
                BodyPart burning = view.Demon.Body.GetPart(evt.PartIndex);
                part.AddBurn(evt.Amount / Mathf.Max(burning.MaxHp, 0.01f) * _gore.BurnCharPerHpFraction);
            }

            // Fire damage arrives every tick; sparks are metered so their number follows time, not the tick rate.
            _sparkCarry += _gore.SparksPerSecondPerMeter * size * _state.Config.TickSeconds;
            int count = Mathf.FloorToInt(_sparkCarry);
            _sparkCarry -= count;
            if (count > 0 && _sparks != null)
            {
                Vector3 center = part != null ? part.transform.position : view.transform.position + Vector3.up * (size * 0.3f);
                EmberEffects.Spark(_sparks, center, size, count);
            }
        }

        // The part flies off as the mesh it was, viscera burst from the wound, the body is soaked at the stump.
        private void OnPartSevered(PartSevered evt)
        {
            if (!_views.TryGetValue(evt.Demon, out DemonView? view) || view.Demon == null || !_state.TryGetFood(evt.Food, out FoodItem? food))
            {
                return;
            }

            float size = view.Demon.SizeMeters;
            BodyPartView? part = view.FindPart(evt.PartIndex);
            Vector3 position = part != null ? part.transform.position : view.transform.position + Vector3.up * (size * 0.5f);
            CreateSeveredPiece(food, position, size, launch: true, part);
            _viscera.Burst(position, size, GoreMath.BurstCount(size, _gore.SeverBurstPerMeter));
            _blood.SplashGround(position, size);
            SoakBody(view, SeverBodyBlood, position);
        }

        private void OnPartDestroyed(PartDestroyed evt)
        {
            if (!_views.TryGetValue(evt.Demon, out DemonView? view) || view.Demon == null)
            {
                return;
            }

            float size = view.Demon.SizeMeters;
            BodyPartView? part = view.FindPart(evt.PartIndex);
            Vector3 position = part != null ? part.transform.position : view.transform.position;
            _viscera.Burst(position, size, GoreMath.BurstCount(size, _gore.DestroyBurstPerMeter));
            SoakBody(view, DestroyBodyBlood, position);
        }

        // The body becomes the corpse, a pool spreads under it with its Biomass, and viscera burst from the kill.
        private void OnDemonDied(DemonDied evt)
        {
            if (!_views.TryGetValue(evt.Demon, out DemonView? view) || view.Demon == null)
            {
                return;
            }

            _pendingUses.Remove(evt.Demon);
            float size = view.Demon.SizeMeters;
            Vector3 center = view.transform.position + Vector3.up * (size * 0.5f);
            _viscera.Burst(center, size, GoreMath.BurstCount(size, _gore.DeathBurstPerMeter));
            view.BecomeCorpse(_palette.Corpse);
            float biomass = 0f;
            if (_state.TryGetFood(evt.Corpse, out FoodItem? corpse))
            {
                biomass = corpse.BiomassRemaining;
                FoodView foodView = view.gameObject.AddComponent<FoodView>();
                foodView.Bind(corpse, null);
                _foodViews[corpse.Id] = foodView;
            }

            _blood.PoolUnderCorpse(view.transform.position, size, biomass);
            for (int i = 0; i < DeathSplashes; i++)
            {
                _blood.SplashGround(view.transform.position + RandomOffset(size), size * DeathSplashSizeFactor);
            }
        }

        // A severed part keeps the look of the view it had: the same mesh, material and tint, now loose with physics.
        // A fresh one flies off, a restored one just lies there; without a view to copy a dark sphere stands in.
        private void CreateSeveredPiece(FoodItem food, Vector3 position, float size, bool launch, BodyPartView? source)
        {
            GameObject piece;
            if (source != null)
            {
                piece = source.CreateDetachedCopy(_root);
            }
            else
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                piece.transform.SetParent(_root, false);
                piece.transform.localScale = Vector3.one * (size * _gore.SeveredFallbackSizePerMeter);
                piece.GetComponent<Renderer>().sharedMaterial = _palette.Corpse;
            }

            piece.name = "Severed part " + food.Id.Value;
            piece.layer = Layers.Food;
            piece.transform.position = position;
            Rigidbody body = piece.AddComponent<Rigidbody>();
            if (launch)
            {
                body.AddForce((Random.insideUnitSphere + Vector3.up).normalized * _gore.SeveredImpulse, ForceMode.VelocityChange);
            }

            FoodView foodView = piece.AddComponent<FoodView>();
            foodView.Bind(food, body);
            _foodViews[food.Id] = foodView;
        }

        // Blood on the core around a point: the stump of a severed limb, the crater of a destroyed part.
        private static void SoakBody(DemonView view, float amount, Vector3 point)
        {
            if (view.Parts.Count > 0)
            {
                view.Parts[0].AddBlood(amount, point);
            }
        }

        private void OnMutationStarted(MutationStarted evt)
        {
            Throb(evt.Demon, evt.UntilTick);
        }

        private void OnEvolved(Evolved evt)
        {
            Throb(evt.Demon, evt.UntilTick);
        }

        // The body throbs for the transformation time, so a mutation reads as growth and not as a popped-in part.
        private void Throb(DemonId demon, long untilTick)
        {
            if (_views.TryGetValue(demon, out DemonView? view))
            {
                view.PlayTransformation((untilTick - _state.Tick) * _state.Config.TickSeconds);
            }
        }

        private void OnFoodRemoved(FoodRemoved evt)
        {
            if (!_foodViews.TryGetValue(evt.Food, out FoodView? view))
            {
                return;
            }

            _foodViews.Remove(evt.Food);
            var corpse = view.GetComponent<DemonView>();
            if (corpse != null && corpse.Demon != null)
            {
                _views.Remove(corpse.Demon.Id);
            }

            Object.Destroy(view.gameObject);
        }

        private static Vector3 RandomOffset(float sizeMeters)
        {
            float range = sizeMeters * SplashOffsetPerMeter;
            return new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));
        }

        private sealed class PendingUse
        {
            public PendingUse(SkillSpec skill, long activeFromTick, long activeUntilTick)
            {
                Skill = skill;
                ActiveFromTick = activeFromTick;
                ActiveUntilTick = activeUntilTick;
            }

            public SkillSpec Skill { get; }

            public long ActiveFromTick { get; }

            public long ActiveUntilTick { get; }

            public bool Reported { get; set; }
        }
    }
}
