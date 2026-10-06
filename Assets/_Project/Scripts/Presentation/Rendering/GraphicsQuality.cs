#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DemonFighter.Presentation.Rendering
{
    /// <summary>Switches the active URP pipeline asset to the one of a graphics preset (D-083); the settings menu calls it.</summary>
    public static class GraphicsQuality
    {
        /// <summary>Makes the pipeline of the preset the one Unity renders with from the next frame on.</summary>
        public static void Apply(GraphicsPreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (preset.Pipeline == null)
            {
                throw new ArgumentException("The preset " + preset.Name + " has no pipeline asset; run Demon Fighter > Generate > Placeholder Assets.", nameof(preset));
            }

            if (QualitySettings.renderPipeline != preset.Pipeline)
            {
                QualitySettings.renderPipeline = preset.Pipeline;
            }
        }

        /// <summary>The URP pipeline asset rendering right now, or null when another pipeline is active.</summary>
        public static UniversalRenderPipelineAsset? Current => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        /// <summary>True when the preset's pipeline is the one rendering right now.</summary>
        public static bool IsCurrent(GraphicsPreset preset)
        {
            return preset != null && preset.Pipeline != null && Current == preset.Pipeline;
        }
    }
}
