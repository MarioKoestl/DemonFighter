#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// Slides the crust texture of a lava pool along (D-083) by moving the texture offset of this renderer only, through
    /// a MaterialPropertyBlock, so the shared lava material and every other pool stay untouched.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class LavaFlow : MonoBehaviour
    {
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        private Renderer _renderer = null!;
        private MaterialPropertyBlock _block = null!;
        private Vector2 _metersPerSecond;
        private float _tiling = 1f;
        private Vector2 _offset;

        /// <summary>Sets the drift in texture tiles per second and how many tiles the pool spans.</summary>
        public void Configure(Vector2 tilesPerSecond, float tiling)
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            _metersPerSecond = tilesPerSecond;
            _tiling = Mathf.Max(0.01f, tiling);
            _offset = new Vector2(Random.value, Random.value);
            Apply();
        }

        private void Update()
        {
            if (_renderer == null)
            {
                return;
            }

            _offset += _metersPerSecond * Time.deltaTime;
            _offset = new Vector2(Mathf.Repeat(_offset.x, 1f), Mathf.Repeat(_offset.y, 1f));
            Apply();
        }

        private void Apply()
        {
            _renderer.GetPropertyBlock(_block);
            _block.SetVector(BaseMapStId, new Vector4(_tiling, _tiling, _offset.x, _offset.y));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
