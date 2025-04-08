using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;
    public GameObject loginPanel;  // Panel with login fields + button
    public GameObject welcomePanel; // Panel to show after login
    public Text welcomeText;

    private bool isLoggedIn = false; // Flag to indicate login success
    private string userEmail = ""; // Store the user's email for the welcome message

    public void LoginUser()
    {
        // Debug.Log("Logging in user");

        // string email = emailInput.text;
        // string password = passwordInput.text;

        // FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
        //     if (task.IsCanceled)
        //     {
        //         Debug.LogError("Login canceled.");
        //         statusText.text = "Login canceled.";
        //         return;
        //     }
        //     if (task.IsFaulted)
        //     {
        //         Debug.LogError($"Login error, SAAD: {task.Exception}");
        //         statusText.text = $"Login failed: {task.Exception.Message}";
        //         return;
        //     }

        //     // Extract the FirebaseUser from the AuthResult
        //     FirebaseUser newUser = task.Result.User;
        //     Debug.Log($"User logged in: {newUser.Email}");

        //     // Store the user's email and set the flag for login success
        //     userEmail = newUser.Email;
        //     isLoggedIn = true;
        // });
        Play();
    }

    public void LogoutUser()
    {
        Debug.Log("Logging out user");

        // Sign out the user
        FirebaseAuth.DefaultInstance.SignOut();

        // Switch back to the login panel
        loginPanel.SetActive(true);
        welcomePanel.SetActive(false);

        // Clear the email and password fields
        emailInput.text = "";
        passwordInput.text = "";

        // Update status text
        statusText.text = "User logged out.";
    }

    private void Update()
    {
        // Check the flag in the main thread and update the UI
        if (isLoggedIn)
        {
            Debug.Log("Switching panels...");
            loginPanel.SetActive(false);
            welcomePanel.SetActive(true);
            welcomeText.text = $"Welcome, {userEmail}!";

            // Reset the flag
            isLoggedIn = false;
        }
    }

    public void Play()
    {
        SceneManager.LoadScene("LevelFall");
    }
}