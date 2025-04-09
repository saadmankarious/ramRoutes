using UnityEngine;

[System.Serializable]
public class GameTrial
{
    [Header("Trial Info")]
    public string trialName;
    public int trialNumber;
    public float timeLimit;
    
    [Header("Objectives")]
    public int targetCoins;
    public int targetTrees;
    public int targetTrash;
    public int targetRecycling;
    
    [Header("Current Progress")]
    [SerializeField] private int _currentCoins;
    [SerializeField] private int _currentTrees;
    [SerializeField] private int _currentTrash;
    [SerializeField] private int _currentRecycling;
    
    // Public properties for read access
    public int currentCoins => _currentCoins;
    public int currentTrees => _currentTrees;
    public int currentTrash => _currentTrash;
    public int currentRecycling => _currentRecycling;
    public bool isCompleted { get; private set; }
    
    // Events
    public System.Action OnTrialComplete;
    public System.Action OnObjectiveProgress;

    public void Initialize()
    {
        _currentCoins = 0;
        _currentTrees = 0;
        _currentTrash = 0;
        _currentRecycling = 0;
        isCompleted = false;
    }

    #region Progress Tracking
    public void AddCoins(int amount)
    {
        if (isCompleted) return;
        _currentCoins = Mathf.Min(_currentCoins + amount, targetCoins);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddTrees(int amount)
    {
        if (isCompleted) return;
        _currentTrees = Mathf.Min(_currentTrees + amount, targetTrees);
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
               _currentTrees >= targetTrees &&
               _currentTrash >= targetTrash &&
               _currentRecycling >= targetRecycling;
    }
    #endregion

    #region Progress Reporting
    public float GetOverallProgress()
    {
        float totalPossible = targetCoins + targetTrees + targetTrash + targetRecycling;
        float currentTotal = _currentCoins + _currentTrees + _currentTrash + _currentRecycling;
        return totalPossible > 0 ? currentTotal / totalPossible : 0;
    }

    public string GetProgressReport()
    {
        return $"{trialName} Progress:\n" +
               $"- Coins: {_currentCoins}/{targetCoins}\n" +
               $"- Trees: {_currentTrees}/{targetTrees}\n" +
               $"- Trash: {_currentTrash}/{targetTrash}\n" +
               $"- Recycling: {_currentRecycling}/{targetRecycling}";
    }
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