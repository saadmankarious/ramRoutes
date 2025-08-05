using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class NotificationSymbolsSetup
{
    static NotificationSymbolsSetup()
    {
        // Add notification symbols automatically
        AddSymbolIfMissing(BuildTargetGroup.Android, "UNITY_NOTIFICATIONS_ANDROID");
        AddSymbolIfMissing(BuildTargetGroup.iOS, "UNITY_NOTIFICATIONS_IOS");
    }

    static void AddSymbolIfMissing(BuildTargetGroup target, string symbol)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        if (!defines.Contains(symbol))
        {
            if (string.IsNullOrEmpty(defines))
                defines = symbol;
            else
                defines += ";" + symbol;
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
            Debug.Log($"Added {symbol} to {target} scripting define symbols");
        }
    }
}
