using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using RamRoutes.Services;
using RamRoutes.Model;
using Firebase.Auth;

[System.Serializable]
public struct UserBuildingEntry
{
    public string userId;
    public string userName;
    public string buildingName;
    public float timestamp;
}

public class RamsManager : MonoBehaviour
{
     private BuildingInteraction building;
    
    [Header("Stage Control")]
    [SerializeField] private bool requireTerminalStage = false;
    
    [Header("Ram Prefab")]
    [SerializeField] private GameObject ramPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private int maxRams = 10;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip despawnSound;
    [SerializeField] private AudioClip backflipClip;
    
    [Header("User Info Panel")]
    [SerializeField] private UserInfoPanel userInfoPanel;
    
    [Header("Chat System")]
    [SerializeField] private ChatManager chatManager;
    
    [Header("Player Count Display")]
    [SerializeField] private GameObject playerCountCanvasPrefab;
        [SerializeField] private GameObject footprintPrefab;

    [SerializeField] private Transform playerCountSpawnPoint;
    
    
    private User currentChatUser;
    private float lastClickTime = 0f;
    private const float CLICK_DEBOUNCE_TIME = 0.5f;
    
    private UserService userService;
    private NotificationManager notificationManager;
    private List<GameObject> spawnedRams = new List<GameObject>();
    private HashSet<string> spawnedUserIds = new HashSet<string>();
    private GameObject playerCountCanvasInstance;
    private bool hasBeenActivated = false;
    private Coroutine refreshCoroutine;
    
    private static bool hasNotifiedBuildingActivity = false;
    
    private HashSet<string> previousPlayersInThisBuilding = new HashSet<string>();
    
    private Coroutine buildingMonitorCoroutine;
    
    private List<UserBuildingEntry> pendingBuildingEntries = new List<UserBuildingEntry>();
    private Coroutine batchProcessingCoroutine;
    
    private Dictionary<string, Color> userColorAssignments = new Dictionary<string, Color>();
    private List<Color> availableColors = new List<Color>();
    private int colorIndex = 0;
    
    void Start()
    {
        building = GetComponent<BuildingInteraction>();
        userService = new UserService();
        
        notificationManager = FindObjectOfType<NotificationManager>();
        if (notificationManager != null)
        {
        }
        else
        {
            Debug.LogWarning("RamsManager: No NotificationManager found in scene - notifications will be logged to console");
        }
        
        InitializeVisibleColors();
        
        InitializePlayerCountDisplay();
        
        // Instantiate player count canvas at scene root (NOT as child of building).
        // Parenting a world-space canvas to the building causes it to be culled/clipped
        // when the camera is close. Unparented, it always renders correctly.
        if (playerCountCanvasInstance == null && playerCountCanvasPrefab != null)
        {
            playerCountCanvasInstance = Instantiate(playerCountCanvasPrefab);  // no parent — scene root
            playerCountCanvasInstance.transform.localScale = Vector3.one * 0.01f; // 1 world-unit = 100px
            
            // Position it at the spawn point's world position
            Vector3 worldPos = playerCountSpawnPoint != null
                ? playerCountSpawnPoint.position
                : transform.position + Vector3.up * 1f;
            playerCountCanvasInstance.transform.position = worldPos;
            playerCountCanvasInstance.transform.rotation = Quaternion.identity;
            playerCountCanvasInstance.SetActive(true);
            WireFootprintButton(playerCountCanvasInstance);
        }

        
        ValidateSpawnPoints();
        
    }
    
