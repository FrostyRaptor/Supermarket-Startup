using System;
using System.Collections.Generic;
using UnityEngine;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;
using ConfigSection = Supermarket.Config.StoreSectionType;

namespace Supermarket.Core
{
    [Serializable]
    public class SectionSave
    {
        public ConfigSection type;
        public bool unlocked;
        public SectionSave() { }
        public SectionSave(ConfigSection type, bool unlocked) { this.type = type; this.unlocked = unlocked; }
    }

    [Serializable]
    public class EmployeeSave
    {
        public EmployeeRole role;
        public int count;
        public EmployeeSave() { }
        public EmployeeSave(EmployeeRole role, int count) { this.role = role; this.count = count; }
    }

    [Serializable]
    public class FixedLevelSave
    {
        public string key;
        public int level;
        public FixedLevelSave() { }
        public FixedLevelSave(string key, int level) { this.key = key; this.level = level; }
    }

    [Serializable]
    public class LicenseSaveData
    {
        public LicenseStage stage;
        public int dueDay;
        public bool willFail;
    }

    [Serializable]
    public class GameSaveData
    {
        public int version = 3;
        public int money;
        public int day;
        public float elapsedSeconds;
        public float lastIncome;
        public float lastWages;
        public float totalIncome;
        public float totalWages;
        public int checkoutLanes;
        public int loadingZones;

        public List<SectionSave> sections = new List<SectionSave>();
        public List<EmployeeSave> employees = new List<EmployeeSave>();
        public List<StockEntry> storage = new List<StockEntry>();
        public List<StockEntry> shelf = new List<StockEntry>();
        public List<StockEntry> finished = new List<StockEntry>();
        public List<Delivery> deliveries = new List<Delivery>();
        public List<FixedLevelSave> fixedLevels = new List<FixedLevelSave>();
        public List<FixtureSave> fixtures = new List<FixtureSave>();
        public LicenseSaveData license = new LicenseSaveData();
    }

    /// <summary>Full-game persistence through PlayerPrefs (WebGL-safe, no file I/O).</summary>
    public class GameSave : MonoBehaviour
    {
        public const string SaveKey = "supermarket.game-state.v3";
        public GameManager game;

        public bool HasSave { get { return PlayerPrefs.HasKey(SaveKey); } }

        public void Save()
        {
            if (game == null) return;
            string json = JsonUtility.ToJson(game.CollectSave());
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public bool Load()
        {
            if (game == null || !PlayerPrefs.HasKey(SaveKey)) return false;
            try
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(SaveKey));
                if (data == null || data.version != 3) return false;
                game.ApplySave(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not load game save: " + e.Message);
                return false;
            }
        }

        public void ResetGame()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            if (game != null) game.ResetAll();
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focused) { if (!focused) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}
