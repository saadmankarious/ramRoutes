using Firebase;
using Firebase.Auth;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseAuth auth;
    public static FirebaseUser user;

    void Start()
    {
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase Initialized");
            }
            else
            {
                Debug.LogError($"Firebase dependency error: {task.Result}");
            }
        });
    }
}
