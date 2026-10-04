#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Presentation
{
    /// <summary>
    /// The layers the views rely on: body parts live on Demon so aim rays find them, food on Food so the eat aim
    /// finds it, and the body preview of the menu on Preview so only its own camera sees it. The placeholder asset
    /// generator creates the layers; a missing layer fails loudly here.
    /// </summary>
    public static class Layers
    {
        public const string DemonLayerName = "Demon";
        public const string FoodLayerName = "Food";
        public const string PreviewLayerName = "Preview";

        /// <summary>Layer index of body parts.</summary>
        public static int Demon => Require(DemonLayerName);

        /// <summary>Layer index of corpses and severed parts.</summary>
        public static int Food => Require(FoodLayerName);

        /// <summary>Layer index of the body preview stage of the mutation menu.</summary>
        public static int Preview => Require(PreviewLayerName);

        /// <summary>Mask that hits only body parts.</summary>
        public static int DemonMask => 1 << Demon;

        /// <summary>Mask that hits only food.</summary>
        public static int FoodMask => 1 << Food;

        /// <summary>Mask of the preview stage.</summary>
        public static int PreviewMask => 1 << Preview;

        private static int Require(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer < 0)
            {
                throw new InvalidOperationException("Layer " + name + " is missing; run Demon Fighter > Generate > Placeholder Assets.");
            }

            return layer;
        }
    }
}
