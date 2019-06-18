using System.IO;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class CustomPostProcessor
{
    [PostProcessBuild]
    public static void OnPostProcessBuild (BuildTarget target, string path)
    {
        UnityEngine.Debug.Log("OnPostProcessBuild:" + target);
        if (target == BuildTarget.iOS) {
            string projPath = PBXProject.GetPBXProjectPath(path);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projPath);

            string xcTarget = project.TargetGuidByName(PBXProject.GetUnityTargetName());
 
            project.AddBuildProperty(xcTarget, "OTHER_LDFLAGS", "-ObjC");

            File.WriteAllText(projPath, project.WriteToString());
        }
    }
}