using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int coinsCollected;
    public int trashCollected;
    public int bottlesCollected;
    public int treesPlanted;

    public int gameLevel; // 1 = Level 1, 2 = Level 2, etc.
    private bool trialCompleted = false;

    private void CheckTrialCompletion()
    {
        if (trialCompleted) return;

        // Example completion condition
        if (trashCollected >= 1 && bottlesCollected >= 1)
        {
            trialCompleted = true;
            FindObjectOfType<UIManager>().OnTrialComplete.Invoke();
        }
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps GameManager across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Methods to update state
    public void AddCoins(int amount)
    {
        coinsCollected += amount;
        Debug.Log("Coins Collected: " + coinsCollected);
    }

    public void AddTrash(int amount)
    {
        trashCollected += amount;
        Debug.Log("Trash Collected: " + trashCollected);
        CheckTrialCompletion();

    }

    public void AddBottles(int amount)
    {
        bottlesCollected += amount;
        Debug.Log("Bottles Collected: " + bottlesCollected);
        CheckTrialCompletion();

    }

    public void PlantTree()
    {
        treesPlanted++;
        Debug.Log("Trees Planted: " + treesPlanted);
    }

    public void SetGameLevel(int level)
    {
        gameLevel = level;
        Debug.Log("Game Level: " + gameLevel);
    }

    // In GameManager.cs
public void ResetLevel()
{
    coinsCollected = 0;
    trashCollected = 0;
    bottlesCollected = 0;
    treesPlanted = 0;
    // Add any other reset logic needed for your game
}

public void ResetTemporaryState()
{
    // Reset only temporary variables while preserving permanent progress
    coinsCollected = 0;
    trashCollected = 0;
    bottlesCollected = 0;
    treesPlanted = 0;
    // Don't reset gameLevel if you want to maintain progression
}
}
