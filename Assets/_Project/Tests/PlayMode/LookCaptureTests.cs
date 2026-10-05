#nullable enable
using System.Collections;
using System.IO;
using DemonFighter.App;
using DemonFighter.Presentation;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Worldgen;
using DemonFighter.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DemonFighter.PlayMode.Tests
{
    /// <summary>
    /// Not a test but a camera: starts a real run with a fixed seed and saves what the player sees, a lava pool and an
    /// overview as PNG files into TestResults/look, so the look can be judged and tuned without opening the editor.
    /// Explicit, so it only runs when asked for by name:
    /// -runTests -testPlatform PlayMode -testFilter DemonFighter.PlayMode.Tests.LookCaptureTests
    /// </summary>
    [Explicit("Renders screenshots for tuning the look; run on purpose with -testFilter.")]
    public sealed class LookCaptureTests
    {
        private const int MaxFramesToWait = 600;
        private const int SettleFrames = 90;
        private const int Width = 1280;
        private const int Height = 720;
        private const string OutputFolder = "TestResults/look";
        private const string SeedText = "lookcapture";
        private const string ArmId = "part.arm";

        [TearDown]
        public void LeaveNoRunBehind()
        {
            TestRuns.EndAll();
        }

        [UnityTest]
        public IEnumerator CaptureLook()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneNames.MainMenu && Object.FindAnyObjectByType<MainMenuScreen>() != null,
                "the main menu did not appear");
            yield return null;

            VisualElement menu = Object.FindAnyObjectByType<MainMenuScreen>().GetComponent<UIDocument>().rootVisualElement;
            menu.Q<TextField>("seed").value = SeedText;
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                Button newRun = menu.Q<Button>("new-run");
                submit.target = newRun;
                newRun.SendEvent(submit);
            }

            yield return WaitUntil(() => RunningState() != null && GameObject.Find("World") != null, "the run did not start");
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }

            Directory.CreateDirectory(OutputFolder);
            Camera main = Camera.main;
            Assert.That(main, Is.Not.Null, "the run has no main camera");
            Capture(main, "1-player-view");

            RunState state = RunningState()!;
            WorldLayout world = state.World!;
            Demon? player = FindPlayer(state);
            Vector3 playerPosition = player != null ? player.Position.ToUnity() : Vector3.zero;

            var cameraObject = new GameObject("Look Capture Camera");
            Camera capture = cameraObject.AddComponent<Camera>();
            capture.CopyFrom(main);
            UniversalAdditionalCameraData data = capture.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            capture.fieldOfView = 55f;

            FeaturePlacement? lava = NearestLava(world, playerPosition);
            if (lava.HasValue)
            {
                Vector3 center = lava.Value.Position.ToUnity();
                float radius = lava.Value.FootprintRadius;
                Vector3 fromSide = (playerPosition - center).sqrMagnitude > 1f ? (playerPosition - center).normalized : Vector3.back;
                fromSide.y = 0f;
                capture.transform.position = center + fromSide.normalized * (radius * 2.2f) + Vector3.up * (radius * 0.9f + 4f);
                capture.transform.LookAt(center);
                Capture(capture, "2-lava");
            }

            capture.transform.position = playerPosition + new Vector3(-30f, 35f, -30f);
            capture.transform.LookAt(playerPosition);
            Capture(capture, "3-overview");

            capture.transform.position = playerPosition + new Vector3(0f, 2.5f, -6f);
            capture.transform.LookAt(playerPosition + Vector3.up * 1f + Vector3.forward * 20f);
            Capture(capture, "4-ground-level");

            // A demon with arms from the front: both flanks, the right arm and its mirror image (D-088).
            Demon? armed = FindArmedDemon(state, playerPosition);
            if (armed != null)
            {
                Vector3 position = armed.Position.ToUnity();
                Vector3 forward = SimulationVectors.YawToRotation(armed.Yaw) * Vector3.forward;
                float size = armed.SizeMeters;
                capture.transform.position = position + (forward * (size * 2.2f)) + (Vector3.up * (size * 0.9f));
                capture.transform.LookAt(position + (Vector3.up * (size * 0.55f)));
                Capture(capture, "5-arms");
            }

            Object.Destroy(cameraObject);
            Debug.Log("Look captured into " + Path.GetFullPath(OutputFolder));
        }

        // Perceived brightness of the picture, 0 to 255, for judging the look in numbers next to the screenshots.
        private static double MeanBrightness(Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            double sum = 0;
            foreach (Color32 pixel in pixels)
            {
                sum += (0.2126 * pixel.r) + (0.7152 * pixel.g) + (0.0722 * pixel.b);
            }

            return sum / pixels.Length;
        }

        private static void Capture(Camera camera, string name)
        {
            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            RenderTexture? previousTarget = camera.targetTexture;
            RenderTexture? previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(OutputFolder, name + ".png"), image.EncodeToPNG());
                Debug.Log("Look " + name + ": mean brightness " + MeanBrightness(image).ToString("F1") + " of 255");
                Object.Destroy(image);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                texture.Release();
                Object.Destroy(texture);
            }
        }

        private static RunState? RunningState()
        {
            foreach (Bootstrapper bootstrapper in Object.FindObjectsByType<Bootstrapper>(FindObjectsSortMode.None))
            {
                if (bootstrapper.RunController != null && bootstrapper.RunController.CurrentRun != null)
                {
                    return bootstrapper.RunController.CurrentRun;
                }
            }

            return null;
        }

        // The AI demon with the most arms, the nearest one among equals.
        private static Demon? FindArmedDemon(RunState state, Vector3 near)
        {
            Demon? best = null;
            int bestArms = 0;
            float bestDistance = float.MaxValue;
            foreach (Demon demon in state.Demons)
            {
                if (!demon.IsAlive || demon.Controller == ControllerKind.Player)
                {
                    continue;
                }

                int arms = 0;
                foreach (BodyPart part in demon.Body.Parts)
                {
                    if (part.Spec.Id == ArmId && !part.IsLost)
                    {
                        arms++;
                    }
                }

                float distance = (demon.Position.ToUnity() - near).sqrMagnitude;
                if (arms > bestArms || (arms > 0 && arms == bestArms && distance < bestDistance))
                {
                    best = demon;
                    bestArms = arms;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static Demon? FindPlayer(RunState state)
        {
            foreach (Demon demon in state.Demons)
            {
                if (demon.Controller == ControllerKind.Player)
                {
                    return demon;
                }
            }

            return null;
        }

        private static FeaturePlacement? NearestLava(WorldLayout world, Vector3 from)
        {
            FeaturePlacement? best = null;
            float bestDistance = float.MaxValue;
            foreach (FeaturePlacement feature in world.Features)
            {
                float distance = (feature.Position.ToUnity() - from).sqrMagnitude;
                if (feature.Kind == FeatureKind.LavaPool && distance < bestDistance)
                {
                    best = feature;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, string message)
        {
            for (int frame = 0; frame < MaxFramesToWait; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.That(condition(), Is.True, message);
        }
    }
}
