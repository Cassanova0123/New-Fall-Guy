using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerData : MonoBehaviour
{
    // L'identité du joueur
    public string playerName;
    public string playerId;
    public int playerLevel;
    public int experiencePoints;

    //La money du jeu
    public int kudos;         // Normal currency
    public int crowns;        // VIP currency

    // La Stats
    public int totalGamesPlayed;
    public int gamesWon;
    public int roundsPlayed;
    public int roundsWon;
    public float winRate;
    public int totalKudosEarned;
    public int totalCrownsEarned;

    // Time Statistics
    public float totalPlayTime;
    public DateTime firstPlayDate;
    public DateTime lastPlayDate;

    // Achievements and Collections
    public List<string> unlockedCostumes;
    public List<string> unlockedEmotes;
    public List<string> unlockedCelebrations;
    public List<string> completedAchievements;

    // Season Progress
    public int currentSeasonLevel;
    public int seasonExperience;
    public List<string> seasonRewardsCollected;

    // Game Performance Stats
    public int totalObstaclesHit;
    public int totalPlayersGrabbed;
    public int timesQualified;
    public int timesFallen;
    public float fastestFinishTime;
    public float averageFinishTime;

    public Dictionary<string, int> roundWins;
    public Dictionary<string, float> bestRoundTimes;


    public PlayerData()
    {
        InitializeDefaultValues();
        InitializeCollections();
    }

    private void InitializeDefaultValues()
    {
        playerName = "New Player";
        playerId = Guid.NewGuid().ToString();
        playerLevel = 1;
        experiencePoints = 0;
        kudos = 0;
        crowns = 0;
        firstPlayDate = DateTime.Now;
        lastPlayDate = DateTime.Now;
        currentSeasonLevel = 1;
        seasonExperience = 0;
        fastestFinishTime = float.MaxValue;
        averageFinishTime = 0;
    }

    private void InitializeCollections()
    {
        unlockedCostumes = new List<string>();
        unlockedEmotes = new List<string>();
        unlockedCelebrations = new List<string>();
        completedAchievements = new List<string>();
        seasonRewardsCollected = new List<string>();
        roundWins = new Dictionary<string, int>();
        bestRoundTimes = new Dictionary<string, float>();
    }

    // Methods to update player statistics
    public void AddExperience(int amount)
    {
        experiencePoints += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        // Simple level up system (can be modified based on your needs)
        int experienceNeeded = playerLevel * 1000;
        if (experiencePoints >= experienceNeeded)
        {
            playerLevel++;
            experiencePoints -= experienceNeeded;
            // You might want to trigger a level up event here
        }
    }

    public void UpdateGameStats(bool won, float completionTime, string roundName)
    {
        totalGamesPlayed++;
        if (won) gamesWon++;
        winRate = (float)gamesWon / totalGamesPlayed;

        // Update round times
        if (!bestRoundTimes.ContainsKey(roundName))
        {
            bestRoundTimes[roundName] = completionTime;
        }
        else if (completionTime < bestRoundTimes[roundName])
        {
            bestRoundTimes[roundName] = completionTime;
        }

        // Update fastest finish time
        if (completionTime < fastestFinishTime)
        {
            fastestFinishTime = completionTime;
        }

        // Update average finish time
        averageFinishTime = ((averageFinishTime * (totalGamesPlayed - 1)) + completionTime) / totalGamesPlayed;

        lastPlayDate = DateTime.Now;
    }

    public void AddUnlockedItem(string itemId, ItemType type)
    {
        switch (type)
        {
            case ItemType.Costume:
                if (!unlockedCostumes.Contains(itemId))
                    unlockedCostumes.Add(itemId);
                break;
            case ItemType.Emote:
                if (!unlockedEmotes.Contains(itemId))
                    unlockedEmotes.Add(itemId);
                break;
            case ItemType.Celebration:
                if (!unlockedCelebrations.Contains(itemId))
                    unlockedCelebrations.Add(itemId);
                break;
        }
    }

    public void AddCurrency(int kudosAmount, int crownsAmount)
    {
        kudos += kudosAmount;
        crowns += crownsAmount;
        totalKudosEarned += kudosAmount;
        totalCrownsEarned += crownsAmount;
    }

    public bool CanPurchase(int kudosCost, int crownsCost)
    {
        return kudos >= kudosCost && crowns >= crownsCost;
    }

    public void PurchaseItem(int kudosCost, int crownsCost)
    {
        if (CanPurchase(kudosCost, crownsCost))
        {
            kudos -= kudosCost;
            crowns -= crownsCost;
        }
        else
        {
            throw new Exception("Insufficient currency for purchase");
        }
    }

    // Save player data to JSON
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    // Load player data from JSON
    public static PlayerData FromJson(string json)
    {
        return JsonUtility.FromJson<PlayerData>(json);
    }
}

// Les items
public enum ItemType
{
    Costume,
    Emote,
    Celebration
}

// Example usage class
public class PlayerDataManager : MonoBehaviour
{
    private PlayerData playerData;
    private const string SAVE_KEY = "PlayerSaveData";

    void Start()
    {
        LoadPlayerData();
    }

    void LoadPlayerData()
    {
        string savedData = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(savedData))
        {
            playerData = new PlayerData();
        }
        else
        {
            playerData = PlayerData.FromJson(savedData);
        }
    }

    void SavePlayerData()
    {
        string jsonData = playerData.ToJson();
        PlayerPrefs.SetString(SAVE_KEY, jsonData);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        SavePlayerData();
    }
}
