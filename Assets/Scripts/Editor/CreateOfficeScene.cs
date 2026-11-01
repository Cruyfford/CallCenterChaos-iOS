#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class CreateOfficeScene
{
    [MenuItem("Tools/Call Center Chaos/Create Playable Scene")]
    public static void GenerateScene()
    {
        const string dir = "Assets/Scenes";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("GameBootstrap").AddComponent<OfficePrototype>();
        const string path = "Assets/Scenes/Office.unity";
        EditorSceneManager.SaveScene(scene,path);
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(path,true)};
        Debug.Log("Generated playable scene: Assets/Scenes/Office.unity. Press Play to test.");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
    }
}
#endif
