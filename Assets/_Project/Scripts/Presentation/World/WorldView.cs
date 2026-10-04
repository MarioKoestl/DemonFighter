#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// Handle to the Unity objects of a built world, so the run can tear them down in one call.
    /// </summary>
    public sealed class WorldView
    {
        public WorldView(GameObject root)
        {
            Root = root;
        }

        /// <summary>Parent of every terrain chunk, wall, feature and light.</summary>
        public GameObject Root { get; }

        /// <summary>Destroys everything the builder created; safe to call when the scene already took it down.</summary>
        public void Destroy()
        {
            if (Root != null)
            {
                Object.Destroy(Root);
            }
        }
    }
}
