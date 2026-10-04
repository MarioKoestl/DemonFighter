#nullable enable
using UnityEngine;

namespace DemonFighter.UI
{
    /// <summary>
    /// Which first-run hints this installation has shown (GAME_DESIGN, "UI"; D-076). PlayerPrefs is the right size
    /// for four flags; the Settings box of the main menu can reset them.
    /// </summary>
    public static class HintMemory
    {
        public const string FirstCorpse = "corpse";
        public const string FirstBiomass = "biomass";
        public const string FirstPoints = "points";
        public const string FirstEvolution = "evolution";

        private const string Prefix = "hint.";

        /// <summary>Every hint id, for resetting.</summary>
        public static readonly string[] All = { FirstCorpse, FirstBiomass, FirstPoints, FirstEvolution };

        /// <summary>True once the hint was shown on this installation.</summary>
        public static bool WasShown(string id)
        {
            return PlayerPrefs.GetInt(Prefix + id, 0) != 0;
        }

        /// <summary>Remembers that the hint was shown.</summary>
        public static void MarkShown(string id)
        {
            PlayerPrefs.SetInt(Prefix + id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Forgets every hint, so the next run shows them again.</summary>
        public static void Reset()
        {
            for (int i = 0; i < All.Length; i++)
            {
                PlayerPrefs.DeleteKey(Prefix + All[i]);
            }

            PlayerPrefs.Save();
        }
    }
}
