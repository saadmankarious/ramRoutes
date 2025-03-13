using UnityEngine;
using UnityEngine.UI;
using Platformer.Gameplay; // For PlayerTokenCollision

public class CoinCounter : MonoBehaviour
{
    private int coinCount = 0; // Number of coins collected

    void OnEnable()
    {
        // Subscribe to PlayerTokenCollision's event
        PlayerTokenCollision.OnEvent += OnPlayerTokenCollision;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        PlayerTokenCollision.OnEvent -= OnPlayerTokenCollision;
    }

    void OnPlayerTokenCollision(PlayerTokenCollision evt)
    {
        // Increment the coin count
        coinCount++;

        // Update the text on the UI to reflect the coin count
        GameManager.Instance.AddCoins(1);

        Debug.Log("Coins Collected: " + coinCount);
    }
}
