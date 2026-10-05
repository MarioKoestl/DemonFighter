#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Simulation.Anatomy;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Draws body parts from their definitions. In the placeholder stage (ASSET_PIPELINE, "Placeholder standard") a
    /// part is a primitive placed on the body capsule as the definition says, with a trigger collider on the Demon
    /// layer so hits can target it. Once the art binder has filled the mesh set of a part, the part is a mesh with
    /// one state per damage stage, hung on a socket anchor of the core. Materials come from the palette by role;
    /// a bound mesh brings its own material and wears the owner color as a tint. The look data of parts and skills
    /// (motions, D-082) is read here too, so the views never touch the catalog themselves.
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

        /// <summary>The bound meshes of a part, or null while it is still drawn as a primitive.</summary>
        public PartMeshSet? MeshesFor(string partId)
        {
            BodyPartDefinition? definition = _catalog.FindBodyPart(partId);
            if (definition != null && PartMeshSet.TryFrom(definition.Visual.Meshes, out PartMeshSet set))
            {
                return set;
            }

            return null;
        }

        /// <summary>The socket anchors of a part in body units; empty when the content has none for it.</summary>
        public IReadOnlyList<SocketAnchorDefinition> AnchorsFor(string partId)
        {
            BodyPartDefinition? definition = _catalog.FindBodyPart(partId);
            return definition != null ? definition.Visual.Anchors : Array.Empty<SocketAnchorDefinition>();
        }

        /// <summary>How the body animator moves a part; None for parts the content does not know.</summary>
        public PartMotion MotionFor(string partId)
        {
            BodyPartDefinition? definition = _catalog.FindBodyPart(partId);
            return definition != null ? definition.Visual.Motion : PartMotion.None;
        }

        /// <summary>How the body animator plays a skill use; None for skills the content does not know.</summary>
        public SkillMotion MotionForSkill(string skillId)
        {
            SkillDefinition? definition = _catalog.FindSkill(skillId);
            return definition != null ? definition.Motion : SkillMotion.None;
        }

        /// <summary>
        /// Creates the primitive for a part under the placeholder space; null when the part is drawn by the body
        /// itself. The second copy of a mirrored part hangs on the other side.
        /// </summary>
        public BodyPartView? Create(BodyPart part, Transform parent, Material ownerMaterial, int copyIndex, out Material material, out bool usesOwnerMaterial)
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
            piece.transform.SetParent(parent, false);
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

        /// <summary>
        /// Creates the mesh view of a part on the rig: pivot at the anchor plus the offset of the set turned with the anchor, +Z along the
        /// anchor rotation, a box trigger around the current mesh. The imported material wins over the palette role.
        /// A set with a legacy clip plays it in a loop instead of the procedural motion (D-082).
        /// </summary>
        public BodyPartView CreateMeshPart(BodyPart part, PartMeshSet meshes, Transform rig, Vector3 anchorPosition, Quaternion anchorRotation, Material ownerMaterial, out Material material, out bool usesOwnerMaterial)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (rig == null)
            {
                throw new ArgumentNullException(nameof(rig));
            }

            var piece = new GameObject(part.Spec.Name + " " + part.Index);
            piece.layer = Layers.Demon;
            piece.transform.SetParent(rig, false);
            BodyPartDefinition? definition = _catalog.FindBodyPart(part.Spec.Id);

            // Fit values are set for the right side; a copy on the left flank is the mirror image, mesh and fit (D-088).
            bool mirrored = definition != null && definition.Visual.MirrorSecondCopy && anchorPosition.x < 0f;
            PartMeshSet placed = mirrored ? meshes.Mirrored() : meshes;
            SocketAnchors.PlaceMesh(anchorPosition, anchorRotation, placed, out Vector3 position, out Quaternion rotation);
            piece.transform.localPosition = position;
            piece.transform.localRotation = rotation;
            piece.transform.localScale = Vector3.one * meshes.Scale;
            piece.AddComponent<MeshFilter>();
            piece.AddComponent<MeshRenderer>();
            BoxCollider box = piece.AddComponent<BoxCollider>();
            box.isTrigger = true;
            if (meshes.Clip != null)
            {
                if (meshes.Clip.legacy)
                {
                    Animation animation = piece.AddComponent<Animation>();
                    animation.clip = meshes.Clip;
                    animation.playAutomatically = true;
                    animation.wrapMode = WrapMode.Loop;
                }
                else
                {
                    Log.Warn(LogCategory.Content, "The clip of " + part.Spec.Id + " is not a legacy clip; import it under Assets/_Project/Art/Animations so the import rules mark it.");
                }
            }

            BodyPartView view = piece.AddComponent<BodyPartView>();
            view.SetMeshes(placed);

            PartMaterialRole role = definition != null ? definition.Visual.Material : PartMaterialRole.Owner;
            usesOwnerMaterial = role == PartMaterialRole.Owner;
            material = meshes.Material != null ? meshes.Material : MaterialFor(role, ownerMaterial);
            return view;
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
