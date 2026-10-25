using UnityEditor;
using UnityEngine;

namespace Homeward.Hopscotch.Editor
{
    public static class PrototypeProjectConfigurator
    {
        public static void BuildStartupSmokePlayer()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int outputIndex = System.Array.IndexOf(args, "-buildOutput");
            if (outputIndex < 0 || outputIndex + 1 >= args.Length)
                throw new System.ArgumentException("Smoke build requires -buildOutput");
            string output = args[outputIndex + 1];
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,
                ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Startup smoke build failed");
        }

        // Invoked by Unity CLI; this is a locally signed test APK, not a store release.
        public static void BuildAndroidApk()
        {
            string output = System.IO.Path.GetFullPath("Builds/Android/HomewardPrototype.apk");
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
                if (arguments[i] == "-buildOutput") output = arguments[i + 1];

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,
                "com.homeward.hopscotch.prototype");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.bundleVersion = "0.1.1";
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            AssetDatabase.SaveAssets();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Android APK build failed: " + report.summary.result);
            Debug.Log("[Hopscotch] Test APK built: " + output);
        }

        [MenuItem("Tools/Hopscotch Prototype/Apply Mobile Portrait Settings")]
        public static void ApplyMobilePortraitSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.companyName = "Homeward Prototype";
            PlayerSettings.productName = "Hopscotch Tap Prototype";

            AssetDatabase.SaveAssets();
            Debug.Log("[Hopscotch] Applied portrait-only mobile prototype settings.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
