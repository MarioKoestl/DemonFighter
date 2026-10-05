#nullable enable
using System;
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

        /// <summary>
        /// The lowest height of the mesh after the transform: exact from its vertices when the mesh is readable (the
        /// body part models are), from the corners of its bounds otherwise, which can reach a little lower.
        /// </summary>
        public static float LowestY(Mesh mesh, Matrix4x4 matrix)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            Vector3[] vertices = mesh.isReadable ? mesh.vertices : Array.Empty<Vector3>();
            if (vertices.Length == 0)
            {
                return Transform(mesh.bounds, matrix).min.y;
            }

            float lowest = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                lowest = Mathf.Min(lowest, matrix.MultiplyPoint3x4(vertices[i]).y);
            }

            return lowest;
        }
    }
}
