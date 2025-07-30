using UnityEngine;
using UnityEngine.UI;

public class BackgroundGeofenceTester : MonoBehaviour
{
    public InputField latitudeInput;
    public InputField longitudeInput;
    public InputField radiusInput;
    public Text statusText;
    
    private GeofenceManager geofenceManager;
    
    void Start()
    {
        geofenceManager = gameObject.AddComponent<GeofenceManager>();
        
        // Default values (example: New York coordinates)
        latitudeInput.text = "41.217097325552956";
        longitudeInput.text = "-96.09742450961889";
        radiusInput.text = "100"; // meters
    }
    
    public void AddTestGeofence()
    {
        double lat = double.Parse(latitudeInput.text);
        double lon = double.Parse(longitudeInput.text);
        float radius = float.Parse(radiusInput.text);
        
        geofenceManager.AddGeofence("test_geofence", lat, lon, radius);
        statusText.text = $"Geofence added at {lat},{lon} (Radius: {radius}m)";
    }
    
    // Called by Android plugin
    public void OnGeofenceTrigger(string message)
    {
        statusText.text = message;
        Debug.Log(message);
    }
}