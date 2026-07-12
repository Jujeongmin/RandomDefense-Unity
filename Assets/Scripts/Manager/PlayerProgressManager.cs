using System;
using UnityEngine;

[Serializable]
public class PlayerProgressData
{
    public int crystals;
    public int highestWave;
    public int endlessHighestWave;
    public int attackResearchLevel;
    public int startGoldResearchLevel;
    public int goldGainResearchLevel;
    public int rareSummonResearchLevel;
    public int bossDamageResearchLevel;
    public bool adsRemoved;
    public long nextRewardAdUtcTicks;
}

public class PlayerProgressManager : MonoBehaviour
{
    const string SaveKey = "RandomDefense.PlayerProgress.v1";
    PlayerProgressData m_data = new PlayerProgressData();

    public int Crystals => m_data.crystals;
    public int HighestWave => m_data.highestWave;
    public int EndlessHighestWave => m_data.endlessHighestWave;
    public PlayerProgressData Data => m_data;
    public bool AdsRemoved => m_data.adsRemoved;
    public DateTime NextRewardAdUtc => m_data.nextRewardAdUtcTicks > 0
        ? new DateTime(m_data.nextRewardAdUtcTicks, DateTimeKind.Utc)
        : DateTime.MinValue;

    public void Initialize() => Load();

    public void AddCrystals(int amount)
    {
        if (amount <= 0) return;
        m_data.crystals += amount;
        Save();
    }

    public bool SpendCrystals(int amount)
    {
        if (amount < 0 || m_data.crystals < amount) return false;
        m_data.crystals -= amount;
        Save();
        return true;
    }

    public void RecordHighestWave(int wave)
    {
        wave = Mathf.Max(0, wave);
        if (wave <= m_data.highestWave) return;
        m_data.highestWave = wave;
        Save();
    }

    /// <summary>무한모드 최고 도달 웨이브 기록.</summary>
    public void RecordEndlessWave(int wave)
    {
        wave = Mathf.Max(0, wave);
        if (wave <= m_data.endlessHighestWave) return;
        m_data.endlessHighestWave = wave;
        Save();
    }

    public void ResetProgress()
    {
        m_data = new PlayerProgressData();
        Save();
    }

    public void SetAdsRemoved()
    {
        m_data.adsRemoved = true;
        Save();
    }

    public bool CanClaimRewardAd => DateTime.UtcNow >= NextRewardAdUtc;

    public void CompleteRewardAd(int crystalReward, TimeSpan cooldown)
    {
        if (!CanClaimRewardAd) return;
        m_data.crystals += Mathf.Max(0, crystalReward);
        m_data.nextRewardAdUtcTicks = DateTime.UtcNow.Add(cooldown).Ticks;
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(m_data));
        PlayerPrefs.Save();
    }

    void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            m_data = new PlayerProgressData();
            return;
        }

        try
        {
            m_data = JsonUtility.FromJson<PlayerProgressData>(json) ?? new PlayerProgressData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Progress save could not be loaded. A new save will be used. {exception.Message}");
            m_data = new PlayerProgressData();
        }
    }
}
