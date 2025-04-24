using UnityEngine;
using Firebase;
using Firebase.Firestore;
using System.Threading.Tasks;

[FirestoreData]
public class GamePlay
{
    [FirestoreProperty] public string PlayerName { get; set; }
    [FirestoreProperty] public int CoinsCollected { get; set; }
    [FirestoreProperty] public Timestamp DateCompleted { get; set; }
    [FirestoreProperty] public int TrialNumber { get; set; }
}
[FirestoreData]
public class GameAttempt
{
    [FirestoreProperty] public string PlayerName { get; set; }
    [FirestoreProperty] public Timestamp Date { get; set; }
}


public static class FirestoreUtility
{
    private static FirebaseFirestore db;

    public static async Task Initialize()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            db = FirebaseFirestore.DefaultInstance;
            Debug.Log("Firebase initialized successfully");
        }
        else
        {
            Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
        }
    }

    public static async Task<bool> TestConnection()
    {
        try
        {
            var testRef = db.Collection("connectionTest").Document("ping");
            await testRef.SetAsync(new { timestamp = FieldValue.ServerTimestamp });
            await testRef.DeleteAsync();
            Debug.Log("Firestore connection test successful");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Connection test failed: {e.Message}");
            return false;
        }
    }

public static async Task SaveGameAttempt(string playerName)
{
    try 
    {
        var gameplay = new GameAttempt { PlayerName = playerName, Date = Timestamp.GetCurrentTimestamp() };
        await db.Collection("game-attempts").AddAsync(gameplay);
    }
    catch (System.Exception ex) { Debug.LogError($"Failed to save attempt: {ex.Message}"); }
}

public static async Task SaveTrialCompletion(string playerName, int coinsCollected, int highestLevelReached)
{
    try 
    {
        var gameplay = new GamePlay { PlayerName = playerName, CoinsCollected = coinsCollected, 
                                   DateCompleted = Timestamp.GetCurrentTimestamp(), TrialNumber = highestLevelReached };
        await db.Collection("game-completions").AddAsync(gameplay);
    }
    catch (System.Exception ex) { Debug.LogError($"Failed to save completion: {ex.Message}"); }
}
}

