using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;
    public GameObject loginPanel;  // Panel with login fields + button
    public GameObject welcomePanel; // Panel to show after login
    public Text welcomeText;

    public void LoginUser()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("Login canceled.");
                statusText.text = "Login canceled.";
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"Login error: {task.Exception}");
                statusText.text = $"Login failed: {task.Exception.Message}";
                return;
            }

            // Extract the FirebaseUser from the AuthResult
            FirebaseUser newUser = task.Result.User;
            Debug.Log($"User logged in: {newUser.Email}");
            // After login success
            Debug.Log("Switching panels...");
            loginPanel.SetActive(false);
            welcomePanel.SetActive(true);


            // Set welcome text
            welcomeText.text = $"Welcome, {newUser.Email}!";
        });
    }
}
