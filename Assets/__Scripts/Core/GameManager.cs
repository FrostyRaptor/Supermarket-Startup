using System.Collections.Generic;
using UnityEngine;
using Supermarket.Building;
using Supermarket.Config;
using Supermarket.Customers;
using Supermarket.Economy;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;

namespace Supermarket.Core
{
    /// <summary>
    /// Top-level orchestrator. Owns the day loop and wires the independent
    /// systems together so each one stays small and testable.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Core")]
        public GameConfig config;
        public GameClock clock;
        public Economy.Economy economy;
        public NotificationCenter notices;
        public GameSave save;

        [Header("Simulation")]
        public SectionManager sections;
        public EmployeeManager employees;
        public UpgradeSystem upgrades;
        public Inventory inventory;
        public SalesProcessor sales;
        public OrderManager orders;
        public ProgressionManager progression;
        public LiquorLicense license;

        [Header("Build system")]
        public BuildGrid grid;

        public static GameManager Instance { get; private set; }

        readonly List<PlacedObject> _placedBuffer = new List<PlacedObject>();

        void Awake()
        {
            Instance = this;
            Resolve();
        }

        void Resolve()
        {
            if (clock == null) clock = FindFirstObjectByType<GameClock>();
            if (economy == null) economy = FindFirstObjectByType<Economy.Economy>();
            if (notices == null) notices = FindFirstObjectByType<NotificationCenter>();
            if (sections == null) sections = FindFirstObjectByType<SectionManager>();
            if (employees == null) employees = FindFirstObjectByType<EmployeeManager>();
            if (upgrades == null) upgrades = FindFirstObjectByType<UpgradeSystem>();
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (sales == null) sales = FindFirstObjectByType<SalesProcessor>();
            if (orders == null) orders = FindFirstObjectByType<OrderManager>();
            if (progression == null) progression = FindFirstObjectByType<ProgressionManager>();
            if (license == null) license = FindFirstObjectByType<LiquorLicense>();
            if (grid == null) grid = FindFirstObjectByType<BuildGrid>();
            if (save == null) save = FindFirstObjectByType<GameSave>();

            // Checkout staffing and sales capacity follow the placed lanes.
            if (sections != null) sections.buildGrid = grid;
            if (employees != null) employees.BindSections(sections);
        }

        void Start()
        {
            Resolve();

            if (clock != null) clock.DayChanged += OnDayChanged;
            if (grid != null)
            {
                grid.ObjectPlaced += OnObjectPlaced;
                grid.ObjectRemoved += OnObjectRemoved;

                // The layout store may have loaded before this component started.
                grid.GetAllPlacedObjects(_placedBuffer);
                for (int i = 0; i < _placedBuffer.Count; i++) upgrades.RegisterPlaced(_placedBuffer[i]);
                _placedBuffer.Clear();
            }
            if (sections != null) sections.Changed += OnStateChanged;
            if (employees != null) employees.CountsChanged += OnStateChanged;

            if (employees != null) employees.ClampCheckoutStaff(false);

            if (save == null || !save.Load())
            {
                if (employees != null) employees.EvaluateStaffing(true);
            }
            else if (employees != null)
            {
                employees.EvaluateStaffing(false);
            }
        }

        void OnDestroy()
        {
            if (clock != null) clock.DayChanged -= OnDayChanged;
            if (grid != null)
            {
                grid.ObjectPlaced -= OnObjectPlaced;
                grid.ObjectRemoved -= OnObjectRemoved;
            }
            if (sections != null) sections.Changed -= OnStateChanged;
            if (employees != null) employees.CountsChanged -= OnStateChanged;
            if (Instance == this) Instance = null;
        }

        void OnDayChanged(int day)
        {
            if (economy != null) economy.BeginNewDay();
            if (inventory != null)
            {
                inventory.ProcessArrivals(day);
                inventory.ProcessUnloading();
                inventory.ProcessStocking();
                inventory.ProcessProduction();
            }
            if (sales != null) sales.ProcessDay(day);
            if (license != null) license.OnBusinessDay(day);
            if (employees != null)
            {
                employees.EvaluateStaffing(true);
                if (economy != null) economy.ChargeWages(employees.DailyWages);
            }
            if (save != null) save.Save();
        }

        void OnObjectPlaced(PlacedObject placed)
        {
            if (upgrades != null) upgrades.RegisterPlaced(placed);
            SaveSoon();
        }

        void OnObjectRemoved(PlacedObject placed)
        {
            if (upgrades != null) upgrades.ForgetPlaced(placed);
            SaveSoon();
        }

