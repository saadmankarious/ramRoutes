using UnityEngine;
using System.Linq; // Add this at the top of your file

[System.Serializable]
public class GameTrial
{
    [Header("Trial Info")]
    public string trialName;
    public string trialObjective;
    public int trialNumber;
    public float timeLimit;
    
    [Header("Objectives")]
    public int targetCoins;
    public int targetTreesPlanted;
    public int targetTreesWatered;
    public int targetTrash;
    public int targetRecycling;
    
    [Header("Current Progress")]
    [SerializeField] private int _currentCoins;
    [SerializeField] private int _currentTreesPlanted;
    [SerializeField] private int _currentTreesWatered;
    [SerializeField] private int _currentTrash;
    [SerializeField] private int _currentRecycling;
    
    // Public properties for read access
    public int currentCoins => _currentCoins;
    public int currentTreesPlanted => _currentTreesPlanted;
    public int currentTreesWatered => _currentTreesWatered;
    public int currentTrash => _currentTrash;
    public int currentRecycling => _currentRecycling;
    public bool isCompleted { get; private set; }
    
    // Events
    public System.Action OnTrialComplete;
    public System.Action OnObjectiveProgress;

    public void Initialize()
    {
        _currentCoins = 0;
        _currentTreesPlanted = 0;
        _currentTreesWatered = 0;
        _currentTrash = 0;
        _currentRecycling = 0;
        isCompleted = false;
    }

    #region Progress Tracking
    public void AddCoins(int amount)
    {
        if (isCompleted) return;
        _currentCoins = _currentCoins+ amount;

        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddTrees(int amount)
    {
        if (isCompleted) return;
        _currentTreesPlanted = Mathf.Min(_currentTreesPlanted + amount, targetTreesPlanted);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }
    public void WaterTrees(int amount)
    {
        if (isCompleted) return;
        _currentTreesWatered = Mathf.Min(_currentTreesWatered + amount, targetTreesWatered);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddTrash(int amount)
    {
        if (isCompleted) return;
        _currentTrash = Mathf.Min(_currentTrash + amount, targetTrash);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddRecycling(int amount)
    {
        if (isCompleted) return;
        _currentRecycling = Mathf.Min(_currentRecycling + amount, targetRecycling);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }
    #endregion

    #region Completion Logic
    private void CheckCompletion()
    {
        if (!isCompleted && AllObjectivesMet())
        {
            isCompleted = true;
            OnTrialComplete?.Invoke();
        }
    }

    private bool AllObjectivesMet()
    {
        return _currentCoins >= targetCoins &&
               _currentTreesPlanted >= targetTreesPlanted &&
               _currentTreesWatered >= targetTreesWatered &&
               _currentTrash >= targetTrash &&
               _currentRecycling >= targetRecycling;
    }
    #endregion

    #region Progress Reporting
    public float GetOverallProgress()
    {
        float totalPossible = targetCoins + targetTreesPlanted + targetTrash + targetRecycling;
        float currentTotal = _currentCoins + _currentTreesPlanted + _currentTrash + _currentRecycling;
        return totalPossible > 0 ? currentTotal / totalPossible : 0;
    }
public string GetProgressReport() => string.Join("\n", 
    new[] {
        trialObjective + "..\n",
        // "Progress:",
        // targetCoins > 0 ? $"- Coins: {_currentCoins}/{targetCoins}" : null,
        // targetTreesPlanted > 0 ? $"- Trees: {_currentTreesPlanted}/{targetTreesPlanted}" : null,
        // targetTreesWatered > 0 ? $"- Trees Watered: {_currentTreesWatered}/{targetTreesWatered}" : null,
        // targetTrash > 0 ? $"- Trash: {_currentTrash}/{targetTrash}" : null,
        // targetRecycling > 0 ? $"- Recycling: {_currentRecycling}/{targetRecycling}" : null
    }.Where(line => line != null)
);
    #endregion

    #region Time Tracking
    public string GetFormattedTimeLeft(float currentTime)
    {
        float timeLeft = timeLimit - currentTime;
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
    #endregion
}