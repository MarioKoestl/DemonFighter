#nullable enable
using System;
using System.Text;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Editor.Art
{
    /// <summary>
    /// The naming convention of art assets (ASSET_PIPELINE, "Modular body parts"): a model BP_&lt;Part&gt;[_&lt;Variant&gt;]
    /// belongs to the part part.&lt;part&gt;[.&lt;variant&gt;], an optional last token names its damage state, and the
    /// empty transforms Socket_&lt;Kind&gt;[L|R] on a core model mark its sockets. The binder and the validator share it.
    /// </summary>
    internal static class ArtAssetNames
    {
        public const string PartPrefix = "BP_";
        public const string SocketPrefix = "Socket_";

        private const string IdPrefix = "part.";
        private const char NameSeparator = '_';
        private const char IdSeparator = '.';

        /// <summary>Parses BP_Hide_Thick_Wounded into part.hide.thick and Wounded; the state is None when the name has none. False outside the convention.</summary>
        public static bool TryParsePart(string name, out string partId, out MeshState state)
        {
            partId = string.Empty;
            state = MeshState.None;
            if (string.IsNullOrEmpty(name) || !name.StartsWith(PartPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] tokens = name.Substring(PartPrefix.Length).Split(new[] { NameSeparator }, StringSplitOptions.RemoveEmptyEntries);
            int count = tokens.Length;
            if (count > 0 && TryParseState(tokens[count - 1], out state))
            {
                count--;
            }

            if (count == 0)
            {
                state = MeshState.None;
                return false;
            }

            var builder = new StringBuilder(IdPrefix);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    builder.Append(IdSeparator);
                }

                builder.Append(tokens[i].ToLowerInvariant());
            }

            partId = builder.ToString();
            return true;
        }

        /// <summary>Parses one of Intact, Wounded, Mangled or Stump in any casing; digits never count as a state.</summary>
        public static bool TryParseState(string token, out MeshState state)
        {
            if (!string.IsNullOrEmpty(token) && !char.IsDigit(token[0]) && Enum.TryParse(token, true, out state) && state != MeshState.None)
            {
                return true;
            }

            state = MeshState.None;
            return false;
        }

        /// <summary>The model name a part id asks for: part.hide.thick becomes BP_Hide_Thick.</summary>
        public static string ModelNameFor(string partId)
        {
            if (string.IsNullOrEmpty(partId))
            {
                throw new ArgumentException("A part id is required.", nameof(partId));
            }

            string rest = partId.StartsWith(IdPrefix, StringComparison.Ordinal) ? partId.Substring(IdPrefix.Length) : partId;
            string[] tokens = rest.Split(new[] { IdSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder(PartPrefix);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(NameSeparator);
                }

                builder.Append(char.ToUpperInvariant(tokens[i][0]));
                builder.Append(tokens[i], 1, tokens[i].Length - 1);
            }

            return builder.ToString();
        }

        /// <summary>Parses Socket_Head or Socket_LimbL into the socket kind; false for other names and for a core socket.</summary>
        public static bool TryParseSocket(string name, out SocketKind kind)
        {
            kind = SocketKind.Core;
            if (string.IsNullOrEmpty(name) || !name.StartsWith(SocketPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string rest = name.Substring(SocketPrefix.Length);
            if (TryParseKind(rest, out kind))
            {
                return true;
            }

            if (rest.Length > 1 && (EndsWith(rest, 'L') || EndsWith(rest, 'R')) && TryParseKind(rest.Substring(0, rest.Length - 1), out kind))
            {
                return true;
            }

            kind = SocketKind.Core;
            return false;
        }

        private static bool TryParseKind(string token, out SocketKind kind)
        {
            if (token.Length > 0 && !char.IsDigit(token[0]) && Enum.TryParse(token, true, out kind) && kind != SocketKind.Core)
            {
                return true;
            }

            kind = SocketKind.Core;
            return false;
        }

        private static bool EndsWith(string text, char letter)
        {
            return char.ToUpperInvariant(text[text.Length - 1]) == letter;
        }
    }
}
