using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public class FootprintService
    {
        private const string Collection = "footprints";
        private FirebaseFirestore db;

        public FootprintService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        public async Task<string> CreateAsync(Footprint footprint)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "text",       footprint.text },
                    { "makerId",    footprint.makerId },
                    { "buildingId", footprint.buildingId }
                };
                var docRef = await db.Collection(Collection).AddAsync(data);
                return docRef.Id;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FootprintService] Create failed: {ex.Message}");
                return null;
            }
        }

        public async Task<Footprint> GetAsync(string id)
        {
            try
            {
                var doc = await db.Collection(Collection).Document(id).GetSnapshotAsync();
                if (!doc.Exists) return null;
                return FromDoc(doc);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FootprintService] Get failed: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Footprint>> GetByMakerAsync(string makerId)
        {
            try
            {
                var query = await db.Collection(Collection)
                    .WhereEqualTo("makerId", makerId)
                    .GetSnapshotAsync();

                var results = new List<Footprint>();
                foreach (var doc in query.Documents)
                    results.Add(FromDoc(doc));
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FootprintService] GetByMaker failed: {ex.Message}");
                return new List<Footprint>();
            }
        }

        public async Task<List<Footprint>> GetByBuildingAsync(string buildingId)
        {
            try
            {
                var query = await db.Collection(Collection)
                    .WhereEqualTo("buildingId", buildingId)
                    .GetSnapshotAsync();

                var results = new List<Footprint>();
                foreach (var doc in query.Documents)
                    results.Add(FromDoc(doc));
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FootprintService] GetByBuilding failed: {ex.Message}");
                return new List<Footprint>();
            }
        }

        public async Task UpdateAsync(string id, string newText)
        {
            try
            {
                await db.Collection(Collection).Document(id).UpdateAsync(
                    new Dictionary<string, object> { { "text", newText } });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FootprintService] Update failed: {ex.Message}");
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await db.Collection(Collection).Document(id).DeleteAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FootprintService] Delete failed: {ex.Message}");
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static Footprint FromDoc(DocumentSnapshot doc)
        {
            var d = doc.ToDictionary();
            return new Footprint
            {
                id         = doc.Id,
                text       = d.ContainsKey("text")       ? d["text"].ToString()       : "",
                makerId    = d.ContainsKey("makerId")    ? d["makerId"].ToString()    : "",
                buildingId = d.ContainsKey("buildingId") ? d["buildingId"].ToString() : ""
            };
        }
    }
}
