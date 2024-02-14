using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor
{
    public class Builder
    {
        /*private static void BuildEmbeddedLinux(EmbeddedLinuxArchitecture architecture)
        {
            // Set architecture in BuildSettings
            EditorUserBuildSettings.

            // Setup build options (e.g. scenes, build output location)
            var options = new BuildPlayerOptions
            {
                // Change to scenes from your project
                scenes = new[]
                {
                    "Assets/Scenes/SampleScene.unity",
                },
                // Change to location the output should go
                locationPathName = "../EmbeddedLinuxPlayer/",
                options = BuildOptions.CleanBuildCache | BuildOptions.StrictMode,
                target = BuildTarget.EmbeddedLinux
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build successful - Build written to {options.locationPathName}");
            }
            else if (report.summary.result == BuildResult.Failed)
            {
                Debug.LogError($"Build failed");
            }
        }*/

        private static void BuildWindows()
        {
            // Setup build options (e.g. scenes, build output location)
            var options = new BuildPlayerOptions
            {
                // Change to scenes from your project
                scenes = new[]
                {
                    "Assets/Scenes/GameScene.unity",
                },
                // Change to location the output should go
                locationPathName = "../Build/TCC.exe",
                options = BuildOptions.None,
                target = BuildTarget.StandaloneWindows64
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build successful - Build written to {options.locationPathName}");
            }
            else if (report.summary.result == BuildResult.Failed)
            {
                Debug.LogError($"Build failed");
            }
        }

        // This function will be called from the build process
        public static void Build()
        {
            // Build EmbeddedLinux ARM64 Unity player
            BuildWindows();
        }
    }
}