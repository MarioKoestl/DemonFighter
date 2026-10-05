#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Content;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonFighter.Editor.Art
{
    /// <summary>
    /// Reads imported models under the art folder the way the binder and the validator need them: which damage
    /// state meshes a file delivers, with their materials and their placement in the file, its top object included
    /// (D-088), which sockets it carries, and how heavy it is.
    /// </summary>
    internal static class ArtModelAssets
    {
        private const string ModelFilter = "t:Model";

        /// <summary>One damage state mesh of a model with the material and the placement its object had in the file.</summary>
        internal sealed class StateMesh
        {
            public StateMesh(Mesh mesh, Material? material, Matrix4x4 localToModel)
            {
                Mesh = mesh;
                Material = material;
                LocalToModel = localToModel;
                BoundsInModel = MeshBounds.Transform(mesh.bounds, localToModel);
            }

            public Mesh Mesh { get; }

            public Material? Material { get; }

            /// <summary>
            /// Transform of the mesh object in the file, its top object included: a Meshy or Blender export carries its
            /// upright turn and centimeter scale here, a single-object file on the top object itself (D-088).
            /// </summary>
            public Matrix4x4 LocalToModel { get; }

            public Bounds BoundsInModel { get; }
        }

        /// <summary>A socket transform of a model in model space.</summary>
        internal sealed class ModelSocket
        {
            public ModelSocket(string name, SocketKind kind, Vector3 position, Quaternion rotation)
            {
                Name = name;
                Kind = kind;
                Position = position;
                Rotation = rotation;
            }

            public string Name { get; }

            public SocketKind Kind { get; }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }
        }

        /// <summary>Everything one model file contributes.</summary>
        internal sealed class ModelContents
        {
            public ModelContents(string path, string name, Dictionary<MeshState, StateMesh> states, List<ModelSocket> sockets, int triangles)
            {
                Path = path;
                Name = name;
                States = states;
                Sockets = sockets;
                Triangles = triangles;
            }

            public string Path { get; }

            public string Name { get; }

            public Dictionary<MeshState, StateMesh> States { get; }

            public List<ModelSocket> Sockets { get; }

            /// <summary>Triangles of the largest state mesh.</summary>
            public int Triangles { get; }
        }

        /// <summary>Every model under the art models folder, ordered by path; empty when the folder does not exist yet.</summary>
        public static string[] FindModelPaths()
        {
            if (!AssetDatabase.IsValidFolder(ArtFolders.Models))
            {
                return Array.Empty<string>();
            }

            string[] guids = AssetDatabase.FindAssets(ModelFilter, new[] { ArtFolders.Models });
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is ModelImporter)
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths.ToArray();
        }

        /// <summary>
        /// Reads a model: a file whose name ends in a state delivers that state with its first mesh; otherwise every
        /// mesh object named after a state delivers that state, and a file with meshes but no state names delivers
        /// its first mesh as the intact one. Null when the path is no model.
        /// </summary>
        public static ModelContents? Load(string path)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                return null;
            }

            string name = Path.GetFileNameWithoutExtension(path);
            ArtAssetNames.TryParsePart(name, out _, out MeshState fileState);
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            var states = new Dictionary<MeshState, StateMesh>();
            if (fileState != MeshState.None)
            {
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh != null)
                    {
                        states[fileState] = Describe(filter);
                        break;
                    }
                }
            }
            else
            {
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh == null)
                    {
                        continue;
                    }

                    if (TryStateOf(filter.name, out MeshState state) && !states.ContainsKey(state))
                    {
                        states[state] = Describe(filter);
                    }
                }

                if (states.Count == 0)
                {
                    foreach (MeshFilter filter in filters)
                    {
                        if (filter.sharedMesh != null)
                        {
                            states[MeshState.Intact] = Describe(filter);
                            break;
                        }
                    }
                }
            }

            int triangles = 0;
            foreach (StateMesh state in states.Values)
            {
                triangles = Mathf.Max(triangles, TriangleCount(state.Mesh));
            }

            var sockets = new List<ModelSocket>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (ArtAssetNames.TryParseSocket(child.name, out SocketKind kind))
                {
                    sockets.Add(new ModelSocket(child.name, kind, child.position, child.rotation));
                }
            }

            sockets.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return new ModelContents(path, name, states, sockets, triangles);
        }

        /// <summary>Triangles of a mesh from its sub-mesh descriptors, which need no read/write access.</summary>
        public static int TriangleCount(Mesh mesh)
        {
            int indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                SubMeshDescriptor sub = mesh.GetSubMesh(i);
                indices += sub.topology == MeshTopology.Triangles ? sub.indexCount : 0;
            }

            return indices / 3;
        }

        // The top object of a model asset has no parent, so world space is the space of the file with the top object's
        // own transform included. Unity folds a single-object file into that top object, turn and scale too.
        private static StateMesh Describe(MeshFilter filter)
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            Material? material = renderer != null ? renderer.sharedMaterial : null;
            return new StateMesh(filter.sharedMesh, material, filter.transform.localToWorldMatrix);
        }

        // A mesh object may carry the whole convention (BP_Jaws_Wounded) or just the state (Wounded).
        private static bool TryStateOf(string objectName, out MeshState state)
        {
            if (ArtAssetNames.TryParsePart(objectName, out _, out state) && state != MeshState.None)
            {
                return true;
            }

            return ArtAssetNames.TryParseState(objectName, out state);
        }
    }
}
