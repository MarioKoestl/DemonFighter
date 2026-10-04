#nullable enable
using System.IO;
using DemonFighter.Presentation;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Makes sure the layers the views need exist in the project settings (Demon for body parts, Food for corpses and
    /// severed parts, Preview for the body preview of the menu), written through the TagManager asset the way the
    /// Inspector does it.
    /// </summary>
    internal static class ProjectLayers
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private const string LayersProperty = "layers";
        private const int FirstUserLayer = 8;

        /// <summary>Adds the missing layers into the first free user slots; existing layers are left alone.</summary>
        public static void EnsureLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets.Length == 0)
            {
                throw new IOException("Could not load " + TagManagerPath + ".");
            }

            using var serialized = new SerializedObject(assets[0]);
            SerializedProperty? layers = serialized.FindProperty(LayersProperty);
            if (layers == null || !layers.isArray)
            {
                throw new IOException(TagManagerPath + " has no layers array.");
            }

            bool changed = Ensure(layers, Layers.DemonLayerName);
            changed |= Ensure(layers, Layers.FoodLayerName);
            changed |= Ensure(layers, Layers.PreviewLayerName);
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        private static bool Ensure(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == name)
                {
                    return false;
                }
            }

            for (int i = FirstUserLayer; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    element.stringValue = name;
                    return true;
                }
            }

            throw new IOException("No free user layer for " + name + ".");
        }
    }
}
