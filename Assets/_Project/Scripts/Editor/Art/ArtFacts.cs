#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Editor.Art
{
    /// <summary>What the validator knows about one model file, gathered from the asset database so the rules stay plain and testable.</summary>
    internal sealed record ModelFacts
    {
        public string Path { get; init; } = string.Empty;

        /// <summary>File name without extension, the name the convention reads.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Triangles of the largest state mesh in the file.</summary>
        public int Triangles { get; init; }

        public int TriangleBudget { get; init; }

        /// <summary>The damage states the file delivers, by its own suffix or by the names of its meshes.</summary>
        public IReadOnlyList<MeshState> States { get; init; } = Array.Empty<MeshState>();

        /// <summary>Names of the Socket_ transforms in the file.</summary>
        public IReadOnlyList<string> Sockets { get; init; } = Array.Empty<string>();

        /// <summary>Bounds of the intact mesh in model space; empty when the file has none.</summary>

        public bool HasLicenseEntry { get; init; }
    }

    /// <summary>What the validator knows about one texture under the art folder.</summary>
    internal sealed record TextureFacts
    {
        public string Path { get; init; } = string.Empty;

        public int MaxSize { get; init; }

        public int Budget { get; init; }
    }

    /// <summary>The parts the content knows, so a model name can be checked against them.</summary>
    internal sealed record PartFacts
    {
        public string Id { get; init; } = string.Empty;

        public PartFate Fate { get; init; }

        public bool IsCore { get; init; }
    }
}
