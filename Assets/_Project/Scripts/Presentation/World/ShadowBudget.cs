#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// Lets only the few point lights nearest the camera cast shadows (D-083): a cavern has dozens of lava and fissure
    /// lights, and every shadowed light costs a shadow map, so the budget follows the player around. The world
    /// builder hands it the lights; it re-picks twice a second.
    /// </summary>
    public sealed class ShadowBudget : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;

        private readonly List<Light> _lights = new List<Light>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private int[] _nearest = Array.Empty<int>();
        private int _budget;
        private float _nextRefresh;
        private Camera? _camera;

        /// <summary>Takes the lights to budget and how many of them may cast shadows at once.</summary>
        public void Configure(IReadOnlyList<Light> lights, int budget)
        {
            if (lights == null)
            {
                throw new ArgumentNullException(nameof(lights));
            }

            _lights.Clear();
            _lights.AddRange(lights);
            _budget = Mathf.Max(0, budget);
            _nearest = new int[Mathf.Min(_budget, _lights.Count)];
            _nextRefresh = 0f;
            for (int i = 0; i < _lights.Count; i++)
            {
                _lights[i].shadows = LightShadows.None;
            }
        }

        /// <summary>
        /// Indices of the nearest points to the origin, nearest first, as many as fit the result; fewer when there
        /// are fewer points. Returns how many were written.
        /// </summary>
        public static int SelectNearest(IReadOnlyList<Vector3> points, Vector3 origin, int[] result)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            int count = 0;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = (points[i] - origin).sqrMagnitude;
                int slot = count < result.Length ? count : -1;

                // Find where this point belongs among the kept ones; the list stays sorted, nearest first.
                int insertAt = slot;
                for (int j = 0; j < count; j++)
                {
                    if (distance < (points[result[j]] - origin).sqrMagnitude)
                    {
                        insertAt = j;
                        break;
                    }
                }

                if (insertAt < 0)
                {
                    continue;
                }

                int last = Mathf.Min(count, result.Length - 1);
                for (int j = last; j > insertAt; j--)
                {
                    result[j] = result[j - 1];
                }

                result[insertAt] = i;
                if (count < result.Length)
                {
                    count++;
                }
            }

            return count;
        }

        private void Update()
        {
            if (_lights.Count == 0 || Time.time < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.time + RefreshSeconds;
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            _positions.Clear();
            for (int i = 0; i < _lights.Count; i++)
            {
                _positions.Add(_lights[i].transform.position);
            }

            int shadowed = SelectNearest(_positions, _camera.transform.position, _nearest);
            for (int i = 0; i < _lights.Count; i++)
            {
                _lights[i].shadows = LightShadows.None;
            }

            for (int i = 0; i < shadowed; i++)
            {
                _lights[_nearest[i]].shadows = LightShadows.Soft;
            }
        }
    }
}
