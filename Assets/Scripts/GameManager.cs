using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamRoutes.Services;
    using Firebase.Firestore;
using System;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameTrial currentTrial;
    public List<GameTrial> allTrials = new List<GameTrial>();
    public int gameLevel = 0;

    private UserService userService;
    private bool isQuitting = false;
        private FirebaseFirestore db;

    private void Awake()
    {
        db = FirebaseFirestore.DefaultInstance;
        if (Instance == null)
        {
            Instance = this;
            userService = new UserService();
            DontDestroyOnLoad(gameObject); // Persist across scenes

            // Subscribe to scene change events
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log($"GameManager created in scene: {gameObject.scene.name}");
            InitializeTrials();
        }
        else
        {
            Debug.Log($"Duplicate GameManager destroyed in scene: {gameObject.scene.name}");
            Destroy(gameObject);
        }
    }

    private void InitializeTrials()
    {
        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 1: Sorting Trash",
            trialObjective = "Collect and deposit 20 litter items using 'C' key. Navigate to the right to find more CEOs.",
            trialNumber = 1,
            timeLimit = 330f,
            targetTrash = 10,
            targetRecycling = 10
        });

        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 2: Tree Planting",
            trialObjective = "Interact with buidlings using 'V' to get saplings to plant.",
            trialNumber = 2,
            timeLimit = 360f,
            targetTreesPlanted = 4
        });
        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 3: Fill my Cup",
            trialObjective = "Call spaceship using 'E' to find Earth/Gaia. Use the eagle by Gaia to water the trees you planted",
            trialNumber = 3,
            timeLimit = 240f,
            targetTreesWatered = 4
        });

        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 4: Deliver the Magic Box",
            trialObjective = "Take the magic box from Venus to Pluto. Use spaceship.",
            trialNumber = 4,
            timeLimit = 240f,
        });


        LoadTrial(gameLevel);
    }

    public void LoadTrial(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < allTrials.Count)
        {
            currentTrial = allTrials[levelIndex];
            currentTrial.Initialize();
            currentTrial.OnTrialComplete += HandleTrialComplete;
        }
    }

    private void HandleTrialComplete()
    {
        if (currentTrial.trialNumber == 1)
        {
            RemoveObjectsWithTag("Trash");
            RemoveObjectsWithTag("Recyclable");

        }
        UIManager.Instance.OnTrialComplete.Invoke();
    }

    void RemoveObjectsWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
    }


    public void AddCoins(int amount)
    {
        currentTrial.AddCoins(amount);
    }

    public void AddTrash(int amount)
    {
        currentTrial.AddTrash(amount);
        Debug.Log($"Trash Collected: {currentTrial.currentTrash}/{currentTrial.targetTrash}");
    }

    public void AddBottles(int amount)
    {
        currentTrial.AddRecycling(amount);
        Debug.Log($"Recycling Collected: {currentTrial.currentRecycling}/{currentTrial.targetRecycling}");
    }

    public void PlantTree()
    {
        currentTrial.AddTrees(1);
        Debug.Log($"Trees Planted: {currentTrial.currentTreesPlanted}/{currentTrial.targetTreesPlanted}");
    }
    public void WaterTree()
    {
        currentTrial.WaterTrees(1);
        Debug.Log($"Trees watered: {currentTrial.currentTreesWatered}/{currentTrial.targetTreesWatered}");
    }

    public void SetGameLevel(int level)
    {
        gameLevel = level;
        LoadTrial(gameLevel);
        Debug.Log($"Loaded Trial: {currentTrial.trialName}");
    }

    public void ResetLevel()
    {
        currentTrial.Initialize();
        Debug.Log($"Reset Trial: {currentTrial.trialName}");
    }

    public void ResetTemporaryState()
    {
        currentTrial.Initialize();
        Debug.Log($"Reset Temporary State for: {currentTrial.trialName}");
    }

    #region Application Lifecycle Management

    /// <summary>
    /// Called when a scene is unloaded - clear user's building when leaving game scenes
    /// </summary>
    /// <param name="scene">The scene being unloaded</param>
    private async void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"Scene unloaded: {scene.name}");

        // Clear building when leaving specific game scenes that contain buildings
        if (IsGameplayScene(scene.name))
        {
            Debug.Log($"Leaving gameplay scene {scene.name} - clearing user's current building");
            await ClearCurrentUserBuilding();
        }
    }

    /// <summary>
    /// Called when a scene is loaded - useful for debugging/logging
    /// </summary>
    /// <param name="scene">The scene being loaded</param>
    /// <param name="mode">The load scene mode</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name} (Mode: {mode})");
    }

    /// <summary>
    /// Determines if a scene is a gameplay scene that contains buildings
    /// Add scene names here that should trigger building cleanup when left
    /// </summary>
    /// <param name="sceneName">Name of the scene to check</param>
    /// <returns>True if it's a gameplay scene with buildings</returns>
    private bool IsGameplayScene(string sceneName)
    {
        // Add your gameplay scene names here
        string[] gameplayScenes = {
            "LevelRPG",           // Main gameplay scene based on SceneManager calls I found
            "MainGame",           // Common name for main game scenes
            "GameWorld",          // Another common name
            "Level1",             // In case you have numbered levels
            "Campus"              // If campus is a scene name
        };

        foreach (string gameplayScene in gameplayScenes)
        {
            if (sceneName.Equals(gameplayScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Called when the application is paused (Android) or minimized
    /// </summary>
    /// <param name="pauseStatus">True if paused, false if resumed</param>
    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && !isQuitting)
        {
            Debug.Log("Application paused - clearing user's current building");
            await ClearCurrentUserBuilding();
        }
    }

    /// <summary>
    /// Called when the application loses or gains focus (iOS/Desktop)
    /// </summary>
    /// <param name="hasFocus">True if has focus, false if lost focus</param>
    private async void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !isQuitting)
        {
            Debug.Log("Application lost focus - clearing user's current building");
            await ClearCurrentUserBuilding();
        }
    }

    /// <summary>
    /// Called when the application is quitting
    /// This is the most reliable method for detecting app termination
    /// </summary>
    private async void OnApplicationQuit()
    {
        isQuitting = true;
        Debug.Log("Application quitting - clearing user's current building");
        await ClearCurrentUserBuilding();
    }

    /// <summary>
    /// Called when this GameObject is destroyed
    /// Fallback method in case the object is destroyed before app quit
    /// </summary>
    private async void OnDestroy()
    {
        // Unsubscribe from scene events to prevent memory leaks
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (!isQuitting)
        {
            Debug.Log("GameManager destroyed - clearing user's current building");
            await ClearCurrentUserBuilding();
        }
    }

    /// <summary>
    /// Sets the current user's building to null when they exit the game
    /// </summary>
    private async Task ClearCurrentUserBuilding()
    {
        try
        {
            // Check if Firebase is still available and user is authenticated
            if (Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser == null)
            {
                Debug.Log("No authenticated user found, skipping building cleanup");
                return;
            }

            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                await ClearCurrentBuildingFromFirestore(currentUserId);
                Debug.Log($"Successfully cleared current building for user {currentUserId} on app lifecycle event");
            }
            else
            {
                Debug.Log("User ID is empty, skipping building cleanup");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to clear current building on app lifecycle event: {e.Message}");
        }
    }

    private async Task ClearCurrentBuildingFromFirestore(string userId)
    {
        try
        {
            var userDoc = db.Collection("users").Document(userId);
            await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "currentBuilding", null }
                });

            // Update the cached user profile
            // var user = await GetUserProfileCachedOrRemoteAsync(userId);
            // if (user != null)
            // {
            //     user.currentBuilding = null;
            //     string json = JsonUtility.ToJson(user);
            //     PlayerPrefs.SetString("current_user_profile", json);
            //     PlayerPrefs.Save();
            //     Debug.Log($"Updated current building for user {userId} to null");
            // }
                                    Debug.Log($"Updated current building for user {userId} to null");

            }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update current building for user {userId}: {ex.Message}");
        }
    }
    
    #endregion
}