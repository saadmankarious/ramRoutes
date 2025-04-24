using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LoginManager : MonoBehaviour
{
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;
    public GameObject loginPanel;
    public GameObject welcomePanel;
    public Text welcomeText;

    private bool isLoggedIn = false;
    private string userEmail = "";

    public async void Play()
    {
        string playerName = emailInput.text;
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        try
        {
            await FirestoreUtility.SaveGameAttempt(playerName);
            SceneManager.LoadScene("LevelFall");
        }
        catch
        {
            SceneManager.LoadScene("LevelFall");
        }
    }

    public void LogoutUser()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        loginPanel.SetActive(true);
        welcomePanel.SetActive(false);
        emailInput.text = "";
        passwordInput.text = "";
        statusText.text = "User logged out.";
    }

    private void Update()
    {
        if (isLoggedIn)
        {
            loginPanel.SetActive(false);
            welcomePanel.SetActive(true);
            welcomeText.text = $"Welcome, {userEmail}!";
            isLoggedIn = false;
        }
    }

    public void ViewOnboarding()
    {
        SceneManager.LoadScene("Onboarding");
    }
}