        void OnStateChanged()
        {
            SaveSoon();
        }

        void SaveSoon()
        {
            if (save != null) save.Save();
        }

        public GameSaveData CollectSave()
        {
            GameSaveData data = new GameSaveData();
            data.money = economy == null ? 0 : economy.Balance;
            data.day = clock == null ? 1 : clock.Day;
            data.elapsedSeconds = clock == null ? 0f : clock.ElapsedSeconds;
            data.lastIncome = economy == null ? 0f : economy.LastDayIncome;
            data.lastWages = economy == null ? 0f : economy.LastDayWages;
            data.totalIncome = economy == null ? 0f : economy.TotalIncome;
            data.totalWages = economy == null ? 0f : economy.TotalWages;
            data.checkoutLanes = sections == null ? 1 : sections.CheckoutLanes;
            data.loadingZones = sections == null ? 1 : sections.LoadingZones;

            if (sections != null)
            {
                foreach (KeyValuePair<StoreSectionType, bool> pair in sections.Snapshot())
                    data.sections.Add(new SectionSave(pair.Key, pair.Value));
            }
            if (employees != null)
            {
                foreach (KeyValuePair<EmployeeRole, int> pair in employees.Snapshot())
                    data.employees.Add(new EmployeeSave(pair.Key, pair.Value));
            }
            if (inventory != null)
            {
                inventory.SyncLists();
                data.storage.AddRange(inventory.storage);
                data.shelf.AddRange(inventory.shelf);
                data.finished.AddRange(inventory.finished);
                data.deliveries.AddRange(inventory.deliveries);
            }
            if (upgrades != null)
            {
                foreach (KeyValuePair<string, int> pair in upgrades.SnapshotFixed())
                    data.fixedLevels.Add(new FixedLevelSave(pair.Key, pair.Value));
                foreach (KeyValuePair<string, UpgradeableFixture> pair in upgrades.Fixtures)
                {
                    if (pair.Value != null)
                        data.fixtures.Add(new FixtureSave(pair.Key, pair.Value.level, pair.Value.stock));
                }
            }
            if (license != null)
            {
                data.license.stage = license.Stage;
                data.license.dueDay = license.DueDay;
                data.license.willFail = license.WillFail;
            }
            return data;
        }

        public void ApplySave(GameSaveData data)
        {
            if (data == null) return;
            if (clock != null) clock.Restore(data.day, data.elapsedSeconds);
            if (economy != null)
                economy.Restore(data.money, data.lastIncome, data.lastWages, data.totalIncome, data.totalWages);
            if (sections != null)
            {
                Dictionary<StoreSectionType, bool> unlocked = new Dictionary<StoreSectionType, bool>();
                for (int i = 0; i < data.sections.Count; i++) unlocked[data.sections[i].type] = data.sections[i].unlocked;
                sections.Restore(unlocked, data.checkoutLanes, data.loadingZones);
            }
            if (employees != null)
            {
                Dictionary<EmployeeRole, int> counts = new Dictionary<EmployeeRole, int>();
                for (int i = 0; i < data.employees.Count; i++) counts[data.employees[i].role] = data.employees[i].count;
                employees.Restore(counts);
            }
            if (inventory != null)
                inventory.Restore(data.storage, data.shelf, data.finished, data.deliveries);
            if (upgrades != null)
            {
                Dictionary<string, int> levels = new Dictionary<string, int>();
                for (int i = 0; i < data.fixedLevels.Count; i++) levels[data.fixedLevels[i].key] = data.fixedLevels[i].level;
                upgrades.Restore(levels, data.fixtures);
            }
            if (license != null) license.Restore(data.license.stage, data.license.dueDay, data.license.willFail);
        }

        public void ResetAll()
        {
            if (clock != null) clock.Restore(1, 0f);
            if (economy != null && config != null) economy.Restore(config.startingMoney, 0f, 0f, 0f, 0f);
            if (sections != null) sections.ResetToDefaults();
            if (employees != null)
            {
                Dictionary<EmployeeRole, int> empty = new Dictionary<EmployeeRole, int>();
                employees.Restore(empty);
                employees.EvaluateStaffing(true);
            }
            if (upgrades != null) upgrades.Restore(new Dictionary<string, int>(), new List<FixtureSave>());
            if (inventory != null) inventory.Restore(null, null, null, null);
            if (license != null) license.Restore(LicenseStage.NotStarted, 0, false);
            if (notices != null) notices.Clear();
            if (grid != null) grid.ClearAll();
            if (save != null) save.Save();
        }
    }
}
