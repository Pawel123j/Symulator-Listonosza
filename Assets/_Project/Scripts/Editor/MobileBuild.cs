using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Listonosz.EditorTools
{
    /// <summary>
    /// Build paczki APK na Androida: ustawia parametry gracza (nazwa, identyfikator, orientacja),
    /// po czym buduje scenę Playground do Builds/Listonosz.apk. Uruchamiać z aktywną platformą Android
    /// (np. -buildTarget Android) — pierwszy build IL2CPP bywa długi i wymaga modułu Android (SDK/NDK).
    /// </summary>
    public static class MobileBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/Playground.unity";

        [MenuItem("Listonosz/Build APK (Android)")]
        public static void BuildAndroid()
        {
            PlayerSettings.companyName = "Listonosz";
            PlayerSettings.productName = "Symulator Listonosza";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.listonosz.symulator");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

            string dir = "Builds";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string apk = Path.Combine(dir, "Listonosz.apk");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Listonosz] APK ZBUDOWANY: {apk} ({summary.totalSize} bajtow).");
            }
            else
            {
                Debug.LogError($"[Listonosz] Build APK NIEUDANY: {summary.result}, bledow: {summary.totalErrors}.");
            }
        }
    }
}
