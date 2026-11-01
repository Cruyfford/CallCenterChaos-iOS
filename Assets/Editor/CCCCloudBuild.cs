#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity Build Automation pre-export entrypoint.
/// Unity Cloud calls CCCCloudBuild.PreExport before its own regular build pipeline.
/// This method creates the prototype scene and intentionally does NOT call BuildPipeline.BuildPlayer.
/// </summary>
public static class CCCCloudBuild
{
    public static void PreExport()
    {
        const string scenePath = "Assets/Scenes/Office.unity";
        Directory.CreateDirectory("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject office = new GameObject("CallCenterChaos");
        office.AddComponent<OfficePrototype>();
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new InvalidOperationException("Unable to save prototype scene.");

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
        Debug.Log("CCCCloudBuild.PreExport created scene: " + scenePath);
    }
}
#endif
