#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>Bounds arithmetic the figure and the art binder share.</summary>
    public static class MeshBounds
    {
        /// <summary>The axis-aligned box that holds all eight corners of the bounds after the transform.</summary>
        public static Bounds Transform(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            var result = new Bounds(matrix.MultiplyPoint3x4(min), Vector3.zero);
            for (int corner = 1; corner < 8; corner++)
            {
                var point = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                result.Encapsulate(matrix.MultiplyPoint3x4(point));
            }

            return result;
        }
    }
}
