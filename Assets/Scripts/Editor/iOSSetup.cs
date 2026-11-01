#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class iOSSetup
{
    [PostProcessBuild(50)]
    public static void Configure(BuildTarget target, string path)
    {
        if(target != BuildTarget.iOS) return;
        string plistPath = Path.Combine(path,"Info.plist");
        var plist = new PlistDocument(); plist.ReadFromFile(plistPath);
        plist.root.SetString("NSMicrophoneUsageDescription", "Talk with fictional callers in the game.");
        plist.root.SetString("NSSpeechRecognitionUsageDescription", "Recognize your voice during fictional phone calls.");
        plist.WriteToFile(plistPath);
        string projectPath = PBXProject.GetPBXProjectPath(path);
        var project = new PBXProject(); project.ReadFromFile(projectPath);
        string targetGuid = project.GetUnityFrameworkTargetGuid();
        project.AddFrameworkToProject(targetGuid, "Speech.framework", false);
        project.AddFrameworkToProject(targetGuid, "AVFoundation.framework", false);
        project.WriteToFile(projectPath);
    }
}
#endif
