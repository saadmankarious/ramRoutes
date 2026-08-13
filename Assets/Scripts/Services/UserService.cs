namespace RamRoutes.Services
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using Firebase.Firestore;
    using RamRoutes.Model;
    using System.Collections.Generic;
    using System.Linq;
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
                    string currentPhysicalBuilding = data.ContainsKey("currentPhysicalBuilding") && data["currentPhysicalBuilding"] != null ? data["currentPhysicalBuilding"].ToString() : "";
                    string residenceHall = data.ContainsKey("residenceHall") && data["residenceHall"] != null ? data["residenceHall"].ToString() : "Not specified";
                    string bio = data.ContainsKey("bio") && data["bio"] != null ? data["bio"].ToString() : "";
                    
                    // Handle friends list
                    List<string> friends = new List<string>();
                    if (data.ContainsKey("friends") && data["friends"] != null)
                    {
                        var friendsData = data["friends"];
                        if (friendsData is List<object> friendsList)
                        {
                            foreach (var friend in friendsList)
                            {
                                if (friend != null)
                                {
                                    friends.Add(friend.ToString());
                                }
                            }
                        }
                    }
                    
                    // Handle interests list (tag preferences used for event recommendations)
                    List<string> interests = new List<string>();
                    if (data.ContainsKey("interests") && data["interests"] != null)
                    {
                        var interestsData = data["interests"];
                        if (interestsData is List<object> interestsList)
                        {
                            foreach (var interest in interestsList)
                            {
                                if (interest != null)
                                {
                                    interests.Add(interest.ToString());
                                }
                            }
                        }
                    }

                    var user = new User(id, token, name, email);
                    user.coins = coins;
                    user.knowledgePoints = knowledgePoints;
                    user.currentBuilding = currentBuilding;
                    user.currentPhysicalBuilding = currentPhysicalBuilding;
                    user.residenceHall = residenceHall;
                    user.bio = bio;
                    user.friends = friends;
                    user.interests = interests;

                    string statusStr = data.ContainsKey("status") && data["status"] != null ? data["status"].ToString() : "Studying";
                    user.SetStatusFromString(statusStr);

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

        public async Task<User> RetrieveUserByName(string username)
        {
            try
            {
                Query query = db.Collection("users").WhereEqualTo("name", username).Limit(1);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
                
                var doc = querySnapshot.Documents.FirstOrDefault();
                if (doc != null && doc.Exists)
                {
                    var data = doc.ToDictionary();
                    string id = data.ContainsKey("id") && data["id"] != null ? data["id"].ToString() : "";
                    string token = data.ContainsKey("notificationToken") && data["notificationToken"] != null ? data["notificationToken"].ToString() : "";
                    string name = data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "";
                    string email = data.ContainsKey("email") && data["email"] != null ? data["email"].ToString() : "";
                    int coins = data.ContainsKey("coins") ? Convert.ToInt32(data["coins"]) : 0;
                    int knowledgePoints = data.ContainsKey("knowledgePoints") ? Convert.ToInt32(data["knowledgePoints"]) : 0;
                    string currentBuilding = data.ContainsKey("currentBuilding") && data["currentBuilding"] != null ? data["currentBuilding"].ToString() : "";
                    string residenceHall = data.ContainsKey("residenceHall") && data["residenceHall"] != null ? data["residenceHall"].ToString() : "Not specified";
                    string bio = data.ContainsKey("bio") && data["bio"] != null ? data["bio"].ToString() : "";
                    
                    // Handle friends list
                    List<string> friends = new List<string>();
                    if (data.ContainsKey("friends") && data["friends"] != null)
                    {
                        var friendsData = data["friends"];
                        if (friendsData is List<object> friendsList)
                        {
                            foreach (var friend in friendsList)
                            {
                                if (friend != null)
                                {
                                    friends.Add(friend.ToString());
                                }
                            }
                        }
                    }
                    
                    var user = new User(id, token, name, email);
                    user.coins = coins;
                    user.knowledgePoints = knowledgePoints;
                    user.currentBuilding = currentBuilding;
                    user.residenceHall = residenceHall;
                    user.bio = bio;
                    user.friends = friends;

                    string statusStr = data.ContainsKey("status") && data["status"] != null ? data["status"].ToString() : "Studying";
                    user.SetStatusFromString(statusStr);

                    return user;
                }
                else
                {
                    Debug.LogWarning($"User with name '{username}' not found in Firestore");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve user by name '{username}': {ex.Message}");
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
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update current building for user {userId}: {ex.Message}");
            }
        }

                public async Task UpdateCurrentPhysicalBuilding(string userId, string buildingName)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "currentPhysicalBuilding", buildingName }
                });

                // Update the cached user profile
                var user = await GetUserProfileCachedOrRemoteAsync(userId);
                if (user != null)
                {
                    user.currentPhysicalBuilding = buildingName;
                    string json = JsonUtility.ToJson(user);
                    PlayerPrefs.SetString("current_user_profile", json);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update current building for user {userId}: {ex.Message}");
            }
        }

        /*
        // DEPRECATED: Whispers are now tracked through inventory items, not user profile
        public async Task<bool> UpdateWhispers(string userId, WhisperType whisperType)
        {
            // This method is no longer used - whispers are managed through inventory system
            return true;
        }

        public async Task<List<WhisperType>> GetUserWhispers(string userId)
        {
            // This method is no longer used - whispers are managed through inventory system
            return new List<WhisperType>();
        }
        */

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

                return users;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve users in building {buildingName}: {ex.Message}");
                return new List<User>();
            }
        }

        /// <summary>
        /// Gets users currently in a building with their complete point data for live display
        /// </summary>
        /// <param name="buildingName">The name of the building to query</param>
        /// <returns>List of users with coins and knowledge points included</returns>
        public async Task<List<User>> GetUsersInBuildingWithPoints(string buildingName)
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
                    int coins = data.ContainsKey("coins") ? Convert.ToInt32(data["coins"]) : 0;
                    int knowledgePoints = data.ContainsKey("knowledgePoints") ? Convert.ToInt32(data["knowledgePoints"]) : 0;
                    string currentBuilding = data.ContainsKey("currentBuilding") && data["currentBuilding"] != null ? data["currentBuilding"].ToString() : "";
                    string residenceHall = data.ContainsKey("residenceHall") && data["residenceHall"] != null ? data["residenceHall"].ToString() : "";
                    string bio = data.ContainsKey("bio") && data["bio"] != null ? data["bio"].ToString() : "";
                    string equippedSkin = data.ContainsKey("equippedSkin") && data["equippedSkin"] != null ? data["equippedSkin"].ToString() : "Default";
                    var user = new User(id, token, name, email);
                    user.coins = coins;
                    user.knowledgePoints = knowledgePoints;
                    user.currentBuilding = currentBuilding;
                    user.residenceHall = residenceHall;
                    user.bio = bio;
                    user.SetEquippedSkinFromString(equippedSkin);

                    string statusStr = data.ContainsKey("status") && data["status"] != null ? data["status"].ToString() : "Studying";
                    user.SetStatusFromString(statusStr);

                    users.Add(user);
                }

                return users;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve users with points in building {buildingName}: {ex.Message}");
                return new List<User>();
            }
        }

        /// <summary>
        /// Gets users currently in ALL buildings, grouped by building name
        /// Detects when currentBuilding is not null and includes those users
        /// </summary>
        /// <returns>Dictionary where key is building name and value is list of users in that building</returns>
        public async Task<Dictionary<string, List<User>>> GetUsersInAllBuildings()
        {
            try
            {
                var buildingUsers = new Dictionary<string, List<User>>();
                
                // Query all users where currentBuilding is not null/empty
                var querySnapshot = await db.Collection("users")
                    .WhereGreaterThan("currentBuilding", "")  // This excludes null and empty strings
                    .GetSnapshotAsync();

                foreach (var doc in querySnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    
                    // Extract user data
                    string id = data.ContainsKey("id") && data["id"] != null ? data["id"].ToString() : "";
                    string token = data.ContainsKey("notificationToken") && data["notificationToken"] != null ? data["notificationToken"].ToString() : "";
                    string name = data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "";
                    string email = data.ContainsKey("email") && data["email"] != null ? data["email"].ToString() : "";
                    int coins = data.ContainsKey("coins") ? Convert.ToInt32(data["coins"]) : 0;
                    int knowledgePoints = data.ContainsKey("knowledgePoints") ? Convert.ToInt32(data["knowledgePoints"]) : 0;
                    string currentBuilding = data.ContainsKey("currentBuilding") && data["currentBuilding"] != null ? data["currentBuilding"].ToString() : "";
                    string residenceHall = data.ContainsKey("residenceHall") && data["residenceHall"] != null ? data["residenceHall"].ToString() : "";
                    string bio = data.ContainsKey("bio") && data["bio"] != null ? data["bio"].ToString() : "";
                    string equippedSkin = data.ContainsKey("equippedSkin") && data["equippedSkin"] != null ? data["equippedSkin"].ToString() : "Default";
                    
                    // Only process users with valid currentBuilding
                    if (!string.IsNullOrEmpty(currentBuilding))
                    {
                        var user = new User(id, token, name, email);
                        user.coins = coins;
                        user.knowledgePoints = knowledgePoints;
                        user.currentBuilding = currentBuilding;
                        user.residenceHall = residenceHall;
                        user.bio = bio;
                        user.SetEquippedSkinFromString(equippedSkin);
                        
                        // Add user to the appropriate building list
                        if (!buildingUsers.ContainsKey(currentBuilding))
                        {
                            buildingUsers[currentBuilding] = new List<User>();
                        }
                        buildingUsers[currentBuilding].Add(user);
                    }
                }

                return buildingUsers;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve users in all buildings: {ex.Message}");
                return new Dictionary<string, List<User>>();
            }
        }

        /// <summary>
        /// Gets a simplified list of all users currently in any building with just basic info
        /// Useful for notifications without heavy data loading
        /// </summary>
        /// <returns>List of users currently in buildings</returns>
        public async Task<List<User>> GetAllUsersInBuildings()
        {
            try
            {
                var users = new List<User>();
                
                // Query all users where currentBuilding is not null/empty
                var querySnapshot = await db.Collection("users")
                    .WhereGreaterThan("currentBuilding", "")
                    .GetSnapshotAsync();

                foreach (var doc in querySnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    
                    string id = data.ContainsKey("id") && data["id"] != null ? data["id"].ToString() : "";
                    string token = data.ContainsKey("notificationToken") && data["notificationToken"] != null ? data["notificationToken"].ToString() : "";
                    string name = data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "";
                    string email = data.ContainsKey("email") && data["email"] != null ? data["email"].ToString() : "";
                    string currentBuilding = data.ContainsKey("currentBuilding") && data["currentBuilding"] != null ? data["currentBuilding"].ToString() : "";
                    
                    // Only process users with valid currentBuilding
                    if (!string.IsNullOrEmpty(currentBuilding))
                    {
                        var user = new User(id, token, name, email);
                        user.currentBuilding = currentBuilding;
                        users.Add(user);
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve all users in buildings: {ex.Message}");
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

            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update last login for user {userId}: {ex.Message}");
                throw; // Re-throw to allow proper error handling in LoginManager
            }
        }

        public async Task CreateUser(string userId, string username, string email, string residenceHall = null)
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
                
                // Initialize new users with starting values
                int startingCoins = 60;           // Give new users 60 coins
                int startingKnowledgePoints = 20; // Give new users 20 KB (knowledge points)
                int userRank = CalculateUserRank(startingCoins, startingKnowledgePoints);
                
                var userData = new Dictionary<string, object>
                {
                    { "id", userId },
                    { "name", username },
                    { "email", email },
                    { "residenceHall", residenceHall },
                    { "bio", "" }, // Initialize with empty bio
                    { "points", 0 }, // Legacy field, keep for compatibility
                    { "coins", startingCoins },
                    { "knowledgePoints", startingKnowledgePoints },
                    { "rank", userRank },
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
                // if (PlayerPrefs.HasKey("current_user_profile"))
                // {
                //     var json = PlayerPrefs.GetString("current_user_profile");
                //     var cachedUser = JsonUtility.FromJson<User>(json);
                //     if (cachedUser != null)
                //     {
                //         cachedUser.coins = coins;
                //         PlayerPrefs.SetString("current_user_profile", JsonUtility.ToJson(cachedUser));
                //         PlayerPrefs.Save();
                //     }
                // }
                
                // Debug.Log($"Updated coins for user {userId} to {coins}");
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
                
                // // Update cache if exists
                // if (PlayerPrefs.HasKey("current_user_profile"))
                // {
                //     var json = PlayerPrefs.GetString("current_user_profile");
                //     var cachedUser = JsonUtility.FromJson<User>(json);
                //     if (cachedUser != null)
                //     {
                //         cachedUser.knowledgePoints = points;
                //         PlayerPrefs.SetString("current_user_profile", JsonUtility.ToJson(cachedUser));
                //         PlayerPrefs.Save();
                //     }
                // }
                
                // Debug.Log($"Updated knowledge points for user {userId} to {points}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update knowledge points for user {userId}: {ex.Message}");
            }
        }

        public async Task UpdateUserBio(string userId, string bio)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "bio", bio }
                });
                
                Debug.Log($"Updated bio for user {userId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update bio for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserInterests(string userId, List<string> interests)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "interests", interests }
                });

                Debug.Log($"Updated interests for user {userId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update interests for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserStatus(string userId, UserStatus newStatus)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "status", newStatus.ToString() }
                });

                Debug.Log($"Updated status for user {userId} to {newStatus}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update status for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task AddFriend(string userId, string friendName)
        {
            try
            {
                var userDoc = db.Collection("users").Document(userId);
                var userSnapshot = await userDoc.GetSnapshotAsync();
                
                if (!userSnapshot.Exists)
                {
                    Debug.LogError($"User {userId} not found when trying to add friend");
                    return;
                }
                
                var userData = userSnapshot.ToDictionary();
                List<string> currentFriends = new List<string>();
                
                // Get current friends list
                if (userData.ContainsKey("friends") && userData["friends"] != null)
                {
                    var friendsData = userData["friends"];
                    if (friendsData is List<object> friendsList)
                    {
                        foreach (var friend in friendsList)
                        {
                            if (friend != null)
                            {
                                currentFriends.Add(friend.ToString());
                            }
                        }
                    }
                }
                
                // Add new friend if not already in list
                if (!currentFriends.Contains(friendName))
                {
                    currentFriends.Add(friendName);
                    
                    await userDoc.UpdateAsync(new Dictionary<string, object>
                    {
                        { "friends", currentFriends }
                    });
                    
                    // // Update cache if exists
                    // if (PlayerPrefs.HasKey("current_user_profile"))
                    // {
                    //     var json = PlayerPrefs.GetString("current_user_profile");
                    //     var cachedUser = JsonUtility.FromJson<User>(json);
                    //     if (cachedUser != null && cachedUser.userId == userId)
                    //     {
                    //         cachedUser.friends = currentFriends;
                    //         PlayerPrefs.SetString("current_user_profile", JsonUtility.ToJson(cachedUser));
                    //         PlayerPrefs.Save();
                    //     }
                    // }
                    
                }
                else
                {
                    // User already has this friend
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add friend for user {userId}: {ex.Message}");
            }
        }

        public int CalculateUserRank(int coins, int knowledgePoints)
        {

      double weightedScore = (0.7 * knowledgePoints) + (0.3 * coins);
    
    if (weightedScore >= 1600)
    {
        return 3;
    }
    else if (weightedScore >= 750)
    {
        return 2;
    }
    else
    {
        return 1;
    }
            // try
            // {
            //     // Get both knowledge points and coins
            //     int knowledgePoints = await GetUserKnowledgePoints(userId);
            //     int coins = await GetUserCoins(userId);

            //     // Calculate the combined total (sophisticated algorithm that considers both types of points)
            //     int totalPoints = knowledgePoints + (coins / 2); // Coins count as half the value of knowledge points

            //     Debug.Log($"Rank calculation - KB: {knowledgePoints}, Coins: {coins}, Total: {totalPoints}");

            //     // Calculate rank based on the combined total, keeping same thresholds
            //     if (totalPoints >= 2000)
            //     {
            //         return 3; // Rank 3: 2000+ total points
            //     }
            //     else if (totalPoints >= 1000)
            //     {
            //         return 2; // Rank 2: 1000-1999 total points
            //     }
            //     else
            //     {
            //         return 1; // Rank 1: 0-999 total points
            //     }
            // }
            // catch (Exception ex)
            // {
            //     Debug.LogError($"Failed to calculate rank for user {userId}: {ex.Message}");
            //     return 1; // Default to rank 1 if an error occurs
            // }
        }

        public async Task ClearCurrentUserBuilding()
        {
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("No authenticated user found, cannot clear current building");
                return;
            }

            try
            {
                var userDoc = db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "currentBuilding", null }
                });

                // // Update the cached user profile
                // var user = await GetUserProfileCachedOrRemoteAsync(userId);
                // if (user != null)
                // {
                //     user.currentBuilding = null;
                //     string json = JsonUtility.ToJson(user);
                //     PlayerPrefs.SetString("current_user_profile", json);
                //     PlayerPrefs.Save();
                //     Debug.Log($"Updated current building for user {userId} to null");
                // }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update current building for user {userId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clears all user-related cached data from PlayerPrefs upon logout
        /// </summary>
        public static void ClearUserCache()
        {
            try
            {
                // Get current user ID before clearing cache to clear user-specific flags
                string currentUserId = null;
                try
                {
                    currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"UserService: Could not get current user ID for cache clearing: {ex.Message}");
                }

                // Clear user profile cache
                if (PlayerPrefs.HasKey("current_user_profile"))
                {
                    PlayerPrefs.DeleteKey("current_user_profile");
                }

                // Clear user info cache
                if (PlayerPrefs.HasKey("UserName"))
                {
                    PlayerPrefs.DeleteKey("UserName");
                }

                if (PlayerPrefs.HasKey("ResidenceHall"))
                {
                    PlayerPrefs.DeleteKey("ResidenceHall");
                }

                if (PlayerPrefs.HasKey("PlayerName"))
                {
                    PlayerPrefs.DeleteKey("PlayerName");
                }

                // Clear user rank cache
                if (PlayerPrefs.HasKey("UserRank"))
                {
                    PlayerPrefs.DeleteKey("UserRank");
                }

                // Clear first-time user flag for the current user
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    string firstTimeKey = $"FirstTime_{currentUserId}";
                    if (PlayerPrefs.HasKey(firstTimeKey))
                    {
                        PlayerPrefs.DeleteKey(firstTimeKey);
                    }
                }

                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"UserService: Failed to clear user cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the currently equipped skin for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The equipped skin enum value</returns>
        public async Task<EquippedSkin> GetEquippedSkin(string userId)
        {
            try
            {
                DocumentSnapshot doc = await db.Collection("users").Document(userId).GetSnapshotAsync();
                if (doc.Exists && doc.TryGetValue("equippedSkin", out object skinValue))
                {
                    if (skinValue is string skinString)
                    {
                        if (Enum.TryParse<EquippedSkin>(skinString, out EquippedSkin skin))
                        {
                            return skin;
                        }
                    }
                    else if (skinValue is long skinNumber)
                    {
                        return (EquippedSkin)skinNumber;
                    }
                }
                
                // Default to Default skin if not found or invalid
                return EquippedSkin.Default;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UserService: Failed to get equipped skin for user {userId}: {ex.Message}");
                return EquippedSkin.Default;
            }
        }

        /// <summary>
        /// Gets the currently equipped accessory for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The equipped accessory, or None if not found</returns>
        public async Task<EquippedAccessory> GetEquippedAccessory(string userId)
        {
            try
            {
                DocumentSnapshot doc = await db.Collection("users").Document(userId).GetSnapshotAsync();
                if (doc.Exists && doc.TryGetValue("equippedAccessory", out object accessoryValue))
                {
                    if (accessoryValue is string accessoryString)
                    {
                        if (Enum.TryParse<EquippedAccessory>(accessoryString, out EquippedAccessory accessory))
                        {
                            return accessory;
                        }
                    }
                    else if (accessoryValue is long accessoryNumber)
                    {
                        return (EquippedAccessory)accessoryNumber;
                    }
                }
                
                // Default to None if not found or invalid
                return EquippedAccessory.None;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UserService: Failed to get equipped accessory for user {userId}: {ex.Message}");
                return EquippedAccessory.None;
            }
        }

        /// <summary>
        /// Updates the equipped skin for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="newSkin">The new skin to equip</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateEquippedSkin(string userId, EquippedSkin newSkin)
        {
            try
            {
                var updateData = new Dictionary<string, object>
                {
                    { "equippedSkin", newSkin.ToString() }
                };

                await db.Collection("users").Document(userId).UpdateAsync(updateData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UserService: Failed to update equipped skin for user {userId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates the user's equipped accessory in Firebase
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="newAccessory">The new accessory to equip</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateEquippedAccessory(string userId, EquippedAccessory newAccessory)
        {
            try
            {
                var updateData = new Dictionary<string, object>
                {
                    { "equippedAccessory", newAccessory.ToString() }
                };

                await db.Collection("users").Document(userId).UpdateAsync(updateData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UserService: Failed to update equipped accessory for user {userId}: {ex.Message}");
                return false;
            }
        }

    }
}
