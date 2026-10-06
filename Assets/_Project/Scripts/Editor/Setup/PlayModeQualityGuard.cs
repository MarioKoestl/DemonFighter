#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Puts the project's quality settings back after Play Mode (D-085): the settings file switches the render pipeline
    /// asset and vsync at boot and from the settings menu, and in the editor Unity writes those switches into the
    /// project. The pipeline and vsync of the active quality level are remembered when Play Mode starts and restored
    /// when it ends, so testing a preset never shows up as a change to commit.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeQualityGuard
    {
        private const string PipelineKey = "DemonFighter.QualityGuard.Pipeline";
        private const string VSyncKey = "DemonFighter.QualityGuard.VSync";
        private const string SavedKey = "DemonFighter.QualityGuard.Saved";

        static PlayModeQualityGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                // Session state survives the domain reload of entering Play Mode; static fields would not.
                RenderPipelineAsset? pipeline = QualitySettings.renderPipeline;
                SessionState.SetString(PipelineKey, pipeline != null ? AssetDatabase.GetAssetPath(pipeline) : string.Empty);
                SessionState.SetInt(VSyncKey, QualitySettings.vSyncCount);
                SessionState.SetBool(SavedKey, true);
            }
            else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(SavedKey, false))
            {
                SessionState.SetBool(SavedKey, false);
                string path = SessionState.GetString(PipelineKey, string.Empty);
                RenderPipelineAsset? pipeline = path.Length > 0 ? AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path) : null;
                if (QualitySettings.renderPipeline != pipeline)
                {
                    QualitySettings.renderPipeline = pipeline;
                }

                int vSync = SessionState.GetInt(VSyncKey, QualitySettings.vSyncCount);
                if (QualitySettings.vSyncCount != vSync)
                {
                    QualitySettings.vSyncCount = vSync;
                }
            }
        }
    }
}
