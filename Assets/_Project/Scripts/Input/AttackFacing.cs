#nullable enable
using System.Numerics;

namespace DemonFighter.Input
{
    /// <summary>
    /// Which way the body turns while the player attacks (D-093). While the camera looks from behind or beside, the body
    /// turns toward where it looks, so the crosshair aims (D-065). A third-person camera turned around to look at the
    /// face is the exception: the body keeps its facing and attacks that way, so the attack can be watched from the
    /// front; the hit lands on whatever stands in front within reach. First person always looks where the body faces.
    /// </summary>
    public static class AttackFacing
    {
        public static Vector2 Choose(Vector2 cameraHeading, Vector2 bodyFacing, bool firstPerson)
        {
            if (firstPerson || Vector2.Dot(cameraHeading, bodyFacing) >= 0f)
            {
                return cameraHeading;
            }

            return bodyFacing;
        }
    }
}
