using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public class ShoutOutService
    {
        private FirebaseFirestore db;
        private UserService userService;

        public ShoutOutService()
        {
            db = FirebaseFirestore.DefaultInstance;
            userService = new UserService();
        }

        public async Task<bool> SendShoutOut(string toId)
        {
            string fromId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                return false;
            }

            try
            {
                // Get current user to check if they have enough resources
                var fromUser = await userService.GetUserProfileCachedOrRemoteAsync(fromId);
                if (fromUser == null || fromUser.knowledgePoints < 10 || fromUser.coins < 10)
                {
                    return false;
                }

                // Create shout out record
                var shoutOut = new ShoutOut(fromId, toId);
                var docData = new Dictionary<string, object>
                {
                    { "fromId", shoutOut.fromId },
                    { "toId", shoutOut.toId },
                    { "timestamp", shoutOut.timestamp.ToString("o") },
                    { "kbAmount", shoutOut.kbAmount },
                    { "coinAmount", shoutOut.coinAmount }
                };

                // Save shout out record
                await db.Collection("shout-outs").AddAsync(docData);

                // Update sender: subtract 10 kb and 10 coins
                await userService.UpdateUserCoins(fromId, -10);

                // Update receiver: add 10 kb and 10 coins
                await userService.UpdateUserCoins(toId, 10);


       await userService.UpdateUserKnowledgePoints(fromId, -10);

                // Update receiver: add 10 kb and 10 coins
                await userService.UpdateUserKnowledgePoints(toId, 10);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send shout out: {ex.Message}");
                return false;
            }
        }
    }
}
