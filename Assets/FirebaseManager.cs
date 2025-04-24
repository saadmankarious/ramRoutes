using UnityEngine;
using Firebase;
using Firebase.Firestore;
using System.Threading.Tasks;

public class FirebaseManager : MonoBehaviour
{
    async void Start()
    {
        await FirestoreUtility.Initialize();
        bool connectionSuccess = await FirestoreUtility.TestConnection();

        if (connectionSuccess)
        {
            Debug.Log("connected successfully to firebase");
            // await FirestoreUtility.SaveGamePlay("Player1", 150, 5);
        }
    }
}