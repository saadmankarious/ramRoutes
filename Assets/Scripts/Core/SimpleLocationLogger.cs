using UnityEngine;
using UnityEngine.Android;

public class BuildingProximityDetector : MonoBehaviour
{
    [System.Serializable]
    public class Building
    {
        public string name;
        public Vector2 entranceGPS; // Latitude, Longitude
        public float detectionRadius = 20f; // Meters
        public float closeProximityRadius = 5f; // Meters
        public Color debugColor = Color.cyan;
    }

    [SerializeField] private Building[] buildings = new Building[2];
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private bool showDebugInfo = true;

    private LocationServiceStatus locationStatus;
    private string currentStatus = "Initializing...";
    private Building closestBuilding;
    private float distanceToBuilding;

    void Start()
    {
        if (buildings.Length == 0)
        {
            buildings = new Building[2] {
                new Building {
                    name = "Main Office",
                    entranceGPS = new Vector2(37.7749f, -122.4194f),
                    detectionRadius = 25f,
                    closeProximityRadius = 8f,
                    debugColor = Color.blue
                },
                new Building {
                    name = "Warehouse",
                    entranceGPS = new Vector2(37.7765f, -122.4162f),
                    detectionRadius = 30f,
                    closeProximityRadius = 10f,
                    debugColor = Color.green
                }
            };
        }

        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        #endif

        StartCoroutine(LocationUpdateRoutine());
    }

    System.Collections.IEnumerator LocationUpdateRoutine()
    {
        if (!Input.location.isEnabledByUser)
        {
            currentStatus = "Location disabled";
            yield break;
        }

        Input.location.Start(5f, 5f); // Higher accuracy for building detection

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            currentStatus = "Init timeout";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            currentStatus = "Location failed";
            yield break;
        }

        locationStatus = Input.location.status;
        
        while (true)
        {
            CheckBuildingProximity(Input.location.lastData);
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void CheckBuildingProximity(LocationInfo location)
    {
        closestBuilding = null;
        float minDistance = float.MaxValue;

        foreach (var building in buildings)
        {
            float distance = CalculatePreciseDistance(
                location.latitude, 
                location.longitude,
                building.entranceGPS.x,
                building.entranceGPS.y
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                closestBuilding = building;
            }

            if (distance <= building.closeProximityRadius)
            {
                OnBuildingEntered(building);
            }
            else if (distance <= building.detectionRadius)
            {
                OnBuildingApproached(building);
            }
        }

        distanceToBuilding = minDistance;
        UpdateStatusText();
    }

    float CalculatePreciseDistance(float lat1, float lon1, float lat2, float lon2)
    {
        // Vincenty formula implementation for higher accuracy
        const float a = 6378137f; // WGS-84 semi-major axis
        const float b = 6356752.314245f; // WGS-84 semi-minor axis
        const float f = 1f / 298.257223563f; // WGS-84 flattening
        
        float L = (lon2 - lon1) * Mathf.Deg2Rad;
        float U1 = Mathf.Atan((1f - f) * Mathf.Tan(lat1 * Mathf.Deg2Rad));
        float U2 = Mathf.Atan((1f - f) * Mathf.Tan(lat2 * Mathf.Deg2Rad));
        
        float sinU1 = Mathf.Sin(U1);
        float cosU1 = Mathf.Cos(U1);
        float sinU2 = Mathf.Sin(U2);
        float cosU2 = Mathf.Cos(U2);
        
        float lambda = L;
        float lambdaP;
        float sinSigma, cosSigma, sigma, sinAlpha, cosSqAlpha, cos2SigmaM;
        int iterLimit = 100;
        
        do {
            float sinLambda = Mathf.Sin(lambda);
            float cosLambda = Mathf.Cos(lambda);
            sinSigma = Mathf.Sqrt((cosU2 * sinLambda) * (cosU2 * sinLambda) + 
                       (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * 
                       (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));
            
            if (sinSigma == 0f) return 0f;
            
            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Mathf.Atan2(sinSigma, cosSigma);
            sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cosSqAlpha = 1f - sinAlpha * sinAlpha;
            cos2SigmaM = cosSigma - 2f * sinU1 * sinU2 / cosSqAlpha;
            
            if (float.IsNaN(cos2SigmaM)) cos2SigmaM = 0f;
            
            float C = f / 16f * cosSqAlpha * (4f + f * (4f - 3f * cosSqAlpha));
            lambdaP = lambda;
            lambda = L + (1f - C) * f * sinAlpha * 
                    (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * 
                    (-1f + 2f * cos2SigmaM * cos2SigmaM)));
        } while (Mathf.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);
        
        if (iterLimit == 0) return float.NaN;
        
        float uSq = cosSqAlpha * (a * a - b * b) / (b * b);
        float A = 1f + uSq / 16384f * (4096f + uSq * (-768f + uSq * (320f - 175f * uSq)));
        float B = uSq / 1024f * (256f + uSq * (-128f + uSq * (74f - 47f * uSq)));
        float deltaSigma = B * sinSigma * (cos2SigmaM + B / 4f * 
                         (cosSigma * (-1f + 2f * cos2SigmaM * cos2SigmaM) - 
                         B / 6f * cos2SigmaM * (-3f + 4f * sinSigma * sinSigma) * 
                         (-3f + 4f * cos2SigmaM * cos2SigmaM)));
        
        return b * A * (sigma - deltaSigma);
    }

    void OnBuildingEntered(Building building)
    {
        Debug.Log($"ENTERED BUILDING: {building.name} (Distance: {distanceToBuilding:F2}m)");
        // Trigger your building entry logic here
    }

    void OnBuildingApproached(Building building)
    {
        Debug.Log($"APPROACHING BUILDING: {building.name} (Distance: {distanceToBuilding:F2}m)");
        // Trigger your approach logic here
    }

    void UpdateStatusText()
    {
        if (closestBuilding == null)
        {
            currentStatus = "No buildings nearby";
            return;
        }

        if (distanceToBuilding <= closestBuilding.closeProximityRadius)
        {
            currentStatus = $"INSIDE {closestBuilding.name} proximity";
        }
        else if (distanceToBuilding <= closestBuilding.detectionRadius)
        {
            currentStatus = $"NEAR {closestBuilding.name} ({distanceToBuilding:F1}m)";
        }
        else
        {
            currentStatus = $"CLOSEST: {closestBuilding.name} ({distanceToBuilding:F1}m away)";
        }
    }

    void OnDestroy()
    {
        if (Input.location.isEnabledByUser)
        Input.location.Stop();
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUI.skin.label.fontSize = 30;
        GUI.Label(new Rect(10, 10, 1000, 100), $"STATUS: {currentStatus}");
        
        if (Input.location.status == LocationServiceStatus.Running)
        {
            var loc = Input.location.lastData;
            GUI.Label(new Rect(10, 50, 1000, 100), 
                $"GPS: {loc.latitude:F6}, {loc.longitude:F6}\n" +
                $"Accuracy: {loc.horizontalAccuracy:F1}m");
        }
    }
}