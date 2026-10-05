#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DemonFighter.UI
{
    /// <summary>The resolution list of the graphics tab (D-085): distinct sizes, largest first, the current one always among them.</summary>
    public static class ResolutionOptions
    {
        private const string Separator = " x ";

        /// <summary>Labels like "1920 x 1080" for the sizes, deduplicated and sorted largest first, with the current size added when missing.</summary>
        public static List<string> Build(IEnumerable<Vector2Int> sizes, Vector2Int current, out int currentIndex)
        {
            if (sizes == null)
            {
                throw new ArgumentNullException(nameof(sizes));
            }

            var distinct = new List<Vector2Int>();
            foreach (Vector2Int size in sizes)
            {
                if (size.x > 0 && size.y > 0 && !distinct.Contains(size))
                {
                    distinct.Add(size);
                }
            }

            if (current.x > 0 && current.y > 0 && !distinct.Contains(current))
            {
                distinct.Add(current);
            }

            distinct.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
            var labels = new List<string>(distinct.Count);
            currentIndex = 0;
            for (int i = 0; i < distinct.Count; i++)
            {
                labels.Add(Label(distinct[i]));
                if (distinct[i] == current)
                {
                    currentIndex = i;
                }
            }

            return labels;
        }

        /// <summary>The label of a size.</summary>
        public static string Label(Vector2Int size)
        {
            return size.x.ToString(CultureInfo.InvariantCulture) + Separator + size.y.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Reads a label back into a size; false for anything that is not two numbers.</summary>
        public static bool TryParse(string? label, out Vector2Int size)
        {
            size = default;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            string[] parts = label.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2
                || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int height)
                || width <= 0 || height <= 0)
            {
                return false;
            }

            size = new Vector2Int(width, height);
            return true;
        }
    }
}
