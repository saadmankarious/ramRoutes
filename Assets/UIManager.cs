using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text coinsText;
    public Text trashText;
    public Text bottlesText;
    public Text treesText;
    public Text levelText;

    private string GetTrialName(int level)
    {
        switch (level)
        {
            case 0:
                return "Trial 1: Sorting Trash";
            case 1:
                return "Trial 2: Tree Planting";
            case 2:
                return "Trial 3: xxx";
            case 3:
                return "Trial 4: yyyy";
            default:
                return "Sorting Trash";
        }
    }

    private void Update()
    {
        // Update the UI with the latest values from GameManager
        coinsText.text = "" + GameManager.Instance.coinsCollected;
        trashText.text = "" + GameManager.Instance.trashCollected;
        bottlesText.text = "" + GameManager.Instance.bottlesCollected;
        treesText.text = "" + GameManager.Instance.treesPlanted;

        // Display both level and trial name
        levelText.text = "Level: " + GameManager.Instance.gameLevel + " - " + GetTrialName(GameManager.Instance.gameLevel);
    }
}
