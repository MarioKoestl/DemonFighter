#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// The asset database chores every generator needs: folders, create-if-missing, required loads and serialized
    /// references written the way the Inspector writes them.
    /// </summary>
    internal static class EditorAssets
    {
        /// <summary>Creates the folder chain below Assets when it does not exist yet.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>
        /// Loads the asset or creates it and runs the initializer once. Existing assets keep their Inspector values,
        /// so re-running a generator never undoes tuning.
        /// </summary>
        public static T LoadOrCreate<T>(string path, Action<T>? initialize = null)
            where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(created);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        /// <summary>Loads an asset that must exist; a clear error names the generator that creates it.</summary>
        public static T LoadRequired<T>(string path, string createdBy)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new IOException("Missing asset " + path + "; run " + createdBy + " first.");
            }

            return asset;
        }

        /// <summary>Sets a serialized object reference by field name, failing loudly when the field is unknown.</summary>
        public static void SetReference(Object target, string propertyName, Object value)
        {
            using var serialized = new SerializedObject(target);
            SerializedProperty? property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new IOException(target.GetType().Name + " has no serialized property named " + propertyName + ".");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Every asset of the type below Assets, ordered by path so results are stable.</summary>
        public static T[] FindAll<T>()
            where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Array.Sort(paths, StringComparer.Ordinal);
            var assets = new T[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
            }

            return assets;
        }
    }
}
