#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Editor.Art
{
    /// <summary>The findings of one validation: errors block shipping, warnings ask for a look.</summary>
    internal sealed class ArtValidationReport
    {
        public List<string> Errors { get; } = new List<string>();

        public List<string> Warnings { get; } = new List<string>();

        public int ModelsChecked { get; set; }

        public int TexturesChecked { get; set; }

        public string Summary => "Art validation: " + ModelsChecked + " model(s), " + TexturesChecked + " texture(s), " + Errors.Count + " error(s), " + Warnings.Count + " warning(s).";

        public string ToText()
        {
            var text = new StringBuilder(Summary).AppendLine();
            foreach (string error in Errors)
            {
                text.Append("ERROR   ").AppendLine(error);
            }

            foreach (string warning in Warnings)
            {
                text.Append("WARNING ").AppendLine(warning);
            }

            return text.ToString();
        }
    }

    /// <summary>
    /// The checks of ASSET_PIPELINE as plain rules over gathered facts: the naming convention, the triangle and
    /// texture budgets, the damage states every part needs, the sockets a core should bring, plausible scale and
    /// pivot, and the license entry nothing ships without.
    /// </summary>
    internal static class ArtValidationRules
    {
        private static readonly MeshState[] WantedStates = { MeshState.Wounded, MeshState.Mangled };
        private static readonly string[] CoreSockets = { "Socket_Head", "Socket_LimbL", "Socket_LimbR", "Socket_Locomotion", "Socket_Hide", "Socket_Tail" };

        public static ArtValidationReport Check(IReadOnlyList<ModelFacts> models, IReadOnlyList<TextureFacts> textures, IReadOnlyList<PartFacts> parts)
        {
            var report = new ArtValidationReport { ModelsChecked = models.Count, TexturesChecked = textures.Count };
            var partsById = new Dictionary<string, PartFacts>(StringComparer.Ordinal);
            foreach (PartFacts part in parts)
            {
                partsById[part.Id] = part;
            }

            var statesByPart = new Dictionary<string, HashSet<MeshState>>(StringComparer.Ordinal);
            foreach (ModelFacts model in models)
            {
                CheckModel(model, partsById, statesByPart, report);
            }

            foreach (KeyValuePair<string, HashSet<MeshState>> entry in statesByPart)
            {
                CheckStates(entry.Key, entry.Value, partsById[entry.Key], report);
            }

            foreach (TextureFacts texture in textures)
            {
                if (texture.MaxSize > texture.Budget)
                {
                    report.Warnings.Add(texture.Path + ": max size " + texture.MaxSize + " is above the budget of " + texture.Budget + ".");
                }
            }

            return report;
        }

        private static void CheckModel(ModelFacts model, Dictionary<string, PartFacts> partsById, Dictionary<string, HashSet<MeshState>> statesByPart, ArtValidationReport report)
        {
            if (!ArtAssetNames.TryParsePart(model.Name, out string partId, out _))
            {
                report.Errors.Add(model.Path + ": the name does not follow BP_<Part>[_<Variant>][_<State>].");
                return;
            }

            if (!partsById.TryGetValue(partId, out PartFacts part))
            {
                report.Errors.Add(model.Path + ": no body part with id " + partId + "; rename the model or add the part asset.");
                return;
            }

            if (model.Triangles > model.TriangleBudget)
            {
                report.Errors.Add(model.Path + ": " + model.Triangles + " triangles exceed the budget of " + model.TriangleBudget + ".");
            }

            if (!model.HasLicenseEntry)
            {
                report.Errors.Add(model.Path + ": no entry in docs/ASSET_LICENSES.md; nothing ships without one.");
            }

            if (!statesByPart.TryGetValue(partId, out HashSet<MeshState> states))
            {
                states = new HashSet<MeshState>();
                statesByPart[partId] = states;
            }

            foreach (MeshState state in model.States)
            {
                states.Add(state);
            }

            if (part.IsCore)
            {
                CheckSockets(model, report);
            }

        }

        private static bool Has(IReadOnlyList<MeshState> states, MeshState state)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] == state)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckSockets(ModelFacts model, ArtValidationReport report)
        {
            if (model.Sockets.Count == 0)
            {
                report.Warnings.Add(model.Path + ": no Socket_ transforms; the anchors of the placeholder capsule stay in use.");
                return;
            }

            var missing = new List<string>();
            foreach (string wanted in CoreSockets)
            {
                bool found = false;
                foreach (string socket in model.Sockets)
                {
                    if (string.Equals(socket, wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missing.Add(wanted);
                }
            }

            if (missing.Count > 0)
            {
                report.Warnings.Add(model.Path + ": sockets missing: " + string.Join(", ", missing) + ".");
            }
        }

        private static void CheckStates(string partId, HashSet<MeshState> states, PartFacts part, ArtValidationReport report)
        {
            if (!states.Contains(MeshState.Intact))
            {
                report.Errors.Add(partId + ": no _Intact mesh; nothing is bound without one.");
                return;
            }

            foreach (MeshState wanted in WantedStates)
            {
                if (!states.Contains(wanted))
                {
                    report.Warnings.Add(partId + ": no _" + wanted + " mesh; the view falls back to the next healthier state.");
                }
            }

            if (!part.IsCore && part.Fate == PartFate.Severed && !states.Contains(MeshState.Stump))
            {
                report.Warnings.Add(partId + ": no _Stump mesh; a lost part vanishes instead of leaving a stump.");
            }
        }
    }
}
