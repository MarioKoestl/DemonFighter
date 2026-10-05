#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation.Demons;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Art
{
    /// <summary>
    /// Points every body part asset at the model meshes that carry its name (ASSET_PIPELINE, "Modular body parts"),
    /// so nobody drags meshes into Inspectors: BP_Jaws_Wounded.fbx becomes the wounded mesh of part.jaws, a core
    /// model with Socket_ transforms rewrites the socket anchors in body units. The turn and unit scale the file
    /// carries go underneath the fit knobs on every bind (D-088); the knobs start neutral on a first bind and are never
    /// overwritten afterwards. Runs from the menu and inside the placeholder
    /// generator; a part whose files disappeared is cleared again. Public because the command line has to find it.
    /// </summary>
    public static class ArtAssetBinder
    {
        internal const string MenuPath = "Demon Fighter/Art/Bind Art Assets";

        private const string SkinShaderName = "DemonFighter/DemonSkin";
        private const string MaterialExtension = ".mat";

        private sealed class PartModels
        {
            public Dictionary<MeshState, ArtModelAssets.StateMesh> States { get; } = new Dictionary<MeshState, ArtModelAssets.StateMesh>();

            public List<ArtModelAssets.ModelSocket> Sockets { get; } = new List<ArtModelAssets.ModelSocket>();

            public int Files { get; set; }
        }

        [MenuItem(MenuPath)]
        public static void Run()
        {
            int bound = Bind();
            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Bound art assets for " + bound + " body part(s).");
        }

        /// <summary>Binds every model under the art folder; returns how many parts received meshes. The caller saves.</summary>
        internal static int Bind()
        {
            var definitions = new Dictionary<string, BodyPartDefinition>(StringComparer.Ordinal);
            foreach (BodyPartDefinition definition in EditorAssets.FindAll<BodyPartDefinition>())
            {
                definitions[definition.Id] = definition;
            }

            var gathered = new Dictionary<string, PartModels>(StringComparer.Ordinal);
            foreach (string path in ArtModelAssets.FindModelPaths())
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!ArtAssetNames.TryParsePart(name, out string partId, out _))
                {
                    Log.Warn(LogCategory.Editor, path + " is ignored: the name does not follow BP_<Part>[_<Variant>][_<State>].");
                    continue;
                }

                if (!definitions.ContainsKey(partId))
                {
                    Log.Warn(LogCategory.Editor, path + " is ignored: no body part with id " + partId + ".");
                    continue;
                }

                ArtModelAssets.ModelContents? contents = ArtModelAssets.Load(path);
                if (contents == null)
                {
                    continue;
                }

                if (!gathered.TryGetValue(partId, out PartModels models))
                {
                    models = new PartModels();
                    gathered[partId] = models;
                }

                models.Files++;
                foreach (KeyValuePair<MeshState, ArtModelAssets.StateMesh> state in contents.States)
                {
                    if (models.States.ContainsKey(state.Key))
                    {
                        Log.Warn(LogCategory.Editor, path + " delivers a second " + state.Key + " mesh for " + partId + "; the later file by name wins.");
                    }

                    models.States[state.Key] = state.Value;
                }

                models.Sockets.AddRange(contents.Sockets);
            }

            int bound = 0;
            foreach (KeyValuePair<string, PartModels> entry in gathered)
            {
                if (BindPart(definitions[entry.Key], entry.Value))
                {
                    bound++;
                }
            }

            // A part whose model files are gone goes back to its primitive, so a deleted model never leaves a dangling reference.
            foreach (KeyValuePair<string, BodyPartDefinition> entry in definitions)
            {
                if (!gathered.ContainsKey(entry.Key))
                {
                    ClearPart(entry.Value);
                }
            }

            return bound;
        }

        private static bool BindPart(BodyPartDefinition definition, PartModels models)
        {
            if (!models.States.TryGetValue(MeshState.Intact, out ArtModelAssets.StateMesh intact))
            {
                Log.Warn(LogCategory.Editor, definition.Id + " has model files but no _Intact mesh; nothing is bound.");
                return false;
            }

            PartMeshSetDefinition meshes = definition.Visual.Meshes;
            bool firstBind = !meshes.HasMeshes;
            bool hadImportFix = meshes.HasImportFix;
            meshes.SetMeshes(intact.Mesh, MeshOf(models, MeshState.Wounded), MeshOf(models, MeshState.Mangled), MeshOf(models, MeshState.Stump), intact.Material);
            UseSkinShader(intact.Material);

            // The upright turn and centimeter scale a Meshy or Blender file carries go underneath the knobs, so the knobs
            // start neutral and mean "relative to how the model looked in the tool that made it" (D-088).
            Quaternion importRotation = intact.LocalToModel.rotation;
            if (firstBind)
            {
                meshes.SetFit(1f, meshes.Offset, Vector3.zero);
            }
            else if (!hadImportFix)
            {
                // Bound before the binder read the file's own turn: the turn moves out of the Euler knob and the look
                // stays. The old binder could not see the file's unit scale, so no Scale was tuned against it.
                Vector3 before = meshes.Euler;
                meshes.SetFit(1f, meshes.Offset, MigratedEuler(before, importRotation));
                Log.Info(LogCategory.Editor, definition.Id + ": the model file's own turn now sits under the Euler knob (D-088); Euler went from " + before + " to " + meshes.Euler + ".");
            }

            ImportFix(intact.Mesh.bounds, intact.LocalToModel, out float importScale, out Vector3 importPivot);
            meshes.SetImportFix(importScale, importRotation, importPivot);

            if (models.Sockets.Count > 0)
            {
                var anchors = new SocketAnchorDefinition[models.Sockets.Count];
                for (int i = 0; i < anchors.Length; i++)
                {
                    ArtModelAssets.ModelSocket socket = models.Sockets[i];
                    anchors[i] = new SocketAnchorDefinition();
                    anchors[i].Configure(socket.Kind, SocketAnchors.Normalize(socket.Position, intact.BoundsInModel), Clean(socket.Rotation.eulerAngles));
                }

                definition.Visual.SetAnchors(anchors);
            }

            EditorUtility.SetDirty(definition);
            Log.Info(LogCategory.Editor, "Bound " + models.States.Count + " state mesh(es) from " + models.Files + " file(s) to " + definition.Id + (models.Sockets.Count > 0 ? " with " + models.Sockets.Count + " socket(s)." : "."));
            return true;
        }

        // Bodies wear the skin shader so blood can soak them (D-081). The extracted material is a .mat asset of its own,
        // so its shader can change; the URP Lit properties carry over because the skin shader has the same ones.
        private static void UseSkinShader(Material? material)
        {
            if (material == null)
            {
                return;
            }

            Shader skin = Shader.Find(SkinShaderName);
            if (skin == null)
            {
                Log.Warn(LogCategory.Editor, "Shader " + SkinShaderName + " not found; " + material.name + " keeps its shader.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(material);
            if (material.shader == skin || !path.EndsWith(MaterialExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            material.shader = skin;
            EditorUtility.SetDirty(material);
            Log.Info(LogCategory.Editor, "Switched " + path + " to " + SkinShaderName + ".");
        }

        private static void ClearPart(BodyPartDefinition definition)
        {
            bool hadMeshes = definition.Visual.Meshes.HasMeshes;
            definition.Visual.Meshes.SetMeshes(null, null, null, null, null);
            EditorUtility.SetDirty(definition);
            if (hadMeshes)
            {
                Log.Info(LogCategory.Editor, definition.Id + " has no model files any more; it is drawn as a primitive again.");
            }
        }

        /// <summary>
        /// The import fix of a mesh (D-088): the file's own turn and unit scale, then the size that makes the model's
        /// longest side one unit as it stood in the tool, and the point of the raw mesh at the bottom center of that
        /// upright model, where the part meets the body. Meshy puts its origin sometimes at the bottom and sometimes in
        /// the middle, and its sizes vary; with this, neither matters.
        /// </summary>
        internal static void ImportFix(Bounds meshBounds, Matrix4x4 localToModel, out float scale, out Vector3 pivot)
        {
            Quaternion rotation = localToModel.rotation;
            float unitScale = UniformScale(localToModel);
            Bounds upright = MeshBounds.Transform(meshBounds, Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * unitScale));
            float longest = Mathf.Max(upright.size.x, Mathf.Max(upright.size.y, upright.size.z));
            scale = longest > 0f ? unitScale / longest : unitScale;
            var bottomCenter = new Vector3(upright.center.x, upright.min.y, upright.center.z);
            pivot = Quaternion.Inverse(rotation) * bottomCenter / unitScale;
        }

        /// <summary>The Euler knob that keeps the look once the file's own turn sits underneath it (D-088).</summary>
        internal static Vector3 MigratedEuler(Vector3 knobEuler, Quaternion importRotation)
        {
            return Clean((Quaternion.Euler(knobEuler) * Quaternion.Inverse(importRotation)).eulerAngles);
        }

        /// <summary>
        /// Euler angles come back in [0, 360) with float noise: -90 as 270, no turn as 359.99997 or -0. Brought into
        /// (-180, 180] and rounded to a thousandth of a degree, the asset file shows what was meant.
        /// </summary>
        internal static Vector3 Clean(Vector3 euler)
        {
            return new Vector3(CleanAngle(euler.x), CleanAngle(euler.y), CleanAngle(euler.z));
        }

        private static float CleanAngle(float degrees)
        {
            float rounded = Mathf.Round(Mathf.DeltaAngle(0f, degrees) * 1000f) / 1000f;
            return Mathf.Approximately(rounded, 0f) ? 0f : rounded;
        }

        private static float UniformScale(Matrix4x4 transform)
        {
            Vector3 scale = transform.lossyScale;
            float uniform = (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
            return uniform > 0f ? uniform : 1f;
        }

        private static Mesh? MeshOf(PartModels models, MeshState state)
        {
            return models.States.TryGetValue(state, out ArtModelAssets.StateMesh mesh) ? mesh.Mesh : null;
        }
    }
}
