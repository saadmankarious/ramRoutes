#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class iOSPostProcessBuild
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        // ── Info.plist ──
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict root = plist.root;

        // Location permission strings
        root.SetString("NSLocationAlwaysAndWhenInUseUsageDescription",
            "Ram Routes needs your location to notify you when you're near a building of interest, even when the app is in the background.");
        root.SetString("NSLocationWhenInUseUsageDescription",
            "Ram Routes needs your location to detect nearby buildings.");

        // Background modes
        PlistElementArray bgModes = root["UIBackgroundModes"] as PlistElementArray;
        if (bgModes == null)
        {
            bgModes = root.CreateArray("UIBackgroundModes");
        }

        AddIfMissing(bgModes, "location");
        AddIfMissing(bgModes, "remote-notification");

        plist.WriteToFile(plistPath);

        // ── Xcode project: link UserNotifications.framework ──
        string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string mainTarget = proj.GetUnityMainTargetGuid();
        string frameworkTarget = proj.GetUnityFrameworkTargetGuid();

        proj.AddFrameworkToProject(frameworkTarget, "UserNotifications.framework", false);
        proj.AddFrameworkToProject(frameworkTarget, "CoreLocation.framework", false);

        proj.WriteToFile(projPath);

        UnityEngine.Debug.Log("[iOSPostProcessBuild] Plist keys + frameworks added for background location & notifications.");
    }

    private static void AddIfMissing(PlistElementArray array, string value)
    {
        foreach (PlistElement elem in array.values)
        {
            if (elem.AsString() == value) return;
        }
        array.AddString(value);
    }
}
#endif
