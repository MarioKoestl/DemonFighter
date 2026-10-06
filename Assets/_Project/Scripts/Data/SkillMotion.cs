#nullable enable
namespace DemonFighter.Data
{
    /// <summary>
    /// How the view animates a skill use (D-082): a bite snaps the jaws and contracts the body, a swipe swings a
    /// limb, a lunge stretches the body forward, a tail swing whips the tail. Content data on the skill asset.
    /// </summary>
    public enum SkillMotion
    {
        None,
        Bite,
        Swipe,
        Lunge,
        TailSwing,
    }
}
