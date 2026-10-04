#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Presentation
{
    /// <summary>
    /// The physics layers the views rely on: body parts live on Demon so aim rays find them, food on Food so the eat
    /// aim finds it. The placeholder asset generator creates both layers; a missing layer fails loudly here.
    /// </summary>
    public static class Layers
    {
        public const string DemonLayerName = "Demon";
        public const string FoodLayerName = "Food";

        /// <summary>Layer index of body parts.</summary>
        public static int Demon => Require(DemonLayerName);

        /// <summary>Layer index of corpses and severed parts.</summary>
        public static int Food => Require(FoodLayerName);

        /// <summary>Mask that hits only body parts.</summary>
        public static int DemonMask => 1 << Demon;

        /// <summary>Mask that hits only food.</summary>
        public static int FoodMask => 1 << Food;

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
