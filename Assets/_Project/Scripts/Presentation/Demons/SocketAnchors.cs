#nullable enable
using System.Collections.Generic;
using DemonFighter.Data;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Socket anchor arithmetic shared by the figure and the art binder: body units have the base of the body at the
    /// origin, the horizontal center on the axis and the body height as one (ASSET_PIPELINE, "Modular body parts").
    /// </summary>
    public static class SocketAnchors
    {
        private const float MinimumHeight = 0.0001f;

        /// <summary>Converts a point on a model into body units using the bounds of the model.</summary>
        public static Vector3 Normalize(Vector3 modelPoint, Bounds modelBounds)
        {
            float height = Mathf.Max(modelBounds.size.y, MinimumHeight);
            var fromBase = new Vector3(modelPoint.x - modelBounds.center.x, modelPoint.y - modelBounds.min.y, modelPoint.z - modelBounds.center.z);
            return fromBase / height;
        }

        /// <summary>
        /// Where the pivot of a part goes: the anchor plus the part's offset turned with the anchor, so +Z of the offset
        /// pushes any part out of the body and the arms on both flanks move alike (D-088).
        /// </summary>
        public static Vector3 Place(Vector3 anchorPosition, Quaternion anchorRotation, Vector3 offset)
        {
            return anchorPosition + (anchorRotation * offset);
        }

        /// <summary>
        /// Pose of a mesh part on its anchor (D-088): the pivot point of the mesh, the bottom center of the model as it
        /// stood in the tool, sits on the anchor plus the offset, turned by the anchor and then by the fit. A copy on
        /// the left flank passes its mirrored set (<see cref="PartMeshSet.Mirrored"/>), so one set of values serves
        /// both arms.
        /// </summary>
        public static void PlaceMesh(Vector3 anchorPosition, Quaternion anchorRotation, in PartMeshSet set, out Vector3 position, out Quaternion rotation)
        {
            rotation = anchorRotation * set.Rotation;
            position = Place(anchorPosition, anchorRotation, set.Offset) - (rotation * (set.Pivot * set.Scale));
        }

        /// <summary>A rotation reflected across the middle of the body: turns about Y and Z change direction.</summary>
        public static Quaternion MirrorRotation(Quaternion rotation)
        {
            return new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        /// <summary>An offset reflected across the middle of the body.</summary>
        public static Vector3 MirrorOffset(Vector3 offset)
        {
            return new Vector3(-offset.x, offset.y, offset.z);
        }

        /// <summary>
        /// Finds the anchor for the n-th copy of a part in a socket kind. With fewer anchors than copies the last
        /// one serves again, mirrored across X for every other copy, like the placeholder primitives. False when the
        /// kind has no anchor at all.
        /// </summary>
        public static bool TryFind(IReadOnlyList<SocketAnchorDefinition> anchors, SocketKind kind, int copyIndex, out Vector3 position, out Quaternion rotation)
        {
            SocketAnchorDefinition? last = null;
            int seen = 0;
            for (int i = 0; i < anchors.Count; i++)
            {
                SocketAnchorDefinition anchor = anchors[i];
                if (anchor.Kind != kind)
                {
                    continue;
                }

                if (seen == copyIndex)
                {
                    position = anchor.Position;
                    rotation = Quaternion.Euler(anchor.Euler);
                    return true;
                }

                seen++;
                last = anchor;
            }

            if (last == null)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            Vector3 mirrored = last.Position;
            Vector3 euler = last.Euler;
            if ((copyIndex - seen) % 2 == 0)
            {
                mirrored.x = -mirrored.x;
                euler.y = -euler.y;
                euler.z = -euler.z;
            }

            position = mirrored;
            rotation = Quaternion.Euler(euler);
            return true;
        }
    }
}
