#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Simulation.Anatomy;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Draws body parts in the placeholder stage (ASSET_PIPELINE, "Placeholder standard"): a primitive per part,
    /// placed on the body capsule as the part definition says, with a trigger collider on the Demon layer so hits can
    /// target it. Materials come from the palette by role; the core and plain flesh wear the owner material.
    /// </summary>
    public sealed class PartVisuals
    {
        private readonly ContentCatalogDefinition _catalog;

        public PartVisuals(ContentCatalogDefinition catalog, PlaceholderPalette palette)
        {
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            Palette = palette != null ? palette : throw new ArgumentNullException(nameof(palette));
        }

        /// <summary>The palette the views color themselves from.</summary>
        public PlaceholderPalette Palette { get; }

        /// <summary>
        /// Creates the primitive for a part under the body capsule; null when the part is drawn by the body itself.
        /// The second copy of a mirrored part hangs on the other side.
        /// </summary>
        public BodyPartView? Create(BodyPart part, Transform body, Material ownerMaterial, int copyIndex, out Material material, out bool usesOwnerMaterial)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            BodyPartDefinition? definition = _catalog.FindBodyPart(part.Spec.Id);
            if (definition == null)
            {
                Log.Warn(LogCategory.Content, "No definition for part " + part.Spec.Id + "; it is drawn as a default capsule.");
            }

            PartVisualDefinition? visual = definition != null ? definition.Visual : null;
            PartVisualKind kind = visual != null ? visual.Kind : PartVisualKind.Capsule;
            if (kind == PartVisualKind.None)
            {
                material = ownerMaterial;
                usesOwnerMaterial = true;
                return null;
            }

            GameObject piece = GameObject.CreatePrimitive(Primitive(kind));
            piece.name = part.Spec.Name + " " + part.Index;
            piece.transform.SetParent(body, false);
            Vector3 position = visual != null ? visual.LocalPosition : new Vector3(0.75f, 0f, 0f);
            Vector3 euler = visual != null ? visual.LocalEuler : Vector3.zero;
            if (visual != null && visual.MirrorSecondCopy && copyIndex % 2 == 1)
            {
                position.x = -position.x;
                euler.y = -euler.y;
                euler.z = -euler.z;
            }

            piece.transform.localPosition = position;
            piece.transform.localRotation = Quaternion.Euler(euler);
            piece.transform.localScale = visual != null ? visual.LocalScale : new Vector3(0.3f, 0.5f, 0.3f);
            piece.GetComponent<Collider>().isTrigger = true;
            piece.layer = Layers.Demon;

            PartMaterialRole role = visual != null ? visual.Material : PartMaterialRole.Owner;
            usesOwnerMaterial = role == PartMaterialRole.Owner;
            material = MaterialFor(role, ownerMaterial);
            return piece.AddComponent<BodyPartView>();
        }

        private Material MaterialFor(PartMaterialRole role, Material ownerMaterial)
        {
            switch (role)
            {
                case PartMaterialRole.Maw:
                    return Palette.Maw;
                case PartMaterialRole.Eye:
                    return Palette.Eye;
                case PartMaterialRole.Plate:
                    return Palette.Plate;
                case PartMaterialRole.Dark:
                    return Palette.Corpse;
                default:
                    return ownerMaterial;
            }
        }

        private static PrimitiveType Primitive(PartVisualKind kind)
        {
            switch (kind)
            {
                case PartVisualKind.Sphere:
                    return PrimitiveType.Sphere;
                case PartVisualKind.Cube:
                    return PrimitiveType.Cube;
                default:
                    return PrimitiveType.Capsule;
            }
        }
    }
}
