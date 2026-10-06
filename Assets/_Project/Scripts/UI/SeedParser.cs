#nullable enable
using System.Globalization;

namespace DemonFighter.UI
{
    /// <summary>
    /// Reads the optional seed field of the main menu (GAME_DESIGN, "UI"; D-085): empty means a random run, a positive
    /// number is used as is, and any other text becomes a seed by a stable hash, so "mario" is the same cavern on
    /// every machine and a seed shared in chat works whatever is typed.
    /// </summary>
    public static class SeedParser
    {
        private const uint FnvOffset = 2166136261;
        private const uint FnvPrime = 16777619;

        /// <summary>The seed for the text, or null when the text is blank.</summary>
        public static int? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string trimmed = text!.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) && number > 0)
            {
                return number;
            }

            return Hash(trimmed);
        }

        /// <summary>A positive, process-independent hash of the text (FNV-1a, folded into the positive range, never zero).</summary>
        public static int Hash(string text)
        {
            uint hash = FnvOffset;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= FnvPrime;
            }

            int folded = (int)(hash & 0x7FFFFFFF);
            return folded == 0 ? 1 : folded;
        }
    }
}
