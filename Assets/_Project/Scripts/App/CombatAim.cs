#nullable enable
using System;
using DemonFighter.Input;
using DemonFighter.Presentation.Combat;
using DemonFighter.Simulation;

namespace DemonFighter.App
{
    /// <summary>Hands the aim Presentation found under the crosshair to the input adapter; App is the only layer that sees both.</summary>
    internal sealed class CombatAim : IPlayerAim
    {
        private readonly CombatPresenter _combat;

        public CombatAim(CombatPresenter combat)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        /// <inheritdoc />
        public FoodId AimedFood => _combat.AimedFood;
    }
}
