using UnityEngine;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Bridge between iOS native background location and Unity. Singleton, survives scene loads.
/// </summary>
public class BackgroundLocationService : MonoBehaviour
{
    public static BackgroundLocationService Instance { get; private set; }

    /// <summary>Fired on every location update (lat, lon, accuracy in meters).</summary>
    public event Action<double, double, float> OnLocationUpdated;

    /// <summary>Fired when a geofence region is entered (building name).</summary>
    public event Action<string> OnGeofenceTriggered;

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public float Accuracy { get; private set; }
    public bool IsRunning { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _StartBackgroundLocation(string gameObjectName, float distanceFilter);

    [DllImport("__Internal")]
    private static extern void _StopBackgroundLocation();

    [DllImport("__Internal")]
    private static extern void _RegisterGeofence(string identifier, double latitude, double longitude, double radius);
#endif

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Start receiving background location updates.</summary>
    public void StartTracking(float distanceFilter = 5f)
    {
        if (IsRunning) return;
        IsRunning = true;

#if UNITY_IOS && !UNITY_EDITOR
        _StartBackgroundLocation(gameObject.name, distanceFilter);
        Debug.Log("[BackgroundLocation] iOS native tracking started");
#else
        Debug.Log("[BackgroundLocation] Not on iOS device — no background tracking");
#endif
    }

    /// <summary>Stop background location updates. Geofences remain active.</summary>
    public void StopTracking()
    {
        if (!IsRunning) return;
        IsRunning = false;

#if UNITY_IOS && !UNITY_EDITOR
        _StopBackgroundLocation();
        Debug.Log("[BackgroundLocation] iOS native tracking stopped (geofences still active)");
#endif
    }

    /// <summary>Register a geofence around a building. Survives app kill. Max 20 regions on iOS.</summary>
    public void RegisterBuildingGeofence(string buildingName, double latitude, double longitude, double radiusMeters = 100.0)
    {
#if UNITY_IOS && !UNITY_EDITOR
        _RegisterGeofence(buildingName, latitude, longitude, radiusMeters);
        Debug.Log($"[BackgroundLocation] Registered geofence: {buildingName} ({latitude}, {longitude}, {radiusMeters}m)");
#else
        Debug.Log($"[BackgroundLocation] Geofence not supported in editor: {buildingName}");
#endif
    }

    /// <summary>Called from native plugin via UnitySendMessage. Do not rename.</summary>
    public void OnNativeLocationUpdate(string message)
    {
        var parts = message.Split(',');
        if (parts.Length < 3) return;

        Latitude = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
        Longitude = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
        Accuracy = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);

        OnLocationUpdated?.Invoke(Latitude, Longitude, Accuracy);
    }

    /// <summary>Called from native plugin when a geofence is entered. Do not rename.</summary>
    public void OnGeofenceEntered(string identifier)
    {
        Debug.Log($"[BackgroundLocation] Geofence entered: {identifier}");
        OnGeofenceTriggered?.Invoke(identifier);
    }

    void OnDestroy()
    {
        StopTracking();
    }
}
