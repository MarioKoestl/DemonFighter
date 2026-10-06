#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Grows the flesh collars of mesh parts (D-097) and does the work once per kind of joint: the surface of a core and
    /// the collar of a part in a pose are the same in body units for every body that wears them, so a hundred demons
    /// share a handful of collar meshes.
    /// </summary>
    internal static class PartCollars
    {
        private const float BoundsPadding = 0.3f;

        private static readonly Dictionary<string, CoreSurface?> Surfaces = new Dictionary<string, CoreSurface?>();
        private static readonly Dictionary<string, CollarShape?> Shapes = new Dictionary<string, CollarShape?>();

        /// <summary>The surface of a core mesh placed in rig space, and the key its collars are filed under; null when the mesh cannot be read.</summary>
        public static CoreSurface? SurfaceOf(Mesh coreMesh, Matrix4x4 coreToRig, out string key)
        {
            if (coreMesh == null)
            {
                throw new ArgumentNullException(nameof(coreMesh));
            }

            key = coreMesh.GetInstanceID().ToString(CultureInfo.InvariantCulture) + "|" + Text(coreToRig);
            if (!Surfaces.TryGetValue(key, out CoreSurface? surface))
            {
                surface = CoreSurface.From(coreMesh, coreToRig);
                Surfaces[key] = surface;
            }

            return surface;
        }

        /// <summary>The collar of a part mesh in its rest pose on a core, built on first use; null when the part does not leave the body there.</summary>
        public static CollarShape? ShapeFor(CoreSurface surface, string surfaceKey, Mesh partMesh, Matrix4x4 partToRig, Vector3 pivot, float collar)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            if (partMesh == null)
            {
                throw new ArgumentNullException(nameof(partMesh));
            }

            string key = surfaceKey + "|" + partMesh.GetInstanceID().ToString(CultureInfo.InvariantCulture) + "|" + Text(partToRig) + "|" + collar.ToString("F3", CultureInfo.InvariantCulture);

            // A cached mesh can be gone after a scene change unloaded it; Unity's null check sees that.
            if (Shapes.TryGetValue(key, out CollarShape? cached) && (cached == null || cached.Mesh != null))
            {
                return cached;
            }

            CollarShape? shape = partMesh.isReadable ? CollarMesh.Build(surface, RigPoints(partMesh, partToRig), pivot, collar, partToRig) : null;
            Shapes[key] = shape;
            return shape;
        }

        /// <summary>The renderer of a collar under the rig, skinned to the rig and to the part, wearing the core's material.</summary>
        public static SkinnedMeshRenderer Create(CollarShape shape, Transform rig, Transform part, Material material, int layer)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            var collarObject = new GameObject("Collar " + part.name) { layer = layer };
            collarObject.transform.SetParent(rig, false);
            SkinnedMeshRenderer renderer = collarObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = shape.Mesh;
            renderer.bones = new[] { rig, part };
            renderer.rootBone = rig;
            renderer.quality = SkinQuality.Bone2;
            renderer.sharedMaterial = material;
            Bounds bounds = shape.Mesh.bounds;
            bounds.Expand(BoundsPadding);
            renderer.localBounds = bounds;
            return renderer;
        }

        /// <summary>Forgets every cached surface and collar; for tests.</summary>
        internal static void Clear()
        {
            foreach (CollarShape? shape in Shapes.Values)
            {
                if (shape != null && shape.Mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(shape.Mesh);
                }
            }

            Shapes.Clear();
            Surfaces.Clear();
        }

        private static Vector3[] RigPoints(Mesh mesh, Matrix4x4 meshToRig)
        {
            Vector3[] points = mesh.vertices;
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = meshToRig.MultiplyPoint3x4(points[i]);
            }

            return points;
        }

        private static string Text(Matrix4x4 matrix)
        {
            var text = new System.Text.StringBuilder(160);
            for (int i = 0; i < 12; i++)
            {
                text.Append(matrix[i].ToString("F4", CultureInfo.InvariantCulture)).Append(',');
            }

            return text.ToString();
        }
    }
}
