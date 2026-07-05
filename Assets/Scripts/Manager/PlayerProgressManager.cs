using System;
using UnityEngine;

[Serializable]
public class PlayerProgressData
{
    public int crystals;
    public int attackResearchLevel;
    public int startGoldResearchLevel;
    public int goldGainResearchLevel;
    public int rareSummonResearchLevel;
    public int bossDamageResearchLevel;
}

public class PlayerProgressManager : MonoBehaviour
{
    const string SaveKey = "RandomDefense.PlayerProgress.v1";
    PlayerProgressData m_data = new PlayerProgressData();

    public int Crystals => m_data.crystals;
    public PlayerProgressData Data => m_data;

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
