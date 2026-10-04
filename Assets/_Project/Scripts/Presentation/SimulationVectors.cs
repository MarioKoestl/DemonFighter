#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation
{
    /// <summary>
    /// The one place where simulation vectors and angles become Unity ones and back. The simulation uses
    /// System.Numerics with X east, Y up, Z north and a clockwise yaw in radians, which matches Unity's axes.
    /// </summary>
    public static class SimulationVectors
    {
        /// <summary>Converts a simulation position to a Unity position.</summary>
        public static Vector3 ToUnity(this System.Numerics.Vector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        /// <summary>Converts a Unity position to a simulation position.</summary>
        public static System.Numerics.Vector3 ToSimulation(this Vector3 vector)
        {
            return new System.Numerics.Vector3(vector.x, vector.y, vector.z);
        }

        /// <summary>Converts a ground-plane direction to a Unity vector on the XZ plane.</summary>
        public static Vector3 ToUnityPlanar(this System.Numerics.Vector2 direction)
        {
            return new Vector3(direction.X, 0f, direction.Y);
        }

        /// <summary>Rotation around the up axis for a simulation yaw in radians.</summary>
        public static Quaternion YawToRotation(float yawRadians)
        {
            return Quaternion.Euler(0f, yawRadians * Mathf.Rad2Deg, 0f);
        }

        /// <summary>Simulation yaw in radians of a Unity rotation.</summary>
        public static float RotationToYaw(Quaternion rotation)
        {
            return rotation.eulerAngles.y * Mathf.Deg2Rad;
        }
    }
}
