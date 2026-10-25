#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class PrototypeStartupCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Schedule()
        {
            // A hidden diagnostic player must advance before Start's first yielded frame.
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-prototypeScreenshot") >= 0)
                Application.runInBackground = true;
            new GameObject("PrototypeStartupCheck").AddComponent<PrototypeStartupCheck>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            var session = FindAnyObjectByType<GameSessionController>();
            var player = FindAnyObjectByType<PlayerHopController>();
            var hud = FindAnyObjectByType<PrototypeHud>();
            bool ready = session != null && session.Phase == GamePhase.WaitingToStart &&
                player != null && player.GetComponentsInChildren<MeshRenderer>().Length >= 4 &&
                hud != null && hud.GetComponentsInChildren<UnityEngine.UI.Text>()
                    .Any(text => text.name == "StartCountdown" && text.text.Replace('\u00a0', ' ') == "TAP TO START") &&
                Camera.main != null && Camera.main.GetComponent<QuarterViewCamera>() != null;
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported ||
                        material.shader.name == "Hidden/InternalErrorShader") ready = false;
            if (ready) Debug.Log("[PrototypeStartup] PASS: player, HUD, quarter-view camera and materials ready.");
            else Debug.LogError("[PrototypeStartup] FAIL: game startup or a required material is missing.");

            string[] args = System.Environment.GetCommandLineArgs();
            int captureArg = System.Array.IndexOf(args, "-prototypeScreenshot");
            if (captureArg >= 0 && captureArg + 1 < args.Length)
            {
                // Explicit development-only static composition checks, never normal gameplay.
                int viewArg = System.Array.IndexOf(args, "-prototypeView");
                if (viewArg >= 0 && viewArg + 1 < args.Length)
                {
                    session.enabled = false;
                    hud.ShowPlaying();
                    string view = args[viewArg + 1];
                    if (view == "gameover")
                    {
                        hud.ShowGameOver(12, "Preview", 34);
                        player.ShowFailurePose();
                    }
                    float startZ = view == "entry" || view == "signal" || view == "near-miss" ? 3f : view == "cross" ? 36f : 0f;
                    var scenery = FindAnyObjectByType<SectionScenery>();
                    scenery.ResetSections();
                    if (view == "school") scenery.PrepareSection(PrototypeGameMode.Hopscotch, -40, -13);
                    scenery.PrepareSection(view == "cross" ? PrototypeGameMode.Crosswalk : PrototypeGameMode.Hopscotch,
                        startZ, startZ + 30);
                    scenery.PrepareSection(PrototypeGameMode.CurbBalance, startZ + 33, startZ + 63);
                    player.transform.position = new Vector3(0, view == "curb" ? .64f : .22f, startZ);
                    if (view == "hop" || view == "school" || view == "ball" || view == "ball-early")
                    {
                        var generator = FindAnyObjectByType<HopscotchPatternGenerator>(FindObjectsInactive.Include);
                        generator.gameObject.SetActive(true);
                        generator.BeginSection(startZ, 9);
                        generator.Current.SetCurrent(true);
                        if (view == "ball-early")
                        {
                            player.transform.position = generator.Current.LandingPosition;
                            generator.Advance();
                            generator.TryGetPattern(3, out var upcoming);
                            FindAnyObjectByType<HopscotchBallObstacle>(FindObjectsInactive.Include)
                                .Preview(upcoming.LandingPosition);
                        }
                        if (view == "ball")
                        {
                            var ball = FindAnyObjectByType<HopscotchBallObstacle>(FindObjectsInactive.Include);
                            ball.Schedule(generator.Current.LandingPosition, AudioSettings.dspTime + .12d);
                            ball.Show(AudioSettings.dspTime);
                        }
                    }
                    else if (view == "cross" || view == "entry" || view == "signal" || view == "near-miss")
                    {
                        var generator = FindAnyObjectByType<CrosswalkPatternGenerator>(FindObjectsInactive.Include);
                        generator.gameObject.SetActive(true);
                        generator.BeginSection(startZ, 8);
                        if (view == "entry" || view == "signal" || view == "near-miss")
                        {
                            player.transform.position = new Vector3(0, .22f, 0);
                            scenery.ResetSections();
                            scenery.PrepareSection(PrototypeGameMode.Crosswalk, startZ, generator.EndZ, generator.CurrentStartZ);
                            scenery.PrepareSection(PrototypeGameMode.CurbBalance, generator.EndZ + 3, generator.EndZ + 33);
                            if (view == "signal" || view == "near-miss")
                            {
                                var signal = FindAnyObjectByType<CrosswalkSignal>(FindObjectsInactive.Include);
                                signal.Prepare(generator.CurrentStartZ);
                                if (view == "near-miss")
                                {
                                    player.transform.position = new Vector3(0f, .22f, generator.CurrentStartZ + CrosswalkSignal.RoadVehicleInset);
                                    signal.ShowNearMiss(.4f, player.transform.position.z, true);
                                }
                                else signal.ShowWait(.75f);
                            }
                        }
                    }
                    else if (view == "curb")
                    {
                        var generator = FindAnyObjectByType<CurbPathGenerator>(FindObjectsInactive.Include);
                        generator.gameObject.SetActive(true);
                        generator.BeginSection(startZ - 1, 26, .28f);
                        FindAnyObjectByType<CurbWindVisual>(FindObjectsInactive.Include)
                            .Show(player.transform.position, 4f);
                    }
                }
                Application.runInBackground = true;
                int poseArg = System.Array.IndexOf(args, "-prototypePose");
                if (poseArg >= 0 && poseArg + 1 < args.Length)
                {
                    string pose = args[poseArg + 1];
                    if (pose == "balance") player.SetBalanceWalkPose(.6f, 1f, 35f);
                    else player.HopTo(player.transform.position,
                        pose == "single" ? LandingType.Single : LandingType.Double, .15f, null);
                }
                var capture = new RenderTexture(Screen.width, Screen.height, 24);
                Camera.main.targetTexture = capture;
                // Include overlay HUD in the off-screen diagnostic render as well.
                Canvas canvas = hud.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1f;
                yield return new WaitForSecondsRealtime(1f);
                yield return new WaitForEndOfFrame();
                Camera.main.targetTexture = null;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(Camera.main,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = capture });
                RenderTexture.active = capture;
                CaptureBitmap(args[captureArg + 1]);
                RenderTexture.active = null;
                Camera.main.targetTexture = null;
                capture.Release();
                Destroy(capture);
                yield return new WaitForSecondsRealtime(2f);
                Application.Quit(ready ? 0 : 1);
            }
            else Destroy(gameObject);
        }

        private static void CaptureBitmap(string path)
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            Color32[] pixels = texture.GetPixels32();
            using (var writer = new System.IO.BinaryWriter(System.IO.File.Create(path)))
            {
                writer.Write((ushort)0x4d42);
                writer.Write(54 + pixels.Length * 4);
                writer.Write(0);
                writer.Write(54);
                writer.Write(40);
                writer.Write(texture.width);
                writer.Write(texture.height);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(0);
                writer.Write(pixels.Length * 4);
                writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                foreach (Color32 pixel in pixels)
                {
                    writer.Write(pixel.b); writer.Write(pixel.g); writer.Write(pixel.r); writer.Write((byte)255);
                }
            }
            Destroy(texture);
        }
    }
}
#endif
