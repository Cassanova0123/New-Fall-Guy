using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    // Singleton instance
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text multiplierText;

    [Header("Score Settings")]
    [SerializeField] private int pointsPerCheckpoint = 100;
    [SerializeField] private int pointsPerWin = 1000;
    [SerializeField] private float baseMultiplier = 1f;

    // Private variables
    private int currentScore;
    private int highScore;
    private float currentMultiplier;
    private int gamesWon;
    private List<int> topScores = new List<int>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeScore();
    }

    private void InitializeScore()
    {
        currentScore = 0;
        currentMultiplier = baseMultiplier;
        LoadHighScore();
        UpdateUI();
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        gamesWon = PlayerPrefs.GetInt("GamesWon", 0);
    }

    public void AddPoints(int points)
    {
        currentScore += Mathf.RoundToInt(points * currentMultiplier);
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        UpdateUI();
    }

    public void AddCheckpointPoints()
    {
        AddPoints(pointsPerCheckpoint);
    }

    public void UpdateMultiplier(float newMultiplier)
    {
        currentMultiplier = newMultiplier;
        UpdateUI();
    }

    public void GameWon()
    {
        AddPoints(pointsPerWin);
        gamesWon++;
        PlayerPrefs.SetInt("GamesWon", gamesWon);
        SaveTopScore();
    }

    private void SaveTopScore()
    {
        topScores.Add(currentScore);
        topScores.Sort((a, b) => b.CompareTo(a));

        // Keep only top 10 scores
        if (topScores.Count > 10)
            topScores.RemoveAt(topScores.Count - 1);

        // Save top scores
        for (int i = 0; i < topScores.Count; i++)
        {
            PlayerPrefs.SetInt($"TopScore_{i}", topScores[i]);
        }
    }

    public void ResetScore()
    {
        currentScore = 0;
        currentMultiplier = baseMultiplier;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (highScoreText != null)
            highScoreText.text = $"High Score: {highScore}";

        if (multiplierText != null)
            multiplierText.text = $"Multiplier: x{currentMultiplier:F1}";
    }

    public void AwardBonusPoints(BonusType bonusType)
    {
        switch (bonusType)
        {
            case BonusType.PerfectRun:
                AddPoints(500);
                break;
            case BonusType.QuickFinish:
                AddPoints(300);
                break;
            case BonusType.NoFalls:
                AddPoints(200);
                break;
            case BonusType.FirstPlace:
                AddPoints(1000);
                break;
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetHighScore()
    {
        return highScore;
    }

    public int GetGamesWon()
    {
        return gamesWon;
    }
}

public enum BonusType
{
    PerfectRun,
    QuickFinish,
    NoFalls,
    FirstPlace
}
