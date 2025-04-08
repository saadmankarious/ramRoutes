using UnityEngine;

[System.Serializable]
public class GameTrial
{
    [Header("Trial Info")]
    public string trialName;
    public int trialNumber;
    public float timeLimit; // In seconds
    
    [Header("Objectives")]
    public int targetCoins;
    public int targetTrees;
    public int targetTrash;
    public int targetRecycling;
    
    [Header("Current Progress")]
    [SerializeField] private int currentCoins;
    [SerializeField] private int currentTrees;
    [SerializeField] private int currentTrash;
    [SerializeField] private int currentRecycling;
    
    // Completion state
    private bool isCompleted;
    
    // Events
    public System.Action OnTrialComplete;
    public System.Action OnObjectiveProgress;

    public void Initialize()
    {
        currentCoins = 0;
        currentTrees = 0;
        currentTrash = 0;
        currentRecycling = 0;
        isCompleted = false;
    }

    #region Progress Tracking
    public void AddCoins(int amount)
    {
        if (isCompleted) return;
        currentCoins = Mathf.Min(currentCoins + amount, targetCoins);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddTrees(int amount)
    {
        if (isCompleted) return;
        currentTrees = Mathf.Min(currentTrees + amount, targetTrees);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddTrash(int amount)
    {
        if (isCompleted) return;
        currentTrash = Mathf.Min(currentTrash + amount, targetTrash);
        CheckCompletion();
        OnObjectiveProgress?.Invoke();
    }

    public void AddRecycling(int amount)
    {
        if (isCompleted) return;
        currentRecycling = Mathf.Min(currentRecycling + amount, targetRecycling);
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
        return currentCoins >= targetCoins &&
               currentTrees >= targetTrees &&
               currentTrash >= targetTrash &&
               currentRecycling >= targetRecycling;
    }
    #endregion

    #region Progress Reporting
    public float GetOverallProgress()
    {
        float totalPossible = targetCoins + targetTrees + targetTrash + targetRecycling;
        float currentTotal = currentCoins + currentTrees + currentTrash + currentRecycling;
        return totalPossible > 0 ? currentTotal / totalPossible : 0;
    }

    public string GetProgressReport()
    {
        return $"{trialName} Progress:\n" +
               $"- Coins: {currentCoins}/{targetCoins}\n" +
               $"- Trees: {currentTrees}/{targetTrees}\n" +
               $"- Trash: {currentTrash}/{targetTrash}\n" +
               $"- Recycling: {currentRecycling}/{targetRecycling}";
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

    public float GetTimeProgress(float currentTime)
    {
        return Mathf.Clamp01(currentTime / timeLimit);
    }
    #endregion
}