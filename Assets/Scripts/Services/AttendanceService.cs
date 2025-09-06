using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Firebase.Firestore;
using UnityEngine;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public static class AttendanceService
    {
        private static FirebaseFirestore db;
        private const string COLLECTION_NAME = "attendance-records";

        // Initialize Firestore instance
        private static FirebaseFirestore DB
        {
            get
            {
                if (db == null)
                {
                    db = FirebaseFirestore.DefaultInstance;
                }
                return db;
            }
        }

        #region Create Operations

        /// <summary>
        /// Record a new attendance entry
        /// </summary>
        /// <param name="attendanceRecord">The attendance record to create</param>
        /// <returns>The created record with its Firestore document ID</returns>
        public static async Task<AttendanceRecord> RecordAttendanceAsync(AttendanceRecord attendanceRecord)
        {
            try
            {
                Debug.Log($"Recording attendance for {attendanceRecord.playerName} at {attendanceRecord.eventName}");

                // Add timestamp for when the record was created
                attendanceRecord.checkInDate = DateTime.Now;

                // Convert to Firestore data
                var data = attendanceRecord.ToFirestoreData();

                // Add to Firestore and get the document reference
                DocumentReference docRef = await DB.Collection(COLLECTION_NAME).AddAsync(data);
                
                // Update the record with the generated ID
                attendanceRecord.recordId = docRef.Id;

                Debug.Log($"Attendance recorded successfully with ID: {docRef.Id}");
                return attendanceRecord;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error recording attendance: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Record attendance using event and player details
        /// </summary>
        /// <param name="buildingEvent">The building event</param>
        /// <param name="playerName">Name of the player</param>
        /// <param name="playerId">ID of the player (optional)</param>
        /// <returns>The created attendance record</returns>
        public static async Task<AttendanceRecord> RecordAttendanceAsync(BuildingEvent buildingEvent, string playerName, string playerId = null)
        {
            var attendanceRecord = new AttendanceRecord(
                buildingEvent.eventId,
                buildingEvent.eventName,
                buildingEvent.buildingName,
                playerName,
                DateTime.Now,
                playerId,
                buildingEvent.buildingId
            );

            return await RecordAttendanceAsync(attendanceRecord);
        }

        #endregion

        #region Read Operations

        /// <summary>
        /// Get all attendance records
        /// </summary>
        /// <returns>List of all attendance records</returns>
        public static async Task<List<AttendanceRecord>> GetAllAttendanceRecordsAsync()
        {
            try
            {
                Debug.Log("Fetching all attendance records...");

                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .OrderBy("checkInDate")
                    .GetSnapshotAsync();

                var records = new List<AttendanceRecord>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var record = AttendanceRecord.FromFirestoreData(data, document.Id);
                        records.Add(record);
                    }
                }

                Debug.Log($"Fetched {records.Count} attendance records");
                return records;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching attendance records: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get attendance records for a specific event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <returns>List of attendance records for the event</returns>
        public static async Task<List<AttendanceRecord>> GetAttendanceByEventAsync(string eventId)
        {
            try
            {
                Debug.Log($"Fetching attendance records for event: {eventId}");

                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereEqualTo("eventId", eventId)
                    .OrderBy("checkInDate")
                    .GetSnapshotAsync();

                var records = new List<AttendanceRecord>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var record = AttendanceRecord.FromFirestoreData(data, document.Id);
                        records.Add(record);
                    }
                }

                Debug.Log($"Fetched {records.Count} attendance records for event {eventId}");
                return records;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching attendance records for event {eventId}: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get attendance records for a specific player
        /// </summary>
        /// <param name="playerId">The player ID</param>
        /// <returns>List of attendance records for the player</returns>
        public static async Task<List<AttendanceRecord>> GetAttendanceByPlayerAsync(string playerId)
        {
            try
            {
                Debug.Log($"Fetching attendance records for player: {playerId}");

                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereEqualTo("playerId", playerId)
                    .OrderBy("checkInDate")
                    .GetSnapshotAsync();

                var records = new List<AttendanceRecord>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var record = AttendanceRecord.FromFirestoreData(data, document.Id);
                        records.Add(record);
                    }
                }

                Debug.Log($"Fetched {records.Count} attendance records for player {playerId}");
                return records;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching attendance records for player {playerId}: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get attendance records for a specific building
        /// </summary>
        /// <param name="buildingName">The building name</param>
        /// <returns>List of attendance records for the building</returns>
        public static async Task<List<AttendanceRecord>> GetAttendanceByBuildingAsync(string buildingName)
        {
            try
            {
                Debug.Log($"Fetching attendance records for building: {buildingName}");

                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereEqualTo("buildingName", buildingName)
                    .OrderBy("checkInDate")
                    .GetSnapshotAsync();

                var records = new List<AttendanceRecord>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var record = AttendanceRecord.FromFirestoreData(data, document.Id);
                        records.Add(record);
                    }
                }

                Debug.Log($"Fetched {records.Count} attendance records for building {buildingName}");
                return records;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching attendance records for building {buildingName}: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get attendance records within a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of attendance records within the date range</returns>
        public static async Task<List<AttendanceRecord>> GetAttendanceByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                Debug.Log($"Fetching attendance records from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereGreaterThanOrEqualTo("checkInDate", Timestamp.FromDateTime(startDate))
                    .WhereLessThanOrEqualTo("checkInDate", Timestamp.FromDateTime(endDate))
                    .OrderBy("checkInDate")
                    .GetSnapshotAsync();

                var records = new List<AttendanceRecord>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var data = document.ToDictionary();
                        var record = AttendanceRecord.FromFirestoreData(data, document.Id);
                        records.Add(record);
                    }
                }

                Debug.Log($"Fetched {records.Count} attendance records for date range");
                return records;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching attendance records for date range: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get a specific attendance record by ID
        /// </summary>
        /// <param name="recordId">The record ID</param>
        /// <returns>The attendance record or null if not found</returns>
        public static async Task<AttendanceRecord> GetAttendanceRecordAsync(string recordId)
        {
            try
            {
                Debug.Log($"Fetching attendance record: {recordId}");

                DocumentSnapshot document = await DB.Collection(COLLECTION_NAME).Document(recordId).GetSnapshotAsync();

                if (document.Exists)
                {
                    var data = document.ToDictionary();
                    var record = AttendanceRecord.FromFirestoreData(data, document.Id);
                    Debug.Log($"Found attendance record: {record}");
                    return record;
                }
                else
                {
                    Debug.LogWarning($"Attendance record not found: {recordId}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching attendance record {recordId}: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Update an existing attendance record
        /// </summary>
        /// <param name="attendanceRecord">The updated attendance record</param>
        /// <returns>The updated record</returns>
        public static async Task<AttendanceRecord> UpdateAttendanceRecordAsync(AttendanceRecord attendanceRecord)
        {
            try
            {
                Debug.Log($"Updating attendance record: {attendanceRecord.recordId}");

                if (string.IsNullOrEmpty(attendanceRecord.recordId))
                {
                    throw new ArgumentException("Record ID is required for updates");
                }

                var data = attendanceRecord.ToFirestoreData();
                await DB.Collection(COLLECTION_NAME).Document(attendanceRecord.recordId).SetAsync(data);

                Debug.Log($"Attendance record updated successfully: {attendanceRecord.recordId}");
                return attendanceRecord;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating attendance record: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Delete Operations

        /// <summary>
        /// Delete an attendance record
        /// </summary>
        /// <param name="recordId">The record ID to delete</param>
        /// <returns>True if successful</returns>
        public static async Task<bool> DeleteAttendanceRecordAsync(string recordId)
        {
            try
            {
                Debug.Log($"Deleting attendance record: {recordId}");

                await DB.Collection(COLLECTION_NAME).Document(recordId).DeleteAsync();

                Debug.Log($"Attendance record deleted successfully: {recordId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting attendance record {recordId}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete all attendance records for a specific event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <returns>Number of records deleted</returns>
        public static async Task<int> DeleteAttendanceByEventAsync(string eventId)
        {
            try
            {
                Debug.Log($"Deleting attendance records for event: {eventId}");

                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereEqualTo("eventId", eventId)
                    .GetSnapshotAsync();

                int deletedCount = 0;
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    await document.Reference.DeleteAsync();
                    deletedCount++;
                }

                Debug.Log($"Deleted {deletedCount} attendance records for event {eventId}");
                return deletedCount;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting attendance records for event {eventId}: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if a player has already checked in to a specific event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="playerId">The player ID</param>
        /// <returns>True if player has already checked in</returns>
        public static async Task<bool> HasPlayerCheckedInAsync(string eventId, string playerId)
        {
            try
            {
                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereEqualTo("eventId", eventId)
                    .WhereEqualTo("playerId", playerId)
                    .Limit(1)
                    .GetSnapshotAsync();

                return snapshot.Documents.Count() > 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error checking player attendance: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get attendance count for a specific event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <returns>Number of attendees</returns>
        public static async Task<int> GetAttendanceCountAsync(string eventId)
        {
            try
            {
                QuerySnapshot snapshot = await DB.Collection(COLLECTION_NAME)
                    .WhereEqualTo("eventId", eventId)
                    .GetSnapshotAsync();

                return snapshot.Documents.Count();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting attendance count for event {eventId}: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get unique attendee count for a specific event (no duplicates)
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <returns>Number of unique attendees</returns>
        public static async Task<int> GetUniqueAttendeeCountAsync(string eventId)
        {
            try
            {
                var records = await GetAttendanceByEventAsync(eventId);
                var uniquePlayerIds = new HashSet<string>();
                
                foreach (var record in records)
                {
                    uniquePlayerIds.Add(record.playerId);
                }

                return uniquePlayerIds.Count;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting unique attendance count for event {eventId}: {e.Message}");
                return 0;
            }
        }

        #endregion
    }
}
