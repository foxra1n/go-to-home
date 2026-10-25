using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class HopscotchPrototypeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (FindFirstObjectByType<HopscotchPrototypeBootstrap>() != null)
            {
                return;
            }

            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;

            GameObject root = new GameObject("HopscotchPrototype");
            root.AddComponent<HopscotchPrototypeBootstrap>();

            DifficultyController difficulty = CreateComponent<DifficultyController>("Difficulty", root.transform);
            difficulty.TestMaximumDifficulty = false;
            ScoreController score = CreateComponent<ScoreController>("Score", root.transform);
            TapInputReader input = CreateComponent<TapInputReader>("Input", root.transform);
            RhythmClock rhythm = CreateComponent<RhythmClock>("Rhythm", root.transform);
            rhythm.Initialize();

            PlayerHopController player = CreateComponent<PlayerHopController>("Player", root.transform);
            player.Initialize();

            HopscotchPatternGenerator generator = CreateComponent<HopscotchPatternGenerator>("PatternGenerator", root.transform);
            generator.Initialize();

            CurbPathGenerator curbPath = CreateComponent<CurbPathGenerator>("CurbPathGenerator", root.transform);
            curbPath.Initialize();

            CrosswalkPatternGenerator crosswalkGenerator = CreateComponent<CrosswalkPatternGenerator>("CrosswalkPatternGenerator", root.transform);
            crosswalkGenerator.Initialize();

            PrototypeHud hud = CreateComponent<PrototypeHud>("HUD", root.transform);
            hud.Initialize();

            ConfigureCamera(player.transform);
            CreateGround(player.transform, root.transform);
            CreateComponent<NeighborhoodScenery>("Neighborhood", root.transform).Configure(player.transform);
            ConfigureLight();

            HopscotchModeController hopscotchMode = CreateComponent<HopscotchModeController>("HopscotchMode", root.transform);
            hopscotchMode.Configure(generator, player, difficulty, rhythm);

            CurbBalanceModeController curbMode = CreateComponent<CurbBalanceModeController>("CurbBalanceMode", root.transform);
            curbMode.Configure(curbPath, player, difficulty);

            CrosswalkModeController crosswalkMode = CreateComponent<CrosswalkModeController>("CrosswalkMode", root.transform);
            crosswalkMode.Configure(crosswalkGenerator, player, difficulty);

            GameModeSequenceController modeSequence = CreateComponent<GameModeSequenceController>("ModeSequence", root.transform);
            modeSequence.Configure(
                hopscotchMode,
                curbMode,
                crosswalkMode,
                generator.gameObject,
                curbPath.gameObject,
                crosswalkGenerator.gameObject,
                player, difficulty, rhythm);

            CreateComponent<SectionScenery>("SectionScenery", root.transform).Configure(modeSequence);

            GameSessionController session = CreateComponent<GameSessionController>("GameSession", root.transform);
            session.Configure(input, modeSequence, score, difficulty, hud, rhythm);
        }

        private static T CreateComponent<T>(string objectName, Transform parent) where T : Component
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        private static void ConfigureCamera(Transform target)
        {
            Camera cameraComponent = Camera.main;
            if (cameraComponent == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraComponent = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            QuarterViewCamera quarterView = cameraComponent.GetComponent<QuarterViewCamera>();
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                AudioListener listener = cameraComponent.GetComponent<AudioListener>();
                if (listener == null) listener = cameraComponent.gameObject.AddComponent<AudioListener>();
                listener.enabled = true;
            }
            if (quarterView == null)
            {
                quarterView = cameraComponent.gameObject.AddComponent<QuarterViewCamera>();
            }

            quarterView.Configure(target);
        }

        private static void CreateGround(Transform target, Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "EndlessGround";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -0.28f, 25f);
            ground.transform.localScale = new Vector3(8.5f, 0.28f, 80f);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            ground.GetComponent<Renderer>().sharedMaterial = new Material(shader)
            {
                name = "GroundMaterial",
                color = new Color(0.43f, 0.47f, 0.46f)
            };

            ground.AddComponent<EndlessGroundFollower>().Configure(target);
        }

        private static void ConfigureLight()
        {
            Light light = FindFirstObjectByType<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.79f);
        }
    }
}
