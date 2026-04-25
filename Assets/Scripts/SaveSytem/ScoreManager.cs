using UnityEngine;
using UnityEngine.UI;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int currentScore = 0;
    public int highScore = 0;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(int amount, string source = "")
    {
        currentScore += amount;

        Debug.Log($"[+{amount}] Score! Total: {currentScore} ({source})");
    }

    public void SaveHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            Debug.Log($" New High Score: {highScore}");
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveHighScore(highScore);
        }
    }

    public void LoadHighScore()
    {
        if (SaveManager.Instance != null)
        {
            highScore = SaveManager.Instance.GetHighScore();
        }
    }


    // Call this when level ends or player dies
    public void OnLevelComplete()
    {
        SaveHighScore();
    }
}