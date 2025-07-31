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

        public async Task UpdateUser(User user)
        {
            var docData = new Dictionary<string, object>
            {
                { "userId", user.userId },
                { "notificationToken", user.notificationToken },
                { "name", user.name },
                { "email", user.email }
            };
            try
            {
                await db.Collection("users").Document(user.userId).SetAsync(docData);
                Debug.Log($"User {user.userId} updated in Firestore");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update user {user.userId}: {ex.Message}");
            }
        }

        public async Task<User> RetrieveUserById(string userId)
        {
            try
            {
                DocumentSnapshot doc = await db.Collection("users").Document(userId).GetSnapshotAsync();
                if (doc.Exists)
                {
                    var data = doc.ToDictionary();
                    string id = data.ContainsKey("userId") ? data["userId"].ToString() : "";
                    string token = data.ContainsKey("notificationToken") ? data["notificationToken"].ToString() : "";
                    string name = data.ContainsKey("name") ? data["name"].ToString() : "";
                    string email = data.ContainsKey("email") ? data["email"].ToString() : "";
                    Debug.Log($"User {id} retrieved from Firestore");
                    return new User(id, token, name, email);
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
            if (PlayerPrefs.HasKey("current_user_profile"))
            {
                string json = PlayerPrefs.GetString("current_user_profile");
                try
                {
                    User cachedUser = JsonUtility.FromJson<User>(json);
                    if (cachedUser != null && !string.IsNullOrEmpty(cachedUser.userId))
                    {
                        Debug.Log($"User profile loaded from cache: {cachedUser.userId}, {cachedUser.name}");
                        return cachedUser;
                    }
                    else
                    {
                        Debug.LogWarning("Cached user profile is malformed or missing userId.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse cached user profile: {ex.Message}");
                }
            }
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
    }
}
