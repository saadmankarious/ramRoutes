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
    }
}
