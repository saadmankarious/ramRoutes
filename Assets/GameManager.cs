using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int coinsCollected;
    public int trashCollected;
    public int bottlesCollected;
    public int treesPlanted;

    public int gameLevel; // 1 = Level 1, 2 = Level 2, etc.

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
    }

    public void AddBottles(int amount)
    {
        bottlesCollected += amount;
        Debug.Log("Bottles Collected: " + bottlesCollected);
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
}
