namespace RamRoutes.Services
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using Firebase.Firestore;
    using RamRoutes.Model;
    using System.Collections.Generic;
    using UnityEngine.SocialPlatforms;
    using Unity.VisualScripting;

    public class UserService
    {
        private FirebaseFirestore db;

        public UserService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        // public async Task UpdateUser(User user)
        // {            var docData = new Dictionary<string, object>
        //     {
        //         { "id", user.userId },
        //         { "notificationToken", user.notificationToken },
        //         { "name", user.name },
        //         { "email", user.email },
        //         { "points", user.points },
        //         { "currentBuilding", user.currentBuilding }
        //     };
        //     try
        //     {
        //         await db.Collection("users").Document(user.userId).SetAsync(docData);
        //         Debug.Log($"User {user.userId} updated in Firestore");
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"Failed to update user {user.userId}: {ex.Message}");
        //         Debug.LogError($"Points: {user.points} id: {user.userId}");

        //     }
        // }

        public async Task<User> RetrieveUserById(string userId)
        {
            try
            {
                DocumentSnapshot doc = await db.Collection("users").Document(userId).GetSnapshotAsync();
                if (doc.Exists)
                {
                    var data = doc.ToDictionary();
                    string id = data.ContainsKey("id") && data["id"] != null ? data["id"].ToString() : "";
                    string token = data.ContainsKey("notificationToken") && data["notificationToken"] != null ? data["notificationToken"].ToString() : "";
                    string name = data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "";
                    string email = data.ContainsKey("email") && data["email"] != null ? data["email"].ToString() : "";
                    int coins = data.ContainsKey("coins") ? Convert.ToInt32(data["coins"]) : 0;
                    int knowledgePoints = data.ContainsKey("knowledgePoints") ? Convert.ToInt32(data["knowledgePoints"]) : 0;
                    string currentBuilding = data.ContainsKey("currentBuilding") && data["currentBuilding"] != null ? data["currentBuilding"].ToString() : "";
                    var user = new User(id, token, name, email);
                    user.coins = coins;
                    user.knowledgePoints = knowledgePoints;
                    user.currentBuilding = currentBuilding;
                    Debug.Log($"User {id} retrieved from Firestore");
                    return user;
                }
                else
                {
                    Debug.LogWarning($"User {userId} not found in Firestore");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve user {userId}: {ex.Message}");
            }
            return null;
        }

        public async Task<User> RetrieveAndCacheCurrentUserProfile(string userId)
        {
            User user = await RetrieveUserById(userId);
            if (user != null)
            {
                string json = JsonUtility.ToJson(user);
                PlayerPrefs.SetString("current_user_profile", json);
                PlayerPrefs.Save();
                Debug.Log($"Current user profile cached locally");
            }
            return user;
        }

        /// <summary>
        /// Attempts to get the current user profile from local cache (PlayerPrefs),
        /// falls back to Firestore if not found or malformed.
        /// Returns a User object or null.
        /// </summary>
        public async Task<User> GetUserProfileCachedOrRemoteAsync(string userId)
        {
            // Try local cache first
            // if (PlayerPrefs.HasKey("current_user_profile"))
            // {
            //     string json = PlayerPrefs.GetString("current_user_profile");
            //     try
            //     {
            //         User cachedUser = JsonUtility.FromJson<User>(json);
            //         if (cachedUser != null && !string.IsNullOrEmpty(cachedUser.userId))
            //         {
            //             Debug.Log($"User profile loaded from cache: {cachedUser.userId}, {cachedUser.name}");
            //             return cachedUser;
            //         }
            //         else
            //         {
            //             Debug.LogWarning($"Cached user profile is malformed or missing userId. User id: {cachedUser.userId}. Points: {cachedUser.coins}");
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         Debug.LogWarning($"Failed to parse cached user profile: {ex.Message}");
            //     }
            // }
            // Fallback to Firestore
            User remoteUser = await RetrieveUserById(userId);
            if (remoteUser != null)
            {
                string json = JsonUtility.ToJson(remoteUser);
                PlayerPrefs.SetString("current_user_profile", json);
                PlayerPrefs.Save();
                Debug.Log($"User profile loaded from Firestore and cached: {remoteUser.userId}, {remoteUser.name}");
                return remoteUser;
            }
            Debug.LogWarning("User profile not found in cache or Firestore.");
            return null;
        }       
        //  public async Task AddPoints(string userId, int pointsToAdd)
        // {
        //     var user = await GetUserProfileCachedOrRemoteAsync(userId);
        //     if (user != null)
        //     {
        //         user.points += pointsToAdd;
        //         await UpdateUser(user);
        //         Debug.Log($"Added {pointsToAdd} points to user {userId}. New total: {user.points}");

        //         // Update cache
        //         string json = JsonUtility.ToJson(user);
        //         PlayerPrefs.SetString("current_user_profile", json);
        //         PlayerPrefs.Save();
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"Cannot add points: User {userId} not found in database");
        //     }
        // }

        // public async Task<int> GetPoints(string userId)
        // {
        //     var user = await GetUserProfileCachedOrRemoteAsync(userId);
        //     Debug.Log($"Getting user points. User: {user.userId}. Points: {user.points}");
        //     return user?.points ?? 0;
        // }

        public async Task UpdateCurrentBuilding(string userId, string buildingName)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "currentBuilding", buildingName }
                });

                // Update the cached user profile
                var user = await GetUserProfileCachedOrRemoteAsync(userId);
                if (user != null)
                {
                    user.currentBuilding = buildingName;
                    string json = JsonUtility.ToJson(user);
                    PlayerPrefs.SetString("current_user_profile", json);
                    PlayerPrefs.Save();
                    Debug.Log($"Updated current building for user {userId} to {buildingName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update current building for user {userId}: {ex.Message}");
            }
        }

        public async Task<List<User>> GetUsersInBuilding(string buildingName)
        {
            try
            {
                var users = new List<User>();
                var querySnapshot = await db.Collection("users")
                    .WhereEqualTo("currentBuilding", buildingName)
                    .GetSnapshotAsync();

                foreach (var doc in querySnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    string id = data.ContainsKey("id") && data["id"] != null ? data["id"].ToString() : "";
                    string token = data.ContainsKey("notificationToken") && data["notificationToken"] != null ? data["notificationToken"].ToString() : "";
                    string name = data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "";
                    string email = data.ContainsKey("email") && data["email"] != null ? data["email"].ToString() : "";
                    int points = data.ContainsKey("points") ? Convert.ToInt32(data["points"]) : 0;
                    string currentBuilding = data.ContainsKey("currentBuilding") && data["currentBuilding"] != null ? data["currentBuilding"].ToString() : "";

                    var user = new User(id, token, name, email);
                    // user.points = points;
                    user.currentBuilding = currentBuilding;
                    users.Add(user);
                }

                Debug.Log($"Found {users.Count} users in building {buildingName}");
                return users;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve users in building {buildingName}: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task UpdateLastLogin(string userId)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "lastLogin", Timestamp.GetCurrentTimestamp() }
                });

                Debug.Log($"Updated last login timestamp for user {userId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update last login for user {userId}: {ex.Message}");
                throw; // Re-throw to allow proper error handling in LoginManager
            }
        }

        public async Task CreateUser(string userId, string username, string email, string residenceHall)
        {
            // Defensive checks and normalization
            userId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            username = username?.Trim();
            email = email?.Trim();
            residenceHall = residenceHall?.Trim();

            if (string.IsNullOrEmpty(userId))
            {
                throw new System.ArgumentException("CreateUser called with empty userId; cannot create Firestore document path.", nameof(userId));
            }

            if (db == null)
            {
                throw new System.InvalidOperationException("Firestore db is not initialized in UserService.");
            }

            try
            {
                var now = Timestamp.FromDateTime(DateTime.UtcNow);
                var userData = new Dictionary<string, object>
                {
                    { "id", userId },
                    { "name", username },
                    { "email", email },
                    { "residenceHall", residenceHall },
                    { "points", 0 },
                    { "createdAt", now },
                    { "lastLoginAt", now },
                    { "currentBuilding", null },
                    { "emailVerified", false },
                    { "notificationToken", null }
                };

                await db.Collection("users").Document(userId).SetAsync(userData);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"UserService.CreateUser failed for userId='{userId}': {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<int> GetUserCoins(string userId)
        {
            try
            {
                var user = await GetUserProfileCachedOrRemoteAsync(userId);
                return user?.coins ?? 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get coins for user {userId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetUserKnowledgePoints(string userId)
        {
            try
            {
                var user = await GetUserProfileCachedOrRemoteAsync(userId);
                return user?.knowledgePoints ?? 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get knowledge points for user {userId}: {ex.Message}");
                return 0;
            }
        }

        public async Task UpdateUserCoins(string userId, int coins)
        {
            try
            {
                var currentCoins = await GetUserCoins(userId);

                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "coins", coins + currentCoins }
                });
                
                // Update cache if exists
                if (PlayerPrefs.HasKey("current_user_profile"))
                {
                    var json = PlayerPrefs.GetString("current_user_profile");
                    var cachedUser = JsonUtility.FromJson<User>(json);
                    if (cachedUser != null)
                    {
                        cachedUser.coins = coins;
                        PlayerPrefs.SetString("current_user_profile", JsonUtility.ToJson(cachedUser));
                        PlayerPrefs.Save();
                    }
                }
                
                Debug.Log($"Updated coins for user {userId} to {coins}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update coins for user {userId}: {ex.Message}");
            }
        }

        public async Task UpdateUserKnowledgePoints(string userId, int points)
        {
            try
            {
                var currentPoints = await GetUserKnowledgePoints(userId);

                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "knowledgePoints", points + currentPoints }
                });
                
                // Update cache if exists
                if (PlayerPrefs.HasKey("current_user_profile"))
                {
                    var json = PlayerPrefs.GetString("current_user_profile");
                    var cachedUser = JsonUtility.FromJson<User>(json);
                    if (cachedUser != null)
                    {
                        cachedUser.knowledgePoints = points;
                        PlayerPrefs.SetString("current_user_profile", JsonUtility.ToJson(cachedUser));
                        PlayerPrefs.Save();
                    }
                }
                
                Debug.Log($"Updated knowledge points for user {userId} to {points}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update knowledge points for user {userId}: {ex.Message}");
            }
        }
    }
}
