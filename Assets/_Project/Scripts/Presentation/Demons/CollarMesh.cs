#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The flesh collar that grows a part out of the body (D-097): a flared sleeve whose base ring is wrapped onto the
    /// surface of the core and whose top ring closes around the root of the part, with a cap that shows as a stump
    /// once the part is gone. Skinned to two bones, the body and the part, so the top follows the part when it swings
    /// or grows and the base stays on the body. Everything is in rig space, body units.
    /// </summary>
    public static class CollarMesh
    {
        /// <summary>Vertices around each ring.</summary>
        public const int Segments = 20;

        /// <summary>Rings from the body to the part, both included.</summary>
        public const int Rings = 7;

        private const float RootSearchRadius = 0.15f;
        private const float OutsideMargin = 0.002f;
        private const float NormalPull = 0.4f;
        private const float Height = 0.07f;
        private const float MinimumHeight = 0.02f;
        private const float SliceBand = 0.015f;
        private const float MaxSliceBand = 0.06f;
        private const float SliceRadius = 0.3f;
        private const int MinimumSlicePoints = 6;
        private const float TopMargin = 1.02f;
        private const float TopPad = 0.002f;
        private const float LipDepth = 0.85f;
        private const float LipRise = 0.35f;
        private const float MinimumTopRadius = 0.005f;
        private const float FlarePerCollar = 0.06f;
        private const float WrapInset = 0.004f;
        private const float CapSink = 0.3f;

        /// <summary>
        /// Builds the collar of a part whose rest pose puts its mesh points where <paramref name="partPoints"/> says, its
        /// pivot at <paramref name="pivot"/>; null when the part does not leave the body near its pivot.
        /// </summary>
        public static CollarShape? Build(CoreSurface core, IReadOnlyList<Vector3> partPoints, Vector3 pivot, float collar, Matrix4x4 partToRig)
        {
            if (core == null)
            {
                throw new ArgumentNullException(nameof(core));
            }

            if (partPoints == null)
            {
                throw new ArgumentNullException(nameof(partPoints));
            }

            if (collar <= 0f || partPoints.Count == 0)
            {
                return null;
            }

            int start = core.Nearest(pivot);
            Vector3 surface = core.Point(start);
            Vector3 axis = DepartureAxis(core, partPoints, surface, core.Normal(start));
            Basis(axis, out Vector3 u, out Vector3 v);

            float height = Mathf.Max(Height * collar, MinimumHeight);
            if (!Slice(partPoints, surface, axis, height, out Vector3 sliceCenter, out List<Vector3> slice))
            {
                return null;
            }

            var top = new float[Segments];
            float topRadius = 0f;
            for (int i = 0; i < Segments; i++)
            {
                Vector3 direction = Direction(u, v, i);
                float extent = MinimumTopRadius;
                for (int p = 0; p < slice.Count; p++)
                {
                    extent = Mathf.Max(extent, Vector3.Dot(slice[p], direction));
                }

                top[i] = (extent * TopMargin) + TopPad;
                topRadius = Mathf.Max(topRadius, top[i]);
            }

            float flare = FlarePerCollar * collar;
            Vector3 topCenter = surface + (axis * height) + sliceCenter;
            Vector3 baseCenter = surface + sliceCenter;
            int count = (Rings * Segments) + 1;
            var vertices = new Vector3[count];
            var uvs = new Vector2[count];
            var weights = new BoneWeight[count];
            var baseNormals = new Vector3[Segments];
            float baseRadius = 0f;
            for (int i = 0; i < Segments; i++)
            {
                Vector3 direction = Direction(u, v, i);
                float wide = top[i] + flare;
                int wrapped = core.Nearest(baseCenter + (direction * wide));
                Vector3 bottom = core.Point(wrapped) - (core.Normal(wrapped) * WrapInset);
                Vector3 upper = topCenter + (direction * top[i]);
                Vector3 lip = topCenter + (axis * (height * LipRise)) + (direction * (top[i] * LipDepth));
                baseNormals[i] = core.Normal(wrapped);
                baseRadius = Mathf.Max(baseRadius, Vector3.Distance(bottom, baseCenter));
                for (int ring = 0; ring < Rings; ring++)
                {
                    // The rings before the last run from the body to the part; the last one tucks under the part's skin.
                    int index = (ring * Segments) + i;
                    float s = ring / (float)(Rings - 1);
                    if (ring == Rings - 1)
                    {
                        vertices[index] = lip;
                    }
                    else
                    {
                        float t = ring / (float)(Rings - 2);
                        float hug = ((1f - t) * (1f - t)) - (1f - t);
                        vertices[index] = Vector3.Lerp(bottom, upper, t) + (direction * ((wide - top[i]) * hug));
                    }

                    uvs[index] = new Vector2(i / (float)Segments, s);
                    float toPart = s * s * (3f - (2f * s));
                    weights[index] = Weights(toPart);
                }
            }

            int cap = count - 1;
            // Sunk into the part, so it never shows over it; once the part is gone it reads as the wound.
            vertices[cap] = topCenter - (axis * (topRadius * CapSink));
            uvs[cap] = new Vector2(0.5f, 1f);
            weights[cap] = Weights(1f);

            var mesh = new Mesh { name = "Collar" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = Triangles();
            mesh.boneWeights = weights;
            mesh.bindposes = new[] { Matrix4x4.identity, partToRig.inverse };
            mesh.RecalculateNormals();

            // The base takes the normals of the core it lies on, so light runs on across the joint.
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < Segments; i++)
            {
                normals[i] = baseNormals[i];
            }

            mesh.normals = normals;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return new CollarShape(mesh, baseCenter, baseRadius, topCenter, topRadius, axis);
        }

        // Where the part leaves the body: from the surface point toward the part points just outside it, leaning to the
        // surface normal so the sleeve stands out of the body rather than lying along it.
        private static Vector3 DepartureAxis(CoreSurface core, IReadOnlyList<Vector3> partPoints, Vector3 surface, Vector3 normal)
        {
            Vector3 sum = Vector3.zero;
            int outside = 0;
            float searchSquared = RootSearchRadius * RootSearchRadius;
            for (int i = 0; i < partPoints.Count; i++)
            {
                Vector3 point = partPoints[i];
                if ((point - surface).sqrMagnitude > searchSquared || core.SignedDistance(point) <= OutsideMargin)
                {
                    continue;
                }

                sum += point;
                outside++;
            }

            if (outside == 0)
            {
                return normal.normalized;
            }

            Vector3 departure = (sum / outside) - surface;
            Vector3 axis = departure.normalized + (normal.normalized * NormalPull);
            return axis.sqrMagnitude > 0.0001f ? axis.normalized : normal.normalized;
        }

        // The part points around the given height along the axis, as offsets from the centre of the cut in the plane
        // across the axis; the band widens for a coarse mesh.
        private static bool Slice(IReadOnlyList<Vector3> partPoints, Vector3 surface, Vector3 axis, float height, out Vector3 center, out List<Vector3> slice)
        {
            slice = new List<Vector3>();
            center = Vector3.zero;
            for (float band = SliceBand; band <= MaxSliceBand; band *= 2f)
            {
                slice.Clear();
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < partPoints.Count; i++)
                {
                    Vector3 offset = partPoints[i] - surface;
                    float along = Vector3.Dot(offset, axis);
                    Vector3 across = offset - (axis * along);
                    if (Mathf.Abs(along - height) <= band && across.magnitude <= SliceRadius)
                    {
                        slice.Add(across);
                        sum += across;
                    }
                }

                if (slice.Count >= MinimumSlicePoints)
                {
                    center = sum / slice.Count;
                    for (int i = 0; i < slice.Count; i++)
                    {
                        slice[i] -= center;
                    }

                    return true;
                }
            }

            return false;
        }

        private static void Basis(Vector3 axis, out Vector3 u, out Vector3 v)
        {
            Vector3 helper = Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right;
            u = Vector3.Cross(axis, helper).normalized;
            v = Vector3.Cross(axis, u);
        }

        private static Vector3 Direction(Vector3 u, Vector3 v, int segment)
        {
            float angle = segment * (Mathf.PI * 2f / Segments);
            return (u * Mathf.Cos(angle)) + (v * Mathf.Sin(angle));
        }

        private static BoneWeight Weights(float toPart)
        {
            return new BoneWeight { boneIndex0 = 0, weight0 = 1f - toPart, boneIndex1 = 1, weight1 = toPart };
        }

        // Quads between the rings facing away from the axis, then a fan over the top ring facing along it.
        private static int[] Triangles()
        {
            var triangles = new List<int>(((Rings - 1) * Segments * 6) + (Segments * 3));
            for (int ring = 0; ring < Rings - 1; ring++)
            {
                for (int i = 0; i < Segments; i++)
                {
                    int next = (i + 1) % Segments;
                    int a = (ring * Segments) + i;
                    int b = (ring * Segments) + next;
                    int c = ((ring + 1) * Segments) + i;
                    int d = ((ring + 1) * Segments) + next;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(c);
                }
            }

            int topRing = (Rings - 1) * Segments;
            int cap = Rings * Segments;
            for (int i = 0; i < Segments; i++)
            {
                triangles.Add(topRing + i);
                triangles.Add(topRing + ((i + 1) % Segments));
                triangles.Add(cap);
            }

            return triangles.ToArray();
        }
    }

    /// <summary>A built collar and the joint it covers, in rig space: where it meets the core and where it closes around the part.</summary>
    public sealed class CollarShape
    {
        public CollarShape(Mesh mesh, Vector3 baseCenter, float baseRadius, Vector3 topCenter, float topRadius, Vector3 axis)
        {
            Mesh = mesh;
            BaseCenter = baseCenter;
            BaseRadius = baseRadius;
            TopCenter = topCenter;
            TopRadius = topRadius;
            Axis = axis;
        }

        public Mesh Mesh { get; }

        /// <summary>The middle of the ring on the core; the core wears junction flesh around it.</summary>
        public Vector3 BaseCenter { get; }

        public float BaseRadius { get; }

        /// <summary>The middle of the ring around the part; the root of the part wears junction flesh around it.</summary>
        public Vector3 TopCenter { get; }

        public float TopRadius { get; }

        /// <summary>The direction the part leaves the body.</summary>
        public Vector3 Axis { get; }
    }
}
