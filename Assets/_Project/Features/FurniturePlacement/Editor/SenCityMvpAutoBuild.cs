using System.IO;
using UnityEditor;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement.Editor
{
    [InitializeOnLoad]
    internal static class SenCityMvpAutoBuild
    {
        private const string ScenePath = "Assets/_Project/Production/SenCityMvp.unity";
        private static string SessionKey => $"SenCity.Mvp.AutoBuild.Attempted.{SenCityMvpSceneBuilder.SceneVersion}";

        static SenCityMvpAutoBuild()
        {
            EditorApplication.delayCall += TryBuildOnce;
        }

        private static void TryBuildOnce()
        {
            if (Application.isBatchMode || IsCurrentVersion() || SessionState.GetBool(SessionKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryBuildOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/sencity-mvp-autobuild.log", "Starting SEN CITY MVP scene build.\n");
                SenCityMvpSceneBuilder.BuildMvpScene();
                File.AppendAllText("Logs/sencity-mvp-autobuild.log", "Build completed.\n");
            }
            catch (System.Exception exception)
            {
                File.WriteAllText("Logs/sencity-mvp-autobuild-error.log", exception.ToString());
                Debug.LogException(exception);
                SessionState.SetBool(SessionKey, false);
            }
        }

        private static bool IsCurrentVersion()
        {
            if (!File.Exists(ScenePath) || !File.Exists(SenCityMvpSceneBuilder.SceneVersionPath))
                return false;

            string value = File.ReadAllText(SenCityMvpSceneBuilder.SceneVersionPath).Trim();
            return int.TryParse(value, out int version) && version == SenCityMvpSceneBuilder.SceneVersion;
        }
    }
}
