#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// A chain of bones generated for a part model that comes without any (D-098): along a straight line through the
    /// model, from the end that meets the body to the far end, with the joints at given shares of that line, each
    /// vertex weighted to the bone it lies on and blended into the next near a joint. Tails, arms and single legs bend
    /// this way without a hand-made rig. The layout is in the mesh's own units; skinned copies are built once and shared.
    /// </summary>
    public sealed class PartSkeleton
    {
        private const float JointBlend = 0.35f;
        private const float EndSlice = 0.04f;

        private static readonly Dictionary<string, Mesh> Skinned = new Dictionary<string, Mesh>();

        private readonly float[] _joints;

        private PartSkeleton(Vector3 root, Vector3 tip, float[] joints)
        {
            Root = root;
            Length = Vector3.Distance(root, tip);
            Direction = (tip - root) / Length;
            Vector3 hint = Mathf.Abs(Vector3.Dot(Direction, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right;
            Rotation = Quaternion.LookRotation(Direction, hint);
            _joints = joints;
        }

        /// <summary>Where the chain starts, in mesh units: where the part meets the body.</summary>
        public Vector3 Root { get; }

        /// <summary>The unit direction from the root to the far end.</summary>
        public Vector3 Direction { get; }

        /// <summary>The distance from the root to the far end.</summary>
        public float Length { get; }

        /// <summary>The far end of the chain in mesh units: the tip of a tail, the claws, the sole of a foot.</summary>
        public Vector3 Tip => Root + (Direction * Length);

        /// <summary>How many bones the chain has.</summary>
        public int Bones => _joints.Length;

        /// <summary>The rest turn of every bone in mesh space: +Z along the chain.</summary>
        public Quaternion Rotation { get; }

        /// <summary>A chain of even bones from the root to the point of the model farthest from it, as for a tail.</summary>
        public static PartSkeleton? For(Mesh mesh, Vector3 root, int bones)
        {
            if (bones < 1)
            {
                return null;
            }

            var joints = new float[bones];
            for (int i = 0; i < bones; i++)
            {
                joints[i] = i / (float)bones;
            }

            return Reaching(mesh, root, joints);
        }

        /// <summary>A chain from the root to the farthest point with the joints at the given shares of its length, as for an arm.</summary>
        public static PartSkeleton? Reaching(Mesh mesh, Vector3 root, float[] joints)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            if (!Valid(joints) || !mesh.isReadable)
            {
                return null;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3 tip = root;
            float farthest = 0f;
            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = (vertices[i] - root).sqrMagnitude;
                if (distance > farthest)
                {
                    farthest = distance;
                    tip = vertices[i];
                }
            }

            return farthest > 0f ? new PartSkeleton(root, tip, (float[])joints.Clone()) : null;
        }

        /// <summary>
        /// A chain down a standing model, as for a leg (D-099): from the middle of its top, the hip, to the middle of its
        /// bottom, the sole, along the model's up.
        /// </summary>
        public static PartSkeleton? Standing(Mesh mesh, Vector3 up, float[] joints)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            if (!Valid(joints) || !mesh.isReadable || mesh.vertexCount == 0)
            {
                return null;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3 axis = up.normalized;
            float low = float.MaxValue;
            float high = float.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                float height = Vector3.Dot(vertices[i], axis);
                low = Mathf.Min(low, height);
                high = Mathf.Max(high, height);
            }

            float slice = (high - low) * EndSlice;
            Vector3 top = Vector3.zero;
            Vector3 bottom = Vector3.zero;
            int topCount = 0;
            int bottomCount = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                float height = Vector3.Dot(vertices[i], axis);
                if (height >= high - slice)
                {
                    top += vertices[i];
                    topCount++;
                }

                if (height <= low + slice)
                {
                    bottom += vertices[i];
                    bottomCount++;
                }
            }

            if (topCount == 0 || bottomCount == 0 || high - low <= 0f)
            {
                return null;
            }

            return new PartSkeleton(top / topCount, bottom / bottomCount, (float[])joints.Clone());
        }

        /// <summary>The start of bone <paramref name="index"/> in mesh units.</summary>
        public Vector3 Joint(int index)
        {
            return Root + (Direction * (Length * _joints[index]));
        }

        /// <summary>The bone a mesh point follows, blended into the next one near a joint, by its place along the chain.</summary>
        public BoneWeight WeightOf(Vector3 point)
        {
            float along = Mathf.Clamp01(Vector3.Dot(point - Root, Direction) / Length);
            int bone = 0;
            for (int i = 1; i < _joints.Length; i++)
            {
                if (along >= _joints[i])
                {
                    bone = i;
                }
            }

            // Near the joint after this bone, or the one before it, the point follows both.
            if (bone + 1 < _joints.Length)
            {
                float joint = _joints[bone + 1];
                float width = JointBlend * Mathf.Min(LengthOf(bone), LengthOf(bone + 1));
                if (along >= joint - width)
                {
                    return Blend(bone, bone + 1, (along - (joint - width)) / (2f * width));
                }
            }

            if (bone > 0)
            {
                float joint = _joints[bone];
                float width = JointBlend * Mathf.Min(LengthOf(bone - 1), LengthOf(bone));
                if (along <= joint + width)
                {
                    return Blend(bone - 1, bone, (along - (joint - width)) / (2f * width));
                }
            }

            return new BoneWeight { boneIndex0 = bone, weight0 = 1f };
        }

        /// <summary>The bind poses of the chain: from mesh space into the space of each bone at rest.</summary>
        public Matrix4x4[] BindPoses()
        {
            var poses = new Matrix4x4[Bones];
            for (int i = 0; i < Bones; i++)
            {
                poses[i] = Matrix4x4.TRS(Joint(i), Rotation, Vector3.one).inverse;
            }

            return poses;
        }

        /// <summary>A copy of a mesh weighted to this chain, built once per mesh and layout; null for an unreadable mesh.</summary>
        public Mesh? SkinnedCopyOf(Mesh mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            string key = mesh.GetInstanceID().ToString(CultureInfo.InvariantCulture) + "|" + Root.ToString("F5") + "|" + Direction.ToString("F5") + "|" + Length.ToString("F5", CultureInfo.InvariantCulture) + "|" + string.Join(",", _joints);
            if (Skinned.TryGetValue(key, out Mesh? cached) && cached != null)
            {
                return cached;
            }

            if (!mesh.isReadable)
            {
                return null;
            }

            Mesh copy = UnityEngine.Object.Instantiate(mesh);
            copy.name = mesh.name + " (skinned)";
            Vector3[] vertices = copy.vertices;
            var weights = new BoneWeight[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                weights[i] = WeightOf(vertices[i]);
            }

            copy.boneWeights = weights;
            copy.bindposes = BindPoses();
            Skinned[key] = copy;
            return copy;
        }

        /// <summary>Creates the bone transforms under the part, each the child of the one before, at rest.</summary>
        public Transform[] CreateBones(Transform part)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            var bones = new Transform[Bones];
            Transform parent = part;
            for (int i = 0; i < Bones; i++)
            {
                var bone = new GameObject("Bone " + i).transform;
                bone.SetParent(parent, false);
                if (i == 0)
                {
                    bone.localPosition = Root;
                    bone.localRotation = Rotation;
                }
                else
                {
                    bone.localPosition = Vector3.forward * (Length * (_joints[i] - _joints[i - 1]));
                    bone.localRotation = Quaternion.identity;
                }

                bones[i] = bone;
                parent = bone;
            }

            return bones;
        }

        private static bool Valid(float[] joints)
        {
            if (joints == null || joints.Length == 0 || joints[0] != 0f)
            {
                return false;
            }

            for (int i = 1; i < joints.Length; i++)
            {
                if (joints[i] <= joints[i - 1] || joints[i] >= 1f)
                {
                    return false;
                }
            }

            return true;
        }

        private static BoneWeight Blend(int first, int second, float toSecond)
        {
            float blend = Mathf.Clamp01(toSecond);
            blend = blend * blend * (3f - (2f * blend));
            return new BoneWeight { boneIndex0 = first, weight0 = 1f - blend, boneIndex1 = second, weight1 = blend };
        }

        private float LengthOf(int bone)
        {
            float end = bone + 1 < _joints.Length ? _joints[bone + 1] : 1f;
            return end - _joints[bone];
        }
    }
}
