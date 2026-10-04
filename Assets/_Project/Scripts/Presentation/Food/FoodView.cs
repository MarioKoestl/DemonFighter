#nullable enable
using System;
using DemonFighter.Simulation.Food;
using UnityEngine;

namespace DemonFighter.Presentation.Food
{
    /// <summary>
    /// The Unity side of a food item: a corpse lying where the demon fell or a severed part dropped by physics, both
    /// on the Food layer so the eat aim finds them. A falling part writes its resting position back to the
    /// simulation, which trusts the view for positions.
    /// </summary>
    public sealed class FoodView : MonoBehaviour
    {
        private FoodItem? _food;
        private Rigidbody? _rigidbody;
        private bool _settled;

        /// <summary>The food this view shows; null before binding.</summary>
        public FoodItem? Food => _food;

        /// <summary>Takes over a food item; with a rigidbody the view follows physics until it comes to rest.</summary>
        public void Bind(FoodItem food, Rigidbody? rigidbody)
        {
            _food = food ?? throw new ArgumentNullException(nameof(food));
            _rigidbody = rigidbody;
            _settled = rigidbody == null;
        }

        private void Update()
        {
            if (_food == null || _rigidbody == null || _settled)
            {
                return;
            }

            _food.SetPosition(transform.position.ToSimulation());
            if (_rigidbody.IsSleeping())
            {
                _settled = true;
            }
        }
    }
}
