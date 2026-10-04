#nullable enable
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// The few settings v1 keeps, in PlayerPrefs (D-076): whether the mutation menu offers a random hand instead of
    /// the whole shop (the Option B playtest of O-001). Graphics, audio and mouse settings come with M5.
    /// </summary>
    internal sealed class GameSettings
    {
        private const string RandomOffersKey = "settings.randomOffers";

        /// <summary>True when the mutation menu offers a random hand of three (O-001, B) instead of every mutation (A).</summary>
        public bool RandomOffers
        {
            get => PlayerPrefs.GetInt(RandomOffersKey, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(RandomOffersKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
