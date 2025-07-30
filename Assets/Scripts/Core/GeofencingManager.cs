using UnityEngine;

public class GeofenceManager : MonoBehaviour
{
    private AndroidJavaObject pluginInstance;
    
    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass pluginClass = new AndroidJavaClass("com.edgoanalytics.ramroutes.plugin.UnityGeofencingInterface");
            pluginClass.CallStatic("initialize");
        #endif
    }
    
    public void AddGeofence(string requestId, double latitude, double longitude, float radius)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass pluginClass = new AndroidJavaClass("com.edgoanalytics.ramroutes.plugin.UnityGeofencingInterface");
            pluginClass.CallStatic("addGeofence", requestId, latitude, longitude, radius);
        #endif
    }
    
    // Called from Android plugin
    public void OnGeofenceEntered(string geofenceId)
    {
        Debug.Log("Entered geofence: " + geofenceId);
        // Handle geofence enter event
    }
    
    // Called from Android plugin
    public void OnGeofenceExited(string geofenceId)
    {
        Debug.Log("Exited geofence: " + geofenceId);
        // Handle geofence exit event
    }
}