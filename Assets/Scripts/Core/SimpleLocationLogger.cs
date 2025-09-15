using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using RamRoutes.Services;

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

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool clearCacheOnStart = false;
    [Tooltip("Simulates GPS being enabled even if it's not")]
    [SerializeField] private bool simulateGpsEnabled = false;

    [Header("Location Settings")]
    public Building[] buildings = new Building[2]; // Make public for BuildingInteraction access
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private Canvas locationDisabledCanvas;
    
    [Header("Building Approach UI")]
    [SerializeField] private GameObject buildingApproachPanel;
    [SerializeField] private Text buildingNameText;
    // Panel now stays on screen - no duration needed
    // [SerializeField] private float panelDisplayDuration = 3f;
    // [SerializeField] private bool simulateBuildingEntry = false;
//change me later

    private LocationServiceStatus locationStatus;
    private string currentStatus = "Initializing...";
    private Building closestBuilding;
    private float distanceToBuilding;
    private float nextUpdateTime = 0f;
    private float secondsRemaining = 0f;
    // Removed tracking variables since panel stays visible

    public delegate void BuildingEvent(Building building);
    public static event BuildingEvent OnApproachBuilding;
    public static event BuildingEvent OnEnterBuilding;

    private void ClearCachedDataIfNeeded()
    {
        // Clear local PlayerPrefs cache
        PlayerPrefs.DeleteKey("unlocked_buildings_cache");
        PlayerPrefs.Save();
        Debug.Log("Cleared unlocked buildings cache");
    }

    void Start()
    {
        // Initialize building approach panel as visible
        if (buildingApproachPanel != null)
        {
            buildingApproachPanel.SetActive(true);
            UpdateBuildingStatusPanel(); // Set initial status
        }
        
        // Clear cache if enabled
        if (clearCacheOnStart)
        {
            ClearCachedDataIfNeeded();
        }
        
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

        nextUpdateTime = Time.time + updateInterval;
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
            nextUpdateTime = Time.time + updateInterval;
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
        UpdateBuildingStatusPanel(); // Update the persistent panel
    }

    public float CalculatePreciseDistance(float lat1, float lon1, float lat2, float lon2)
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
        
        // Show panel with "Entering" status if not already shown for this building
        if (lastNotifiedBuilding == null || lastNotifiedBuilding.name != building.name || !isPanelCurrentlyShown)
        {
            ShowBuildingStatusPanel(building.name, "Entering");
            lastNotifiedBuilding = building;
        }
        
        OnEnterBuilding?.Invoke(building);
        // Trigger your building entry logic here
    }

    void OnBuildingApproached(Building building)
    {
        Debug.Log($"APPROACHING BUILDING: {building.name} (Distance: {distanceToBuilding:F2}m)");
        
        // Show panel with "Approaching" status if not already shown for this building
        if (lastNotifiedBuilding == null || lastNotifiedBuilding.name != building.name || !isPanelCurrentlyShown)
        {
            ShowBuildingStatusPanel(building.name, "Approaching");
            lastNotifiedBuilding = building;
        }
        
        OnApproachBuilding?.Invoke(building);
        
        // Trigger your approach logic here
    }

    /// <summary>
    /// Shows the building status panel with the building name and status
    /// </summary>
    private void ShowBuildingStatusPanel(string buildingName, string status)
    {
        if (buildingApproachPanel != null && buildingNameText != null)
        {
            // Set concise text: "Status Building"
            buildingNameText.text = $"{status} {buildingName}";
            
            // Show the panel only if not already shown
            if (!isPanelCurrentlyShown)
            {
                buildingApproachPanel.SetActive(true);
                isPanelCurrentlyShown = true;
                
                // Start coroutine to hide the panel after specified duration
                StartCoroutine(HidePanelAfterDelay());
                
                Debug.Log($"Showing building status panel: {status} {buildingName}");
            }
            else
            {
                // Just update the text if panel is already shown
                Debug.Log($"Updated building status panel text: {status} {buildingName}");
            }
        }
        else
        {
            Debug.LogWarning("Building approach panel or text component not assigned!");
        }
    }
    
    /// <summary>
    /// Coroutine to hide the panel after a delay
    /// </summary>
    private System.Collections.IEnumerator HidePanelAfterDelay()
    {
        yield return new WaitForSeconds(panelDisplayDuration);
        
        if (buildingApproachPanel != null)
        {
            buildingApproachPanel.SetActive(false);
            isPanelCurrentlyShown = false;
        }
    }    void UpdateStatusText()
    {
        if (closestBuilding == null)
        {
            currentStatus = "No buildings nearby";
            lastNotifiedBuilding = null; // Reset when no buildings are nearby
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
            lastNotifiedBuilding = null; // Reset when far from all buildings
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
        // Status label
        GUI.Label(new Rect(10, 10, 1000, 40), $"STATUS: {currentStatus}");
        // GPS label
        if (Input.location.status == LocationServiceStatus.Running)
        {
            var loc = Input.location.lastData;
            GUI.Label(new Rect(10, 55, 1000, 40), 
                $"GPS: {loc.latitude:F6}, {loc.longitude:F6}    Accuracy: {loc.horizontalAccuracy:F1}m");
        }
        // Countdown label (move down to avoid overlap)
        GUI.Label(new Rect(10, 100, 1000, 40), $"Next location update in: {secondsRemaining:F1} seconds");
    }

    void Update()
    {
        bool locationEnabled = (Input.location.isEnabledByUser || simulateGpsEnabled) && Input.location.status != LocationServiceStatus.Failed;
        if (locationDisabledCanvas != null)
        {
            locationDisabledCanvas.gameObject.SetActive(!locationEnabled);
            if (!locationEnabled)
            {
                if (Time.timeScale != 0) Time.timeScale = 0; // Pause game
            }
            else
            {
                if (Time.timeScale != 1) Time.timeScale = 1; // Resume game
                if (locationEnabled && !Input.location.isEnabledByUser)
                {
                    Input.location.Start(5f, 5f);
                }
            }
        }

        // Countdown logic for next location update
        secondsRemaining = Mathf.Max(0f, nextUpdateTime - Time.time);

        // if (simulateBuildingEntry && buildings[3]!= null)
        // {
        //     OnBuildingEntered(buildings[3]);
        //     simulateBuildingEntry = false;
        // }
    }

    private void UpdateBuildingStatusPanel()
    {
        if (buildingApproachText == null) return;

        if (currentBuilding != null)
        {
            buildingApproachText.text = $"Inside Building: {currentBuilding}";
        }
        else if (approachingBuilding)
        {
            buildingApproachText.text = $"Approaching Building: {closestBuildingName}";
        }
        else if (!string.IsNullOrEmpty(closestBuildingName))
        {
            buildingApproachText.text = $"Closest building: {closestBuildingName}";
        }
        else
        {
            buildingApproachText.text = "No buildings nearby";
        }
    }
}