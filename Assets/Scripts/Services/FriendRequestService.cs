using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public class FriendRequestService
    {
        private FirebaseFirestore db;
        private const string COLLECTION_NAME = "friend-requests";

        public FriendRequestService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        /// <summary>
        /// Send a friend request from the current user to another user
        /// </summary>
        /// <param name="toUserId">The user ID to send the request to</param>
        /// <returns>The created friend request</returns>
        public async Task<FriendRequest> SendFriendRequest(string toUserId)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("FriendRequestService.SendFriendRequest: No logged-in user");
                return null;
            }

            if (currentUserId == toUserId)
            {
                Debug.LogError("FriendRequestService.SendFriendRequest: Cannot send friend request to yourself");
                return null;
            }

            // Check if current user has already sent a request to this user
            var outgoingRequests = await GetOutgoingFriendRequests(currentUserId);
            var existingRequest = outgoingRequests.FirstOrDefault(r => r.toId == toUserId);
            
            if (existingRequest != null)
            {
                Debug.LogWarning($"FriendRequestService.SendFriendRequest: User {currentUserId} already sent a friend request to {toUserId}");
                return null; // Return null to indicate "already sent"
            }

            var friendRequest = new FriendRequest(currentUserId, toUserId);
            
            try
            {
                // Get user names for the request
                var userService = new UserService();
                var fromUser = await userService.GetUserProfileCachedOrRemoteAsync(currentUserId);
                var toUser = await userService.GetUserProfileCachedOrRemoteAsync(toUserId);
                
                string fromName = fromUser?.name ?? "Unknown";
                string toName = toUser?.name ?? "Unknown";
                
                // Set the names on the friend request object
                friendRequest.fromName = fromName;
                friendRequest.toName = toName;
                
                var docData = new Dictionary<string, object>
                {
                    { "fromId", friendRequest.fromId },
                    { "toId", friendRequest.toId },
                    { "timestamp", friendRequest.timestamp },
                    { "fromName", fromName },
                    { "toName", toName },
                    { "accepted", false }
                };
                
                var docRef = await db.Collection(COLLECTION_NAME).AddAsync(docData);
                
                // Update the document with its own ID
                await docRef.UpdateAsync("requestId", docRef.Id);
                
                // Set the request ID on our object
                friendRequest.requestId = docRef.Id;
                
                Debug.Log($"Friend request sent from {fromName} ({currentUserId}) to {toName} ({toUserId}) with ID {docRef.Id}");
                return friendRequest;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.SendFriendRequest: Error sending friend request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all incoming friend requests for a user
        /// </summary>
        /// <param name="userId">The user ID to get requests for (null for current user)</param>
        /// <returns>List of incoming friend requests</returns>
        public async Task<List<FriendRequest>> GetIncomingFriendRequests(string userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("FriendRequestService.GetIncomingFriendRequests: No user ID provided");
                return new List<FriendRequest>();
            }

            try
            {
                var query = db.Collection(COLLECTION_NAME).WhereEqualTo("toId", userId);
                var snapshot = await query.GetSnapshotAsync();

                var requests = new List<FriendRequest>();
                foreach (var doc in snapshot.Documents)
                {
                    var request = doc.ConvertTo<FriendRequest>();
                    request.requestId = doc.Id;
                    requests.Add(request);
                }

                // Sort by timestamp descending (newest first)
                return requests.OrderByDescending(r => r.GetTimestampAsDateTime()).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.GetIncomingFriendRequests: Error getting requests: {ex.Message}");
                return new List<FriendRequest>();
            }
        }

        /// <summary>
        /// Get all outgoing friend requests for a user
        /// </summary>
        /// <param name="userId">The user ID to get requests for (null for current user)</param>
        /// <returns>List of outgoing friend requests</returns>
        public async Task<List<FriendRequest>> GetOutgoingFriendRequests(string userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("FriendRequestService.GetOutgoingFriendRequests: No user ID provided");
                return new List<FriendRequest>();
            }

            try
            {
                var query = db.Collection(COLLECTION_NAME).WhereEqualTo("fromId", userId);
                var snapshot = await query.GetSnapshotAsync();

                var requests = new List<FriendRequest>();
                foreach (var doc in snapshot.Documents)
                {
                    var request = doc.ConvertTo<FriendRequest>();
                    request.requestId = doc.Id;
                    requests.Add(request);
                }

                // Sort by timestamp descending (newest first)
                return requests.OrderByDescending(r => r.GetTimestampAsDateTime()).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.GetOutgoingFriendRequests: Error getting requests: {ex.Message}");
                return new List<FriendRequest>();
            }
        }

        /// <summary>
        /// Get all friend requests for a user (both incoming and outgoing)
        /// </summary>
        /// <param name="userId">The user ID to get requests for (null for current user)</param>
        /// <returns>List of all friend requests</returns>
        public async Task<List<FriendRequest>> GetAllFriendRequests(string userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("FriendRequestService.GetAllFriendRequests: No user ID provided");
                return new List<FriendRequest>();
            }

            try
            {
                // Get both incoming and outgoing requests in parallel
                var incomingTask = GetIncomingFriendRequests(userId);
                var outgoingTask = GetOutgoingFriendRequests(userId);

                await Task.WhenAll(incomingTask, outgoingTask);

                var allRequests = new List<FriendRequest>();
                allRequests.AddRange(incomingTask.Result);
                allRequests.AddRange(outgoingTask.Result);

                // Sort by timestamp descending (newest first)
                return allRequests.OrderByDescending(r => r.GetTimestampAsDateTime()).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.GetAllFriendRequests: Error getting requests: {ex.Message}");
                return new List<FriendRequest>();
            }
        }

        /// <summary>
        /// Accept a friend request
        /// </summary>
        /// <param name="requestId">The ID of the request to accept</param>
        /// <returns>True if successful</returns>
        public async Task<bool> AcceptFriendRequest(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                Debug.LogError("FriendRequestService.AcceptFriendRequest: Request ID is required");
                return false;
            }

            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("FriendRequestService.AcceptFriendRequest: No logged-in user");
                return false;
            }

            try
            {
                // Get the friend request first to validate
                var doc = await db.Collection(COLLECTION_NAME).Document(requestId).GetSnapshotAsync();
                if (!doc.Exists)
                {
                    Debug.LogError("FriendRequestService.AcceptFriendRequest: Friend request not found");
                    return false;
                }

                var request = doc.ConvertTo<FriendRequest>();
                
                // Verify this request is meant for the current user
                if (request.toId != currentUserId)
                {
                    Debug.LogError("FriendRequestService.AcceptFriendRequest: Cannot accept request not meant for you");
                    return false;
                }

                // Add both users as friends in their user profiles
                var userService = new UserService();
                
                // Add sender's name to receiver's friends list
                string senderName = request.fromName ?? "Unknown";
                await userService.AddFriend(currentUserId, senderName);
                
                // Add receiver's name to sender's friends list
                string receiverName = request.toName ?? "Unknown";
                await userService.AddFriend(request.fromId, receiverName);

                // Delete the friend request after successful acceptance
                await DeleteFriendRequest(requestId);

                Debug.Log($"Friend request accepted between {request.fromId} and {request.toId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.AcceptFriendRequest: Error accepting request: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete/reject a friend request
        /// </summary>
        /// <param name="requestId">The ID of the request to delete</param>
        /// <returns>True if successful</returns>
        public async Task<bool> DeleteFriendRequest(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                Debug.LogError("FriendRequestService.DeleteFriendRequest: Request ID is required");
                return false;
            }

            try
            {
                await db.Collection(COLLECTION_NAME).Document(requestId).DeleteAsync();
                Debug.Log($"Friend request {requestId} deleted");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.DeleteFriendRequest: Error deleting request: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a friend request exists between two users (in either direction)
        /// </summary>
        /// <param name="userId1">First user ID</param>
        /// <param name="userId2">Second user ID</param>
        /// <returns>The existing friend request, or null if none exists</returns>
        public async Task<FriendRequest> GetFriendRequestBetweenUsers(string userId1, string userId2)
        {
            try
            {
                // Check for request from userId1 to userId2
                var query1 = db.Collection(COLLECTION_NAME)
                    .WhereEqualTo("fromId", userId1)
                    .WhereEqualTo("toId", userId2);
                var snapshot1 = await query1.GetSnapshotAsync();

                if (snapshot1.Documents.Any())
                {
                    var firstDoc = snapshot1.Documents.First();
                    var request = firstDoc.ConvertTo<FriendRequest>();
                    request.requestId = firstDoc.Id;
                    return request;
                }

                // Check for request from userId2 to userId1
                var query2 = db.Collection(COLLECTION_NAME)
                    .WhereEqualTo("fromId", userId2)
                    .WhereEqualTo("toId", userId1);
                var snapshot2 = await query2.GetSnapshotAsync();

                if (snapshot2.Documents.Any())
                {
                    var firstDoc = snapshot2.Documents.First();
                    var request = firstDoc.ConvertTo<FriendRequest>();
                    request.requestId = firstDoc.Id;
                    return request;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FriendRequestService.GetFriendRequestBetweenUsers: Error checking for existing request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the count of incoming friend requests for a user
        /// </summary>
        /// <param name="userId">The user ID to get count for (null for current user)</param>
        /// <returns>Number of incoming friend requests</returns>
        public async Task<int> GetIncomingRequestCount(string userId = null)
        {
            var requests = await GetIncomingFriendRequests(userId);
            return requests.Count;
        }
    }
}