    private void ValidateSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"RamsManager on {gameObject.name}: No spawn points assigned! Rams will spawn at RamsManager position. Please assign spawn points in the inspector.");
            return;
        }
        
        int nullCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                nullCount++;
        }
        
        if (nullCount > 0)
        {
            Debug.LogWarning($"RamsManager on {gameObject.name}: {nullCount} out of {spawnPoints.Length} spawn points are null. Please assign all spawn points.");
        }
        else
        {
        }
    }
    
    [ContextMenu("Debug Spawn Points")]
    private void DebugSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
        Debug.LogWarning("No spawn points to debug!");
        return;
        }
        
    }
    
    private void InitializeVisibleColors()
    {
        availableColors.Clear();
        
        availableColors.Add(new Color(1f, 0.2f, 0.2f));
        availableColors.Add(new Color(0.2f, 0.2f, 1f));
        availableColors.Add(new Color(1f, 0.6f, 0f));
        availableColors.Add(new Color(0.8f, 0f, 0.8f));
        availableColors.Add(new Color(0.4f, 0.2f, 0.6f));
        availableColors.Add(new Color(1f, 1f, 0.2f));
        availableColors.Add(new Color(0f, 0.8f, 0.8f));
        availableColors.Add(new Color(0.8f, 0.4f, 0.2f));
        availableColors.Add(new Color(1f, 0.4f, 0.8f));
        availableColors.Add(new Color(0.2f, 0.2f, 0.2f));
        availableColors.Add(new Color(0.6f, 0.3f, 0f));
        availableColors.Add(new Color(0.2f, 0.6f, 0.8f));
        
        colorIndex = 0;
    }
    
    private async void InitializePlayerCountDisplay()
    {
        try
        {
            await Task.Delay(500);
            DisplayPlayerCount();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing player count display: {ex.Message}");
        }
    }

    private async void GetPlayersInAllBuildingsAndNotify()
    {
        try
        {
            if (hasNotifiedBuildingActivity)
            {
                return;
            }
            
            hasNotifiedBuildingActivity = true;
            
            await Task.Delay(1000);
            
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("RamsManager: No authenticated user, cannot notify about building occupancy");
                return;
            }
            
            var buildingUsers = await userService.GetUsersInAllBuildings();
            
            if (buildingUsers.Count == 0)
            {
                if (notificationManager != null)
                {
                    notificationManager.ShowNotification("Campus Status", "No players currently in any buildings");
                }
                return;
            }
            
            foreach (var kvp in buildingUsers)
            {
                string buildingName = kvp.Key;
                var users = kvp.Value;
                
                if (users.Count > 0)
                {
                    string message;
                    if (users.Count == 1)
                    {
                        message = $"{users[0].name} is in {buildingName}. Go say hi!";
                    }
                    else if (users.Count <= 3)
                    {
                        var names = users.Take(3).Select(u => u.name).ToArray();
                        message = $"{string.Join(", ", names)} are in {buildingName}. Go say hi!";
                    }
                    else
                    {
                        var firstThree = users.Take(3).Select(u => u.name).ToArray();
                        message = $"{string.Join(", ", firstThree)} and {users.Count - 3} others are in {buildingName}. Go say hi!";
                    }
                    
                    if (notificationManager != null)
                    {
                        notificationManager.ShowNotification("Building Activity", message);
                        
                        await Task.Delay(500);
                    }
                    else
                    {
                    }
                }
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RamsManager: Error getting players in all buildings: {ex.Message}");
        }
    }

    private void StartLiveBuildingMonitoring()
    {
        if (buildingMonitorCoroutine == null)
        {
            buildingMonitorCoroutine = StartCoroutine(MonitorBuildingOccupancyCoroutine());
        }
    }
    
    private IEnumerator MonitorBuildingOccupancyCoroutine()
    {
        yield return new WaitForSeconds(5f);
        
        while (true)
        {
            yield return new WaitForSeconds(3f);

            if (requireTerminalStage)
            {
                var currentStage = GameStageService.LoadStageFromPrefs();
                if (currentStage != null && currentStage.area == Stage.Terminal)
                {
                    var task = CheckForNewPlayersInBuildings();
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }
            else
            {
                var task = CheckForNewPlayersInBuildings();
                yield return new WaitUntil(() => task.IsCompleted);
            }
        }
    }
    
    private async Task CheckForNewPlayersInBuildings()
    {
        try
        {
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }
            
            string buildingName = building.buildingName;
            if (string.IsNullOrEmpty(buildingName))
            {
                return;
            }
            
            var currentUsersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);
            
            if (currentUsersInBuilding.Count > 0)
            {
                var currentUserIds = currentUsersInBuilding.Select(u => u.userId).ToHashSet();
                
                var newUserIds = currentUserIds.Except(previousPlayersInThisBuilding).ToList();
                
                if (newUserIds.Count > 0)
                {
                    var newUsers = currentUsersInBuilding.Where(u => newUserIds.Contains(u.userId)).ToList();
                    
                    foreach (var newUser in newUsers)
                    {
                        if (newUser.userId == currentUserId)
                        {
                            continue;
                        }
                        
                        var entry = new UserBuildingEntry
                        {
                            userId = newUser.userId,
                            userName = newUser.name,
                            buildingName = buildingName,
                            timestamp = Time.time
                        };
                        
                        pendingBuildingEntries.Add(entry);
                    }
                    
                    if (batchProcessingCoroutine == null && pendingBuildingEntries.Count > 0)
                    {
                        batchProcessingCoroutine = StartCoroutine(ProcessNotificationBatch());
                    }
                }
                
                previousPlayersInThisBuilding = currentUserIds;
            }
            else
            {
                previousPlayersInThisBuilding.Clear();
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RamsManager ({building.buildingName}): Error checking for new players in building: {ex.Message}");
        }
    }

    public async void OnBuildingActivated()
    {
        if (spawnedRams.Count > 0)
        {
            return;
        }
        
           
         if (requireTerminalStage)
            {
                var currentStage = GameStageService.LoadStageFromPrefs();
                if (currentStage == null || currentStage.area != Stage.Terminal)
                {
                    return;
                }
            }
            
            await SpawnRams();
            
            DisplayPlayerCount();
            
            if (refreshCoroutine == null)
            {
                refreshCoroutine = StartCoroutine(RefreshPlayersCoroutine());
            }
     
    }
    
    public void OnPlayerLeavesBuilding()
    {
        if (hasBeenActivated)
        {
            hasBeenActivated = false;
            
            try
            {
                DisplayPlayerCount();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating player count when player leaves: {ex.Message}");
            }
            
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }
            
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HandlePlayerLeavingWithDelay());
            }
            else
            {
                HandleImmediateCleanup();
            }
        }
    }
    
    private async void HandleImmediateCleanup()
    {
        ClearSpawnedRams();       
    }
    
    private IEnumerator HandlePlayerLeavingWithDelay()
    {
        yield return new WaitForSeconds(20f);
        ChatManager chat = chatManager;
          if (chat == null)
        {
            chat = FindObjectOfType<ChatManager>();
        }
        
        if (chat != null)
        {
            chat.CloseChatPanel();
            currentChatUser = null;
        }
        
        yield return StartCoroutine(DespawnRamsWithDelay());
        
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            yield break;
        }

        
    }

    private async Task SpawnRams()
    {
        if (requireTerminalStage)
        {
            var currentStage = GameStageService.LoadStageFromPrefs();
            if (currentStage == null || currentStage.area != Stage.Terminal)
            {
                return;
            }
        }
        
        string buildingName = building.buildingName;
        if (string.IsNullOrEmpty(buildingName))
        {
            Debug.LogError("Building name not set in RamsManager!");
            return;
        }

        if (ramPrefab == null)
        {
            Debug.LogError("Ram prefab not assigned in RamsManager!");
            return;
        }

        try
        {
            var usersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);

            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            

            ClearSpawnedRams();
            spawnedUserIds.Clear();

            int spawnCount = Mathf.Min(usersInBuilding.Count, maxRams);
            if (spawnCount > 0)
            {
                StartCoroutine(SpawnRamsWithDelay(usersInBuilding, spawnCount));

                foreach (var user in usersInBuilding.Take(spawnCount))
                {
                    spawnedUserIds.Add(user.userId);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn rams: {e.Message}");
        }
    }
    

    private IEnumerator RefreshPlayersCoroutine()
    {
           
         yield return new WaitForSeconds(1f);
            
               
             var task = CheckForNewPlayers();
                yield return new WaitUntil(() => task.IsCompleted);
    }
    
    private async Task CheckForNewPlayers()
    {
        if (requireTerminalStage)
        {
            var currentStage = GameStageService.LoadStageFromPrefs();
            if (currentStage == null || currentStage.area != Stage.Terminal)
            {
                return;
            }
        }
        
        string buildingName = building.buildingName;
        if (string.IsNullOrEmpty(buildingName))
        {
            return;
        }
        
        try
        {
            var usersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);

            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            var newUsers = usersInBuilding.Where(user => !spawnedUserIds.Contains(user.userId)).ToList();
            
            if (newUsers.Count > 0)
            {
                int availableSlots = maxRams - spawnedRams.Count;
                int usersToSpawn = Mathf.Min(newUsers.Count, availableSlots);
                
                if (usersToSpawn > 0)
                {
                    StartCoroutine(SpawnNewUsersWithDelay(newUsers.Take(usersToSpawn).ToList()));
                }
              
            }
            
            var currentUserIds = usersInBuilding.Select(u => u.userId).ToHashSet();
            var usersToRemove = spawnedUserIds.Where(id => !currentUserIds.Contains(id)).ToList();
            
            if (usersToRemove.Count > 0)
            {
                PlayDespawnSound();
                
                RemoveRamsForUsers(usersToRemove);
            }
            
            DisplayPlayerCount();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to check for new players: {e.Message}");
        }
    }
    

    private IEnumerator SpawnNewUsersWithDelay(List<User> newUsers)
    {
        for (int i = 0; i < newUsers.Count; i++)
        {
            var user = newUsers[i];
            int nextIndex = spawnedRams.Count;
            SpawnRamForUser(user, nextIndex);
            spawnedUserIds.Add(user.userId);
            
            if (i < newUsers.Count - 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
    
    private void RemoveRamsForUsers(List<string> userIdsToRemove)
    {
        for (int i = spawnedRams.Count - 1; i >= 0; i--)
        {
            var ram = spawnedRams[i];
            if (ram != null)
            {
                var allTexts = ram.GetComponentsInChildren<UnityEngine.UI.Text>();
                var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
                string ramUserName = "";
                if (nameText != null)
                {
                    ramUserName = nameText.text.Split('(')[0].Trim();
                }
                
                bool shouldRemove = false;
                foreach (string userIdToRemove in userIdsToRemove)
                {
                    if (spawnedUserIds.Contains(userIdToRemove))
                    {
                        shouldRemove = true;
                        spawnedUserIds.Remove(userIdToRemove);
                        break;
                    }
                }
                
                if (shouldRemove)
                {
                    Destroy(ram);
                    spawnedRams.RemoveAt(i);
                    break;
                }
            }
        }
    }
    

    private IEnumerator SpawnRamsWithDelay(List<User> users, int spawnCount)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            var user = users[i];
            SpawnRamForUser(user, i);
            
            if (i < spawnCount - 1)
            {
                yield return new WaitForSeconds(.1f);
            }
        }
    }


    private IEnumerator DespawnRamsWithDelay()
    {
        var ramsToDestroy = new List<GameObject>(spawnedRams);
        
        for (int i = 0; i < ramsToDestroy.Count; i++)
        {
            var ram = ramsToDestroy[i];
            if (ram != null)
            {
                PlayDespawnSound();
                
                Destroy(ram);
                
                if (i < ramsToDestroy.Count - 1)
                {
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }
        
        spawnedRams.Clear();
        spawnedUserIds.Clear();
    }


    private IEnumerator RandomBackflipAnimation(GameObject ramInstance)
    {
        if (ramInstance == null) yield break;
        
        Animator animator = ramInstance.GetComponent<Animator>();
        if (animator == null)
        {
            yield break;
        }
        
        yield return new WaitForSeconds(2f);
        
        while (ramInstance != null)
        {
            float waitTime = Random.Range(10f, 30f);
            yield return new WaitForSeconds(waitTime);
            
            if (ramInstance != null && animator != null)
            {
                animator.SetBool("backflip", true);
                
                StartCoroutine(ResumeMovementAfterBackflip(ramInstance, animator));
            }
        }
    }

    private IEnumerator ResumeMovementAfterBackflip(GameObject ramInstance, Animator animator)
    {
        yield return new WaitForSeconds(.5f);
        
        if (ramInstance != null && animator != null)
        {
            animator.SetBool("backflip", false);
                        animator.SetBool("idle", false);

            
        }
    }

    private void SpawnRamForUser(User user, int index)
    {
        Vector3 spawnPosition = CalculateSpawnPosition(index);

        GameObject ramInstance = Instantiate(ramPrefab, spawnPosition, Quaternion.identity);

        Transform parentTransform = transform;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnPointIndex = index % spawnPoints.Length;
            Transform selectedSpawnPoint = spawnPoints[spawnPointIndex];
            
            if (selectedSpawnPoint != null)
            {
                parentTransform = selectedSpawnPoint;
            }
            else
            {
                parentTransform = spawnPoints.FirstOrDefault(sp => sp != null) ?? transform;
            }
        }
        ramInstance.transform.SetParent(parentTransform);


        var allTexts = ramInstance.GetComponentsInChildren<UnityEngine.UI.Text>();
        
        string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        bool isCurrentPlayer = !string.IsNullOrEmpty(currentUserId) && user.userId == currentUserId;
        
        var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
        if (nameText != null)
        {
            string displayName = isCurrentPlayer ? "just me" : user.name;
            nameText.text = displayName;
        }
    
        
        var coinsText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("coins"));
        if (coinsText != null)
        {
            coinsText.text = $"{user.knowledgePoints}";
        }
      

        if (nameText == null)
        {
            var tmpTexts = ramInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            var tmpNameText = tmpTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
            if (tmpNameText != null)
            {
                string displayName = isCurrentPlayer ? "Me" : (user.name ?? "Unknown");
                tmpNameText.text = displayName;
            }
        }

        spawnedRams.Add(ramInstance);

        SetupRamClickHandler(ramInstance, user);

        ApplyUserSkinToRam(ramInstance, user);

        PlaySpawnSound();

        if (UIManager.Instance != null)
        {
            StartCoroutine(ApplyScaleAfterPopAnimation(ramInstance, user));
        }
        else
        {
            float scale = CalculateRamScaleByRank(user);
            ramInstance.transform.localScale = Vector3.one * scale;
            ApplyUniqueColorToRamText(ramInstance, user);
        }
        
        StartCoroutine(RandomBackflipAnimation(ramInstance));
        
    }

    private void OnRamClicked(User user)
    {
        float currentTime = Time.time;
        if (currentTime - lastClickTime < CLICK_DEBOUNCE_TIME)
        {
            return;
        }
        lastClickTime = currentTime;
        
        ChatManager chat = chatManager;
        if (chat == null)
        {
            chat = FindObjectOfType<ChatManager>();
        }
        
        if (chat != null)
        {
            if (currentChatUser != null && currentChatUser.userId == user.userId)
            {
                chat.CloseChatPanel();
                currentChatUser = null;
            }
            else
            {
                chat.StartChatWithUser(user);
                currentChatUser = user;
            }
        }
      
    }
    
    public void ResetCurrentChatUser()
    {
        currentChatUser = null;
    }
    
    private void SetupRamClickHandler(GameObject ramInstance, User user)
    {
        string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(currentUserId) && user.userId == currentUserId)
        {
            return;
        }
        
        RamClickHandler ramClickHandler = ramInstance.AddComponent<RamClickHandler>();
        
        ramClickHandler.Initialize(user, this);
        
    }
    
    private void ApplyUserSkinToRam(GameObject ramInstance, User user)
    {
        try
        {
            var ramAnimator = ramInstance.GetComponent<Animator>();
            if (ramAnimator == null)
            {
                return;
            }
            
            var skinManager = FindObjectOfType<SkinManager>();
            if (skinManager == null)
            {
                return;
            }
            
            RuntimeAnimatorController skinAnimator = skinManager.GetAnimatorForSkin(user.equippedSkin);
            if (skinAnimator != null)
            {
                ramAnimator.runtimeAnimatorController = skinAnimator;
            }
            else
            {
                Debug.LogWarning($"No animator controller found for skin {user.equippedSkin} - RAM will use default appearance");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to apply skin to RAM for user {user.name}: {ex.Message}");
        }
    }
    
    public void HandleRamClick(User user, GameObject ramInstance)
    {
        TriggerRamBackflip(ramInstance);
        
        PlayBackflipSound();
        
        OnRamClicked(user);
    }

    private void TriggerRamBackflip(GameObject ramInstance)
    {
        if (ramInstance == null) return;
        
        Animator animator = ramInstance.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("backflip", true);
            
            StartCoroutine(ResumeMovementAfterBackflip(ramInstance, animator));
        }
        else
        {
            Debug.LogWarning("No Animator found on clicked ram - cannot trigger backflip");
        }
    }

    private void PlayBackflipSound()
    {
        if (backflipClip != null && building != null)
        {
            var buildingAudioSource = building.GetComponent<AudioSource>();
            if (buildingAudioSource != null)
            {
                buildingAudioSource.PlayOneShot(backflipClip);
            }
            else
            {
                Debug.LogWarning("No AudioSource found on building - cannot play backflip sound");
            }
        }
        else
        {
            Debug.LogWarning("Backflip clip not assigned or building reference missing");
        }
    }
    
    private void PlaySpawnSound()
    {
        if (spawnSound != null && building != null)
        {
            var buildingAudioSource = building.GetComponent<AudioSource>();
            if (buildingAudioSource != null)
            {
                buildingAudioSource.PlayOneShot(spawnSound);
            }
            else
            {
                Debug.LogWarning("No AudioSource found on building for spawn sound!");
            }
        }
        else if (spawnSound == null)
        {
            Debug.LogWarning("Spawn sound not assigned in RamsManager!");
        }
    }
    
    private void PlayDespawnSound()
    {
        if (despawnSound != null && building != null)
        {
            var buildingAudioSource = building.GetComponent<AudioSource>();
            if (buildingAudioSource != null)
            {
            }
            else
            {
                Debug.LogWarning("No AudioSource found on building for despawn sound!");
            }
        }
        else if (despawnSound == null)
        {
            Debug.LogWarning("Despawn sound not assigned in RamsManager!");
        }
    }
    
    private Color GetUniqueColorForUser(string userId)
    {
        if (userColorAssignments.ContainsKey(userId))
        {
            return userColorAssignments[userId];
        }
        
        if (availableColors.Count == 0)
        {
            InitializeVisibleColors();
        }
        
        Color assignedColor = availableColors[colorIndex % availableColors.Count];
        userColorAssignments[userId] = assignedColor;
        
        colorIndex++;
        
        return assignedColor;
    }
    
    private void ApplyUniqueColorToRamText(GameObject ramInstance, User user)
    {
        Color userColor = GetUniqueColorForUser(user.userId);
        
        var allTexts = ramInstance.GetComponentsInChildren<UnityEngine.UI.Text>();
        var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
        var coinsText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("coins"));
        
        if (nameText != null)
        {
            nameText.color = userColor;
        }
        
        if (coinsText != null)
        {
            coinsText.color = userColor;
        }
        
        if (nameText == null || coinsText == null)
        {
            var tmpTexts = ramInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            
            
            if (coinsText == null)
            {
                var tmpCoinsText = tmpTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("coins"));
                if (tmpCoinsText != null)
                {
                    tmpCoinsText.color = userColor;
                }
            }
            
            if (nameText == null && coinsText == null)
            {
                Debug.LogWarning($"No name or knowledge points text components found to apply color to RAM for user {user.name}");
            }
        }
    }
    
    private float CalculateRamScaleByRank(User user)
    {
        int userRank = userService.CalculateUserRank(user.coins, user.knowledgePoints);
        
        const float smallScale = 1f;
        const float mediumScale = 1.5f;
        const float largeScale = 2f;
        
        switch (userRank)
        {
            case 1:
                return smallScale;
            case 2:
                return mediumScale;
            case 3:
                return largeScale;
            default:
                return smallScale;
        }
    }
    
    private float CalculateRamScale(int knowledgePoints)
    {
        const int minKnowledgePoints = 0;
        const int maxKnowledgePoints = 1500;
        const float minScale = 0.5f;
        const float maxScale = 2.0f;
        
        int clampedKP = Mathf.Clamp(knowledgePoints, minKnowledgePoints, maxKnowledgePoints);
        float normalizedKP = (float)(clampedKP - minKnowledgePoints) / (maxKnowledgePoints - minKnowledgePoints);
        return Mathf.Lerp(minScale, maxScale, normalizedKP);
    }
    
    private Vector3 CalculateSpawnPosition(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Vector3 fallbackPosition = transform.position;
            float fallbackAngle = (360f / maxRams) * index * Mathf.Deg2Rad;
            float fallbackX = fallbackPosition.x + Mathf.Cos(fallbackAngle) * spawnRadius;
            float fallbackZ = fallbackPosition.z + Mathf.Sin(fallbackAngle) * spawnRadius;
            return new Vector3(fallbackX, fallbackPosition.y, fallbackZ);
        }
        
        int spawnPointIndex = index % spawnPoints.Length;
        Transform selectedSpawnPoint = spawnPoints[spawnPointIndex];
        
        if (selectedSpawnPoint == null)
        {
            selectedSpawnPoint = spawnPoints.FirstOrDefault(sp => sp != null) ?? transform;
        }
        
        Vector3 basePosition = selectedSpawnPoint.position;
        
        int ramsPerSpawnPoint = Mathf.CeilToInt((float)maxRams / spawnPoints.Length);
        int localIndex = index / spawnPoints.Length;
        
        float angle = (360f / ramsPerSpawnPoint) * localIndex * Mathf.Deg2Rad;
        float x = basePosition.x + Mathf.Cos(angle) * spawnRadius;
        float z = basePosition.z + Mathf.Sin(angle) * spawnRadius;
        
        return new Vector3(x, basePosition.y, z);
    }
    
    private void ClearSpawnedRams()
    {
        foreach (var ram in spawnedRams)
        {
            if (ram != null)
            {
                Destroy(ram);
            }
        }
        spawnedRams.Clear();
        spawnedUserIds.Clear();
        
        userColorAssignments.Clear();
        colorIndex = 0;
    }
    
    private void ScaleRamByKnowledgePoints(GameObject ramInstance, int knowledgePoints)
    {
        const int minKnowledgePoints = 0;
        const int maxKnowledgePoints = 1000;
        const float minScale = 1.0f;
        const float maxScale = 2.0f;
        
        int clampedKP = Mathf.Clamp(knowledgePoints, minKnowledgePoints, maxKnowledgePoints);
        
        float normalizedKP = (float)(clampedKP - minKnowledgePoints) / (maxKnowledgePoints - minKnowledgePoints);
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, normalizedKP);
        
        ramInstance.transform.localScale = Vector3.one * scaleMultiplier;
        
    }

    
    private IEnumerator ApplyScaleAfterPopAnimation(GameObject ramInstance, User user)
    {
        yield return StartCoroutine(UIManager.Instance.AnimatePanelPopup(ramInstance));
        
        float scale = CalculateRamScaleByRank(user);
        ramInstance.transform.localScale = Vector3.one * scale;
        
        ApplyUniqueColorToRamText(ramInstance, user);
        
        int userRank = userService.CalculateUserRank(user.coins, user.knowledgePoints);
    }
    
    private void DisplayPlayerCount()
    {
        StartCoroutine(DisplayPlayerCountCoroutine());
    }
    
    private IEnumerator DisplayPlayerCountCoroutine()
    {
        // NOTE: simplified - always show static player count (don't hide based on stage)
        var getUsersTask = userService.GetUsersInBuilding(building.buildingName);
        
        yield return new WaitUntil(() => getUsersTask.IsCompleted);
        
        try
        {
            var playersInBuilding = getUsersTask.Result;
            int playerCount = playersInBuilding?.Count ?? 0;
            
            StartCoroutine(UpdatePlayerCountCoroutine(playerCount));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error displaying player count: {ex.Message}");
        }
    }
    
    private IEnumerator HidePlayerCountCanvas()
    {
        // Intentionally left empty to keep player count canvas always visible and static
        yield return null;
    }
    
    private IEnumerator UpdatePlayerCountCoroutine(int count)
    {
        try
        {
            if (count > 0)
            {
                if (playerCountCanvasInstance == null && playerCountCanvasPrefab != null)
                {
                    playerCountCanvasInstance = Instantiate(playerCountCanvasPrefab); // scene root, not a child
                    playerCountCanvasInstance.transform.localScale = Vector3.one * 0.01f;
                    Vector3 worldPos = playerCountSpawnPoint != null
                        ? playerCountSpawnPoint.position
                        : transform.position + Vector3.up * 1f;
                    playerCountCanvasInstance.transform.position = worldPos;
                    playerCountCanvasInstance.transform.rotation = Quaternion.identity;
                    WireFootprintButton(playerCountCanvasInstance);
                }
                
                UpdatePlayerCountDisplay(count);
            }
            else
            {
                if (playerCountCanvasInstance != null)
                {
                    playerCountCanvasInstance.SetActive(false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating player count display in coroutine: {ex.Message}");
        }
        
        yield return null;
    }
    
    private void UpdatePlayerCountDisplay(int count)
    {
        if (playerCountCanvasInstance == null) return;
        
        var countText = playerCountCanvasInstance.GetComponentInChildren<UnityEngine.UI.Text>();
        
        if (countText == null)
        {
            // access text field by its name
            // var tmpText = playerCountCanvasInstance.transform.Find("PlayerCount")?.GetComponent<TMPro.TextMeshProUGUI>();
            // var tmpText = playerCountCanvasInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            // this reads the wrong text, let's ensure we find exact Text not TMPro, and by name to avoid accidentally grabbing the "Enter Footprint" text
            var tmpText = playerCountCanvasInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>()
                .FirstOrDefault(t => t.gameObject.name == "PlayerCount");
            if (tmpText != null)
            {
                tmpText.text = count.ToString();
                playerCountCanvasInstance.SetActive(true);
                // change the image sprite big > small with animation (two seconds big two seconds small)

                return;
            }
            
            return;
        }
        
        // Static update: set text, ensure visible, keep scale fixed (no animation)
        countText.text = count.ToString();
        playerCountCanvasInstance.SetActive(true);
        playerCountCanvasInstance.transform.localScale = Vector3.one;
        
    }
    
    private IEnumerator BeatingAnimation()
    {
        // Disabled: keep player count canvas static
        yield break;
    }
    
    void Update()
    {
        // Keep the unparented player count canvas locked to the spawn point world position
        if (playerCountCanvasInstance != null && playerCountCanvasInstance.activeSelf)
        {
            Vector3 worldPos = playerCountSpawnPoint != null
                ? playerCountSpawnPoint.position
                : transform.position + Vector3.up * 1f;
            playerCountCanvasInstance.transform.position = worldPos;
        }
    }

    void LateUpdate()
    {
        // Billboard: make the canvas always face the main camera
        if (playerCountCanvasInstance != null && playerCountCanvasInstance.activeSelf && Camera.main != null)
        {
            playerCountCanvasInstance.transform.rotation = Camera.main.transform.rotation;
        }
    }
    
    public static void ResetBuildingActivityNotification()
    {
        hasNotifiedBuildingActivity = false;
    }
    
    private void OnDestroy()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
        
        if (buildingMonitorCoroutine != null)
        {
            StopCoroutine(buildingMonitorCoroutine);
            buildingMonitorCoroutine = null;
        }
        
        if (playerCountCanvasInstance != null)
        {
            Destroy(playerCountCanvasInstance);
            playerCountCanvasInstance = null;
        }
        
        ClearSpawnedRams();
    }
    
    private void SetupRamPhysics(GameObject ramInstance)
    {
        SphereCollider ramCollider = ramInstance.GetComponent<SphereCollider>();
        if (ramCollider == null)
        {
            ramCollider = ramInstance.AddComponent<SphereCollider>();
        }
        
        ramCollider.radius = 1f;
        ramCollider.isTrigger = false;
        
        Rigidbody ramRigidbody = ramInstance.GetComponent<Rigidbody>();
        if (ramRigidbody == null)
        {
            ramRigidbody = ramInstance.AddComponent<Rigidbody>();
        }
        
        ramRigidbody.mass = 1f;
        ramRigidbody.linearDamping = 5f;
        ramRigidbody.freezeRotation = true;
        
        ramInstance.layer = LayerMask.NameToLayer("Rams");
    }
    
    private IEnumerator ProcessNotificationBatch()
    {
        yield return new WaitForSeconds(10f);
        
        if (pendingBuildingEntries.Count == 0)
        {
            batchProcessingCoroutine = null;
            yield break;
        }
        
        var entriesByBuilding = pendingBuildingEntries
            .GroupBy(entry => entry.buildingName)
            .ToList();
        
        foreach (var buildingGroup in entriesByBuilding)
        {
            var buildingName = buildingGroup.Key;
            var entries = buildingGroup.ToList();
            
            string title, message;
            
            if (entries.Count == 1)
            {
                var entry = entries[0];
                title = $"{entry.userName} entered {buildingName}";
                message = $"{entry.userName} just joined {buildingName}. Go say hi!";
            }
            else if (entries.Count == 2)
            {
                var first = entries[0].userName;
                var second = entries[1].userName;
                title = $"{first} and {second} entered {buildingName}";
                message = $"{first} and {second} just joined {buildingName}. Go say hi!";
            }
            else
            {
                var firstTwo = entries.Take(2).Select(e => e.userName).ToArray();
                var remaining = entries.Count - 2;
                title = $"{firstTwo[0]}, {firstTwo[1]} and {remaining} more entered {buildingName}";
                message = $"{firstTwo[0]}, {firstTwo[1]} and {remaining} more players just joined {buildingName}. Go say hi!";
            }
            
            if (notificationManager != null)
            {
                notificationManager.ShowNotification(title, message);
                
                if (entriesByBuilding.Count > 1)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
           
        }
        
        pendingBuildingEntries.Clear();
        batchProcessingCoroutine = null;
    }

    // ── Footprint ────────────────────────────────────────────────────────────

    private void WireFootprintButton(GameObject canvasInstance)
    {
        var addBtn = canvasInstance.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(b => b.name == "add-footprint");
        var input = canvasInstance.GetComponentsInChildren<InputField>(true)
            .FirstOrDefault(f => f.name == "Input");

        var typeDropdown = canvasInstance.GetComponentsInChildren<Dropdown>(true)
            .FirstOrDefault(d => d.name == "TypeDropdown");
        if (addBtn == null || input == null || typeDropdown == null)
        {
            Debug.LogWarning("[RamsManager] WireFootprintButton: could not find 'add-footprint' button, 'footprint' input field, or 'TypeDropdown' on canvas.");
            return;
        }

        // Hide the input field until the button is clicked
        input.gameObject.SetActive(false);
        typeDropdown.gameObject.SetActive(false);

        addBtn.onClick.RemoveAllListeners();
        addBtn.onClick.AddListener(() =>
        {
            bool isOpen = input.gameObject.activeSelf;
            if (!isOpen)
            {
                input.gameObject.SetActive(true);
                typeDropdown.gameObject.SetActive(true);
                input.text = "";
                input.ActivateInputField();
            }
            else
            {
                _ = PublishFootprintAsync(input, canvasInstance, typeDropdown);
            }
        });

        // Load existing footprints for this building on startup
        _ = LoadBuildingFootprintsAsync(canvasInstance);
    }

    private async Task LoadBuildingFootprintsAsync(GameObject canvasInstance)
    {
        if (footprintPrefab == null)
        {
            Debug.LogWarning("[RamsManager] footprintPrefab is not assigned.");
            return;
        }

        // Find the "footprints" parent with VerticalLayoutGroup
        Transform container = FindChildByName(canvasInstance.transform, "footprints");
        if (container == null)
        {
            Debug.LogWarning("[RamsManager] Could not find 'footprints' container in canvas.");
            return;
        }

        // Clear existing items
        foreach (Transform child in container)
            Destroy(child.gameObject);

        string buildingId = building != null ? building.buildingName : "unknown";

        try
        {
            var svc = new FootprintService();
            var all = await svc.GetByBuildingAsync(buildingId);

            // Sort by createdAt descending, take 2 most recent
            var recent = all
                .OrderByDescending(f => f.createdAt)
                .Take(2)
                .ToList();

            foreach (var fp in recent)
            {
                var item = Instantiate(footprintPrefab, container);

                // Capture the prefab's original scale before zeroing it
                Vector3 originalScale = item.transform.localScale;
                item.transform.localScale = Vector3.zero;

                var txt = item.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(t => t.name == "text");
                if (txt != null)
                    txt.text = fp.text;

                var posterName = item.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(t => t.name == "poster-username");
                if (posterName != null)
                {
                    try
                    {
                        var user = await userService.GetUserProfileCachedOrRemoteAsync(fp.makerId);
                        posterName.text = user != null ? user.name : fp.makerId;
                    }
                    catch
                    {
                        posterName.text = fp.makerId;
                    }
                }

                var timeText = item.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(t => t.name == "post-time");
                if (timeText != null)
                    timeText.text = FormatRelativeTime(fp.createdAt);

                StartCoroutine(PopInFootprintItem(item, originalScale));

                // Stagger each item
                await Task.Delay(150);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RamsManager] LoadBuildingFootprintsAsync failed: {ex.Message}");
        }
    }

    // Recursively find a child Transform by name
    private Transform FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var found = FindChildByName(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private string FormatRelativeTime(long createdAtMs)
    {
        if (createdAtMs <= 0) return "";
        long nowMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long deltaMs = nowMs - createdAtMs;
        if (deltaMs < 0) return "now";

        long deltaSec = deltaMs / 1000;
        if (deltaSec < 10)  return "now";
        if (deltaSec < 60)  return $"{deltaSec}s ago";
        long deltaMin = deltaSec / 60;
        if (deltaMin < 60)  return $"{deltaMin}m ago";
        long deltaHr = deltaMin / 60;
        return $"{deltaHr}h ago";
    }

    private IEnumerator PopInFootprintItem(GameObject item, Vector3 originalScale)
    {
        if (item == null) yield break;

        float duration = 0.25f;
        float elapsed  = 0f;
        Vector3 overshoot = originalScale * 1.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = t < 0.8f
                ? t / 0.8f              // 0 → 1 (normalised) during first 80%
                : 1f + (1f - (t - 0.8f) / 0.2f) * 0.1f; // 1.1 → 1.0 during last 20%
            item.transform.localScale = originalScale * scale;
            yield return null;
        }

        item.transform.localScale = originalScale;
    }

    private bool ValidateFootprintText(string text)
    {
        return text != null && text.Length >= 10 && text.Length <= 60;
    }

    private async Task PublishFootprintAsync(InputField input, GameObject canvasInstance, Dropdown typeDropdown)
    {
        string text = input.text?.Trim();
        FootprintType type = (FootprintType)typeDropdown.value;

        if (!ValidateFootprintText(text))
        {
            Debug.LogWarning("[RamsManager] Footprint must be between 10 and 60 characters.");
            return;
        }

        string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[RamsManager] PublishFootprint: no authenticated user.");
            return;
        }

        string buildingId = building != null ? building.buildingName : "unknown";

        try
        {
            var svc = new FootprintService();
            await svc.CreateAsync(new Footprint(text, userId, buildingId, type));

            // Hide input field after successful submission
            input.text = "";
            input.gameObject.SetActive(false);

            UIManager.Instance.ShowQuickUpdate("footprint submitted");
            Debug.Log($"[RamsManager] Footprint published for building '{buildingId}'.");

            // Refresh the displayed footprints
            await LoadBuildingFootprintsAsync(canvasInstance);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RamsManager] Failed to publish footprint: {ex.Message}");
        }
    }
}
