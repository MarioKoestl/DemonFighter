#nullable enable
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using DemonFighter.Simulation.Content;
using UnityEditor;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Finds every content definition in the project and writes the list into the catalog asset, so nobody edits
    /// the catalog by hand (ARCHITECTURE, "Content pipeline"). Building the catalog afterwards validates all content.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class ContentCatalogRebuilder
    {
        internal const string CatalogFolder = "Assets/_Project/Content/Catalog";
        internal const string CatalogPath = CatalogFolder + "/ContentCatalog.asset";
        internal const string MenuPath = "Demon Fighter/Rebuild Content Catalog";

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            EditorAssets.EnsureFolder(CatalogFolder);
            CombatTuningDefinition[] tunings = EditorAssets.FindAll<CombatTuningDefinition>();
            if (tunings.Length != 1)
            {
                throw new IOException("Exactly one Combat Tuning asset is required, found " + tunings.Length + ".");
            }

            ContentCatalogDefinition catalog = EditorAssets.LoadOrCreate<ContentCatalogDefinition>(CatalogPath);
            catalog.SetContent(
                tunings[0],
                EditorAssets.FindAll<BodyPartDefinition>(),
                EditorAssets.FindAll<SkillDefinition>(),
                EditorAssets.FindAll<DemonDefinition>());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            ContentCatalog built = catalog.Build();
            Log.Info(
                LogCategory.Editor,
                "Content catalog rebuilt: " + built.BodyParts.Count + " body parts, " + built.Skills.Count + " skills, " +
                built.Demons.Count + " demons.");
        }
    }
}
