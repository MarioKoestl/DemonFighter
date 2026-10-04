#nullable enable
using DemonFighter.Simulation.Anatomy;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// One body part as a primitive with a trigger collider on the Demon layer, so the aim rays of skills find the
    /// part under the crosshair (ARCHITECTURE, "Movement, collision and hits"). Shows the condition of the part:
    /// wounded parts darken and shrink, lost parts vanish (GAME_DESIGN, "Visible damage and gore", stage 1). On
    /// death the part freezes as dead flesh on the Food layer, visible and aimable whatever its condition says.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public sealed class BodyPartView : MonoBehaviour
    {
        private const float WoundedScale = 0.8f;
        private const float WoundedDarkening = 0.5f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer _renderer = null!;
        private Collider _collider = null!;
        private MaterialPropertyBlock _block = null!;
        private Vector3 _baseScale = Vector3.one;
        private float _conditionScale = 1f;
        private float _pulse = 1f;
        private bool _visible = true;
        private bool _lost;
        private bool _corpse;
        private bool _hasShown;
        private PartCondition _shown;

        /// <summary>The body this part belongs to; null before the owner binds.</summary>
        public DemonView? Owner { get; private set; }

        /// <summary>Index of the part in the simulation body.</summary>
        public int PartIndex { get; private set; }

        /// <summary>Applies the look of a condition; cheap to call every frame, it only acts on a change. Frozen on a corpse.</summary>
        public void ShowCondition(PartCondition condition)
        {
            if (_corpse || (_hasShown && condition == _shown))
            {
                return;
            }

            _hasShown = true;
            _shown = condition;
            _lost = condition == PartCondition.Lost;
            _conditionScale = condition == PartCondition.Wounded ? WoundedScale : 1f;
            _block.Clear();
            if (condition == PartCondition.Wounded)
            {
                Color darker = _renderer.sharedMaterial.color * WoundedDarkening;
                darker.a = 1f;
                _block.SetColor(BaseColorId, darker);
            }

            _renderer.SetPropertyBlock(_block);
            _collider.enabled = !_lost;
            ApplyVisibility();
            ApplyScale();
        }

        internal void Initialize(DemonView owner, int partIndex, Material material)
        {
            Owner = owner;
            PartIndex = partIndex;
            _renderer.sharedMaterial = material;
            gameObject.layer = Layers.Demon;
            _baseScale = transform.localScale;
            _corpse = false;
            _hasShown = false;
            ShowCondition(PartCondition.Healthy);
        }

        /// <summary>Called after the owner sized the body, so the healthy scale is the sized one.</summary>
        internal void RememberBaseScale()
        {
            _baseScale = transform.localScale;
            ApplyScale();
        }

        /// <summary>Scale factor of the attack pulse; 1 when idle.</summary>
        internal void SetPulse(float factor)
        {
            if (factor != _pulse)
            {
                _pulse = factor;
                ApplyScale();
            }
        }

        /// <summary>Hides the part for the first-person camera; a lost part stays hidden either way.</summary>
        internal void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyVisibility();
        }

        /// <summary>
        /// Freezes the part as dead flesh: the corpse material, the Food layer, visible with its collider on, so the
        /// corpse can be seen and the eat aim can find it even though the destroyed core counts as lost.
        /// </summary>
        internal void ShowAsCorpse(Material material)
        {
            _corpse = true;
            _lost = false;
            _conditionScale = 1f;
            _renderer.sharedMaterial = material;
            _block.Clear();
            _renderer.SetPropertyBlock(_block);
            gameObject.layer = Layers.Food;
            _collider.enabled = true;
            ApplyVisibility();
            ApplyScale();
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _block = new MaterialPropertyBlock();
        }

        private void ApplyVisibility()
        {
            _renderer.enabled = _visible && !_lost;
        }

        private void ApplyScale()
        {
            transform.localScale = _baseScale * (_conditionScale * _pulse);
        }
    }
}
