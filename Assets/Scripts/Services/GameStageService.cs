using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public static class GameStageService
    {
        private const string PrefsKey = "game_stage_area";
        private const string PrefsKeyDisplay = "game_stage_display";

        // Save to PlayerPrefs only
        public static void SaveStageToPrefs(GameStage stage)
        {
            if (stage == null)
            {
                Debug.LogWarning("GameStageService.SaveStageToPrefs: stage is null");
                return;
            }
            PlayerPrefs.SetInt(PrefsKey, (int)stage.area);
            PlayerPrefs.SetString(PrefsKeyDisplay, stage.stageDisplayName ?? GameStage.GetDefaultDisplayName(stage.area));
            PlayerPrefs.Save();
        }

        // Clear only the stage-related PlayerPrefs keys
        public static void ClearStageFromPrefs()
        {
            if (PlayerPrefs.HasKey(PrefsKey)) PlayerPrefs.DeleteKey(PrefsKey);
            if (PlayerPrefs.HasKey(PrefsKeyDisplay)) PlayerPrefs.DeleteKey(PrefsKeyDisplay);
            PlayerPrefs.Save();
            Debug.Log("GameStageService: cleared stage from PlayerPrefs");
        }

        // Read from PlayerPrefs; returns null if not set
        public static GameStage LoadStageFromPrefs()
        {
            if (!PlayerPrefs.HasKey(PrefsKey)) return null;
            var area = (Stage)PlayerPrefs.GetInt(PrefsKey, 0);
            var display = PlayerPrefs.GetString(PrefsKeyDisplay, GameStage.GetDefaultDisplayName(area));
            return new GameStage(area, display);
        }

        // Save to Firestore for current user
        public static async Task SaveStageToFirestore(GameStage stage)
        {
            if (stage == null)
            {
                Debug.LogWarning("GameStageService.SaveStageToFirestore: stage is null");
                return;
            }

            string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("GameStageService.SaveStageToFirestore: no logged-in user");
                return;
            }

            var doc = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
            var data = new
            {
                gameStage = new
                {
                    area = stage.area.ToString(),
                    areaIndex = (int)stage.area,
                    displayName = stage.stageDisplayName ?? GameStage.GetDefaultDisplayName(stage.area)
                },
                gameStageUpdatedAt = FieldValue.ServerTimestamp
            };
            await doc.SetAsync(data, SetOptions.MergeAll);
            Debug.Log("GameStageService: saved stage to Firestore");
        }

        // Read from Firestore for current user
        public static async Task<GameStage> LoadStageFromFirestore()
        {
            string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("GameStageService.LoadStageFromFirestore: no logged-in user");
                return null;
            }

            var doc = await FirebaseFirestore.DefaultInstance.Collection("users").Document(userId).GetSnapshotAsync();
            if (!doc.Exists) return null;

            if (doc.TryGetValue("gameStage", out object raw) && raw is System.Collections.Generic.Dictionary<string, object> map)
            {
                Stage area = Stage.EasternCampus;
                string display = null;

                if (map.TryGetValue("areaIndex", out var idxObj) && idxObj is long lidx)
                {
                    area = (Stage)(int)lidx;
                }
                else if (map.TryGetValue("area", out var areaObj) && areaObj is string areaStr)
                {
                    // Fallback parse by name
                    System.Enum.TryParse(areaStr, true, out area);
                }

                if (map.TryGetValue("displayName", out var dispObj) && dispObj is string dispStr)
                {
                    display = dispStr;
                }
                else
                {
                    display = GameStage.GetDefaultDisplayName(area);
                }

                return new GameStage(area, display);
            }

            return null;
        }

        // Convenience: set current stage everywhere (prefs + Firestore)
        public static async Task SetStage(GameStage stage)
        {
            SaveStageToPrefs(stage);
            await SaveStageToFirestore(stage);
        }

        public static void ClearGameStageCache()
        {
             PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.DeleteKey(PrefsKeyDisplay);
            PlayerPrefs.Save();
            Debug.Log("GameStageService: Cleared game stage cache");
        }
    }
}
