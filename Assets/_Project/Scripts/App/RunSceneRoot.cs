#nullable enable
using System;
using DemonFighter.Presentation;
using DemonFighter.Presentation.Cameras;
using DemonFighter.Presentation.Combat;
using DemonFighter.Presentation.Demons;
using DemonFighter.UI;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// The one object the Run scene contains besides camera and HUD: it hands the run controller every asset and
    /// scene object a run needs, so nothing is looked up by name or loaded from Resources. The scene generator wires
    /// the references.
    /// </summary>
    internal sealed class RunSceneRoot : MonoBehaviour
    {
        [SerializeField] private PlaceholderPalette _palette = null!;
        [SerializeField] private WorldBuildSettings _worldSettings = null!;
        [SerializeField] private DemonViewSettings _demonSettings = null!;
        [SerializeField] private CameraRigSettings _cameraSettings = null!;
        [SerializeField] private GoreSettings _goreSettings = null!;
        [SerializeField] private DemonView _demonPrefab = null!;
        [SerializeField] private CameraRig _cameraRig = null!;
        [SerializeField] private HudScreen _hud = null!;

        public PlaceholderPalette Palette => _palette;

        public WorldBuildSettings WorldSettings => _worldSettings;

        public DemonViewSettings DemonSettings => _demonSettings;

        public CameraRigSettings CameraSettings => _cameraSettings;

        public GoreSettings GoreSettings => _goreSettings;

        public DemonView DemonPrefab => _demonPrefab;

        public CameraRig CameraRig => _cameraRig;

        public HudScreen Hud => _hud;

        /// <summary>Fails loudly at run start when the scene lost a reference; regenerating the scenes fixes it.</summary>
        public void Validate()
        {
            Require(_palette, nameof(_palette));
            Require(_worldSettings, nameof(_worldSettings));
            Require(_demonSettings, nameof(_demonSettings));
            Require(_cameraSettings, nameof(_cameraSettings));
            Require(_goreSettings, nameof(_goreSettings));
            Require(_demonPrefab, nameof(_demonPrefab));
            Require(_cameraRig, nameof(_cameraRig));
            Require(_hud, nameof(_hud));
        }

        private static void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
            {
                throw new InvalidOperationException("RunSceneRoot is missing " + field + "; run Demon Fighter > Generate > Scenes.");
            }
        }
    }
}
