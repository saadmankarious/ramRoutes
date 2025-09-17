using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using RamRoutes.Model;
using UnityEngine;

namespace RamRoutes.Services
{
    public class ChatService
    {
        private readonly FirebaseFirestore db;
        private const string COLLECTION_NAME = "chat";
        
        public ChatService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }
        
        /// <summary>
        /// Send a chat message with emojis
        /// </summary>
        public async Task<bool> SendChatAsync(string fromId, string toId, string emojis)
        {
            try
            {
                var chat = new Chat(fromId, toId, emojis);
                var chatData = new Dictionary<string, object>
                {
                    { "fromId", chat.fromId },
                    { "toId", chat.toId },
                    { "chatEmojies", chat.chatEmojies },
                    { "timestamp", chat.timestamp }
                };
                
                await db.Collection(COLLECTION_NAME).AddAsync(chatData);
                Debug.Log($"Chat sent from {fromId} to {toId}: {emojis}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send chat: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get chat messages for a specific user (received messages)
        /// Requires composite index: toId (Ascending), timestamp (Descending)
        /// </summary>
        public async Task<List<Chat>> GetChatsForUserAsync(string userId, int limit = 20)
        {
            try
            {
                var query = db.Collection(COLLECTION_NAME)
                    .WhereEqualTo("toId", userId)
                    .OrderByDescending("timestamp")
                    .Limit(limit);
                
                var snapshot = await query.GetSnapshotAsync();
                var chats = new List<Chat>();
                
                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var chat = new Chat
                        {
                            fromId = data.ContainsKey("fromId") ? data["fromId"].ToString() : "",
                            toId = data.ContainsKey("toId") ? data["toId"].ToString() : "",
                            chatEmojies = data.ContainsKey("chatEmojies") ? data["chatEmojies"].ToString() : "",
                            timestamp = data.ContainsKey("timestamp") ? ((Timestamp)data["timestamp"]).ToDateTime() : DateTime.UtcNow
                        };
                        chats.Add(chat);
                    }
                }
                
                return chats;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get chats for user {userId}: {ex.Message}");
                return new List<Chat>();
            }
        }
        
        /// <summary>
        /// Get chat conversation between two users
        /// Requires composite indexes:
        /// 1. fromId (Ascending), toId (Ascending), timestamp (Ascending)
        /// 2. fromId (Ascending), toId (Ascending), timestamp (Ascending) 
        /// </summary>
        public async Task<List<Chat>> GetConversationAsync(string userId1, string userId2, int limit = 50)
        {
            try
            {
                // Get messages from user1 to user2 with server-side ordering
                var query1 = db.Collection(COLLECTION_NAME)
                    .WhereEqualTo("fromId", userId1)
                    .WhereEqualTo("toId", userId2)
                    .OrderBy("timestamp")
                    .Limit(limit);
                    
                // Get messages from user2 to user1 with server-side ordering
                var query2 = db.Collection(COLLECTION_NAME)
                    .WhereEqualTo("fromId", userId2)
                    .WhereEqualTo("toId", userId1)
                    .OrderBy("timestamp")
                    .Limit(limit);
                
                // Execute both queries
                var snapshot1 = await query1.GetSnapshotAsync();
                var snapshot2 = await query2.GetSnapshotAsync();
                
                var chats = new List<Chat>();
                
                // Process first query results
                foreach (var document in snapshot1.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var chat = new Chat
                        {
                            fromId = data.ContainsKey("fromId") ? data["fromId"].ToString() : "",
                            toId = data.ContainsKey("toId") ? data["toId"].ToString() : "",
                            chatEmojies = data.ContainsKey("chatEmojies") ? data["chatEmojies"].ToString() : "",
                            timestamp = data.ContainsKey("timestamp") ? ((Timestamp)data["timestamp"]).ToDateTime() : DateTime.UtcNow
                        };
                        chats.Add(chat);
                    }
                }
                
                // Process second query results
                foreach (var document in snapshot2.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var chat = new Chat
                        {
                            fromId = data.ContainsKey("fromId") ? data["fromId"].ToString() : "",
                            toId = data.ContainsKey("toId") ? data["toId"].ToString() : "",
                            chatEmojies = data.ContainsKey("chatEmojies") ? data["chatEmojies"].ToString() : "",
                            timestamp = data.ContainsKey("timestamp") ? ((Timestamp)data["timestamp"]).ToDateTime() : DateTime.UtcNow
                        };
                        chats.Add(chat);
                    }
                }
                
                // Sort all messages by timestamp and apply final limit
                // Server-side ordering helps but we still need to merge and sort the two result sets
                chats = chats.OrderBy(c => c.timestamp).Take(limit).ToList();
                
                return chats;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get conversation between {userId1} and {userId2}: {ex.Message}");
                return new List<Chat>();
            }
        }
    }
}
