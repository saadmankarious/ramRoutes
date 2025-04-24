using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameTrial currentTrial;
    public List<GameTrial> allTrials = new List<GameTrial>();
    public int gameLevel = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeTrials();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTrials()
    {
        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 1: Sorting Trash",
            trialObjective = "CEOs are trashing our campus. Recycle 10 trash and 10 recycle items. Step right on their head to kill them but don't miss!",
            trialNumber = 1,
            timeLimit = 330f,
            targetTrash = 1,
            targetRecycling = 1
        });

        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 2: Tree Planting",
            trialObjective = "Reparation is needed after damage CEOs caused. Plant trees in place of CEOs. Tree saplings are hidden in buidlings.",
            trialNumber = 2,
            timeLimit = 360f,
            targetTreesPlanted = 4
        });
        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 3: Fill my Cup",
            trialObjective = "Water 4 trees you planted  using  Golden Eagle from Venus. ",
            trialNumber = 3,
            timeLimit = 240f,
            targetTreesWatered = 4
        });

        allTrials.Add(new GameTrial()
        {
            trialName = "Trial 4: Deliver the Magic Box",
            trialObjective = "Take the magic box from Venus to Pluto. Use spaceship.",
            trialNumber = 4,
            timeLimit = 240f,
        });


        LoadTrial(gameLevel);
    }

    public void LoadTrial(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < allTrials.Count)
        {
            currentTrial = allTrials[levelIndex];
            currentTrial.Initialize();
            currentTrial.OnTrialComplete += HandleTrialComplete;
        }
    }

    private void HandleTrialComplete()
    {
        if(currentTrial.trialNumber == 1)
        {
            RemoveObjectsWithTag("Trash");
            RemoveObjectsWithTag("Recyclable");
    
        }
        UIManager.Instance.OnTrialComplete.Invoke();
    }

void RemoveObjectsWithTag(string tag) {
    GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
    foreach (GameObject obj in objects) {
        Destroy(obj);
    }
}


    public void AddCoins(int amount)
    {
        currentTrial.AddCoins(amount);
    }

    public void AddTrash(int amount)
    {
        currentTrial.AddTrash(amount);
        Debug.Log($"Trash Collected: {currentTrial.currentTrash}/{currentTrial.targetTrash}");
    }

    public void AddBottles(int amount)
    {
        currentTrial.AddRecycling(amount);
        Debug.Log($"Recycling Collected: {currentTrial.currentRecycling}/{currentTrial.targetRecycling}");
    }

    public void PlantTree()
    {
        currentTrial.AddTrees(1);
        Debug.Log($"Trees Planted: {currentTrial.currentTreesPlanted}/{currentTrial.targetTreesPlanted}");
    }
    public void WaterTree()
    {
        currentTrial.WaterTrees(1);
        Debug.Log($"Trees watered: {currentTrial.currentTreesWatered}/{currentTrial.targetTreesWatered}");
    }

    public void SetGameLevel(int level)
    {
        gameLevel = level;
        LoadTrial(gameLevel);
        Debug.Log($"Loaded Trial: {currentTrial.trialName}");
    }

    public void ResetLevel()
    {
        currentTrial.Initialize();
        Debug.Log($"Reset Trial: {currentTrial.trialName}");
    }

    public void ResetTemporaryState()
    {
        currentTrial.Initialize();
        Debug.Log($"Reset Temporary State for: {currentTrial.trialName}");
    }
}