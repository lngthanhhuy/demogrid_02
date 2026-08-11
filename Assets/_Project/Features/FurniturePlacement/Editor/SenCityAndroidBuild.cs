using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement.Editor
{
    public static class SenCityAndroidBuild
    {
        private const string ScenePath = "Assets/_Project/Production/SenCityMvp.unity";
        private const string OutputDirectory = "Builds/Android";
        private const string OutputPath = OutputDirectory + "/SenCity-MVP.apk";

        [MenuItem("Tools/SEN CITY/Android/Build MVP APK")]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Android Build Support is not installed for this Unity Editor version.");

            if (!File.Exists(ScenePath))
                SenCityMvpSceneBuilder.BuildMvpScene();

            Directory.CreateDirectory(OutputDirectory);
            EditorUserBuildSettings.buildAppBundle = false;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new BuildFailedException("Unity could not switch the active platform to Android.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android APK build failed: {report.summary.result}");

            Debug.Log($"[SenCityAndroidBuild] APK ready: {Path.GetFullPath(OutputPath)} ({report.summary.totalSize} bytes)");
        }

        [MenuItem("Tools/SEN CITY/Android/Build MVP APK", true)]
        private static bool ValidateBuildApk()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
