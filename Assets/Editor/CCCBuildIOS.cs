#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity 2022.3 LTS batch-mode exporter. Creates the scene before making an iOS Xcode project.
/// Must run with iOS Build Support installed and a valid activated Unity license.
/// </summary>
public static class CCCBuildIOS
{
    public static void Export()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            throw new InvalidOperationException("Switch Unity build target to iOS first.");

        const string scenePath = "Assets/Scenes/Office.unity";
        Directory.CreateDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject game = new GameObject("CallCenterChaos");
        game.AddComponent<OfficePrototype>();
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new InvalidOperationException("Failed to write playable Unity scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

        PlayerSettings.productName = "Call Center Chaos";
        PlayerSettings.companyName = "Independent Prototype";
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.callcenterchaos.ios");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = "14.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string location = Path.GetFullPath("build/iOS/CallCenterChaos");
        Directory.CreateDirectory(Path.GetDirectoryName(location));
        BuildPlayerOptions options = new BuildPlayerOptions {
            scenes = new[] { scenePath },
            locationPathName = location,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };
        BuildReport result = BuildPipeline.BuildPlayer(options);
        if (result == null || result.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Unity iOS export failed; inspect the editor build log.");
        Debug.Log("iOS Xcode export complete: " + location);
    }
}
#endif
