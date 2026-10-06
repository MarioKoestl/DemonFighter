#nullable enable
using System.IO;
using DemonFighter.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Renderer features the game relies on, added to the URP renderer from code so a fresh clone renders right
    /// without Inspector clicks (rule 4). The decal feature lets blood decals draw (D-081). Public because the
    /// generator and the command line call it.
    /// </summary>
    public static class RenderPipelineSetup
    {
        internal const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string DecalFeatureName = "Decals";
        private const string FeaturesProperty = "m_RendererFeatures";
        private const string FeatureMapProperty = "m_RendererFeatureMap";

        /// <summary>Adds the URP decal renderer feature to the PC renderer once; true when it was added now.</summary>
        public static bool EnsureDecalFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            if (renderer == null)
            {
                throw new IOException("Missing " + PcRendererPath + "; the URP renderer asset of the project is gone.");
            }

            if (HasDecalFeature(renderer))
            {
                return false;
            }

            var feature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            feature.name = DecalFeatureName;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            // The renderer keeps a list of features and a map of their local ids side by side; the Inspector grows
            // both at once, so this does the same.
            using var serialized = new SerializedObject(renderer);
            SerializedProperty features = serialized.FindProperty(FeaturesProperty);
            SerializedProperty map = serialized.FindProperty(FeatureMapProperty);
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Added the decal renderer feature to " + PcRendererPath + ".");
            return true;
        }

        /// <summary>True when the PC renderer draws decals.</summary>
        public static bool HasDecalFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            return renderer != null && HasDecalFeature(renderer);
        }

        private static bool HasDecalFeature(UniversalRendererData renderer)
        {
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature is DecalRendererFeature)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
