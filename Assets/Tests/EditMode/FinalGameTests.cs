using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Supermarket.Building;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Customers;
using GameWallet = Supermarket.Economy.Economy;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;

namespace Supermarket.Tests
{
    public class FinalGameTests
    {
        GameObject _host;
        readonly List<Object> _cleanup = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Test Host");
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _cleanup.Count; i++)
            {
                if (_cleanup[i] != null) Object.DestroyImmediate(_cleanup[i]);
            }
            _cleanup.Clear();

            Object.DestroyImmediate(_host);
        }

        static GameConfig Config()
        {
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            config.startMonth = 1;
            config.startDay = 1;
            config.startYear = 2026;
            config.realMinutesPerDay = 10f;
            config.licenseStage1MinDays = 1;
            config.licenseStage1MaxDays = 1;
            config.licenseStage2MinDays = 1;
            config.licenseStage2MaxDays = 1;
            return config;
        }

        [Test]
        public void Clock_FormatsDateAndAdvancesBusinessDays()
        {
            GameClock clock = _host.AddComponent<GameClock>();
            clock.config = Config();
            clock.Restore(1, 0f);

            Assert.AreEqual("01/01/2026", clock.DateText);
            clock.AdvanceDay();
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(2, clock.BusinessDay);
            Assert.AreEqual("01/02/2026", clock.DateText);
        }

        [Test]
        public void Clock_ReportsInGameTimeFromDayProgress()
        {
            GameClock clock = _host.AddComponent<GameClock>();
            clock.config = Config();

            clock.Restore(1, 0f);
            Assert.AreEqual("12:00 AM", clock.TimeText);

            clock.Restore(1, 75f);
            Assert.AreEqual("3:00 AM", clock.TimeText);

            clock.Restore(1, 300f);
            Assert.AreEqual("12:00 PM", clock.TimeText);
        }

        [Test]
        public void Economy_TracksSalesWagesAndNetRate()
        {
            GameWallet wallet = _host.AddComponent<GameWallet>();
            wallet.config = Config();
            wallet.Restore(1000, 0f, 0f, 0f, 0f);

            wallet.RecordSale(200f);
            wallet.ChargeWages(50);

            Assert.AreEqual(1150, wallet.Balance);
            Assert.AreEqual(200f, wallet.LastDayIncome, 0.01f);
            Assert.AreEqual(50f, wallet.LastDayWages, 0.01f);
            Assert.AreEqual(15f, wallet.NetPerRealMinute, 0.01f);
            Assert.AreEqual(900f, wallet.NetPerRealHour, 0.01f);
        }

        [Test]
        public void Employees_CapsFollowLanesAndZeroStaffingWarns()
        {
            EmployeeConfig wages = ScriptableObject.CreateInstance<EmployeeConfig>();
            SectionConfig sectionConfig = ScriptableObject.CreateInstance<SectionConfig>();

            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.config = sectionConfig;
            sections.Restore(null, 1, 1);

            NotificationCenter notices = _host.AddComponent<NotificationCenter>();
            List<string> messages = new List<string>();
            notices.NoticeRaised += delegate(string m, NoticeKind k) { messages.Add(m); };

            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = wages;
            employees.sections = sections;
            employees.notices = notices;
            employees.Restore(null);

            Assert.AreEqual(1, employees.Max(EmployeeRole.Checkout));
            Assert.AreEqual(5, employees.Max(EmployeeRole.Butcher));
            Assert.AreEqual(6, employees.Max(EmployeeRole.Unloader));
            Assert.AreEqual(8, employees.Max(EmployeeRole.Stocker));

            employees.EvaluateStaffing(true);
            Assert.IsFalse(employees.CheckoutOpen);
            Assert.IsTrue(messages.Exists(m => m.Contains("checkout")));
            Assert.IsTrue(messages.Exists(m => m.Contains("unloaded")));
            Assert.IsTrue(messages.Exists(m => m.Contains("stocked")));

            Assert.IsTrue(employees.TryHire(EmployeeRole.Checkout));
            Assert.IsTrue(employees.CheckoutOpen);
            Assert.IsFalse(employees.TryHire(EmployeeRole.Checkout), "One lane allows only one checkout employee.");

            sections.Restore(null, 2, 1);
            Assert.AreEqual(2, employees.Max(EmployeeRole.Checkout));
            Assert.IsTrue(employees.TryHire(EmployeeRole.Checkout));
            Assert.AreEqual(2, employees.Count(EmployeeRole.Checkout));
        }

        [Test]
        public void PlacedCheckoutLanes_SetCheckoutEmployeeCap()
        {
            GameObject gridHost = new GameObject("Test Build Grid");
            _cleanup.Add(gridHost);
            BuildGrid grid = gridHost.AddComponent<BuildGrid>();
            grid.size = new Vector2Int(96, 256);
            grid.cellSize = 1f;
            grid.origin = new Vector3(-8f, 0.75f, -8f);
            StoreBuildZones.ApplyTo(grid);

            GameObject prefab = new GameObject("Test Lane Prefab");
            _cleanup.Add(prefab);
            prefab.AddComponent<PlacedObject>();
            PlaceableDefinition lane = ScriptableObject.CreateInstance<PlaceableDefinition>();
            _cleanup.Add(lane);
            lane.id = "test_checkout_lane";
            lane.prefab = prefab;
            lane.footprint = new Vector2Int(4, 3);
            lane.allowedArea = PlaceableArea.Checkout;

            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.config = ScriptableObject.CreateInstance<SectionConfig>();
            sections.Restore(null, 0, 1);
            sections.buildGrid = grid;

            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = ScriptableObject.CreateInstance<EmployeeConfig>();
            employees.BindSections(sections);
            employees.Restore(null);

            Assert.AreEqual(0, employees.Max(EmployeeRole.Checkout));
            Assert.IsFalse(employees.TryHire(EmployeeRole.Checkout));

            PlacedObject first = grid.Place(lane, grid.WorldToCoord(new Vector3(-7.5f, 0.75f, 60f)), 0);
            Assert.IsNotNull(first);
            Assert.AreEqual(1, sections.CheckoutLanes);
            Assert.AreEqual(1, employees.Max(EmployeeRole.Checkout));
            Assert.IsTrue(employees.TryHire(EmployeeRole.Checkout));
            Assert.IsFalse(employees.TryHire(EmployeeRole.Checkout));

            PlacedObject second = grid.Place(lane, grid.WorldToCoord(new Vector3(-7.5f, 0.75f, 70f)), 0);
            Assert.IsNotNull(second);
            Assert.AreEqual(2, employees.Max(EmployeeRole.Checkout));
            Assert.IsTrue(employees.TryHire(EmployeeRole.Checkout));
            Assert.AreEqual(2, employees.Count(EmployeeRole.Checkout));

            Assert.IsTrue(grid.Remove(first));
            Assert.AreEqual(1, sections.CheckoutLanes);
            Assert.AreEqual(1, employees.Max(EmployeeRole.Checkout));
            Assert.AreEqual(1, employees.Count(EmployeeRole.Checkout), "Removing a lane removes the excess checkout hire.");
        }

        [Test]
        public void ZeroCheckoutLanes_CloseCheckoutAndBlockHiring()
        {
            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.config = ScriptableObject.CreateInstance<SectionConfig>();
            sections.Restore(null, 0, 1);

            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = ScriptableObject.CreateInstance<EmployeeConfig>();
            employees.sections = sections;
            employees.Restore(null);

            Assert.AreEqual(0, sections.CheckoutLanes);
            Assert.AreEqual(0, employees.Max(EmployeeRole.Checkout));
            Assert.IsFalse(employees.TryHire(EmployeeRole.Checkout));
            Assert.IsFalse(employees.CheckoutOpen);

            Assert.IsTrue(sections.TryExpandCheckout());
            Assert.AreEqual(1, employees.Max(EmployeeRole.Checkout));
            Assert.IsTrue(employees.TryHire(EmployeeRole.Checkout));
            Assert.IsTrue(employees.CheckoutOpen);
        }

        [Test]
        public void License_StageTwoFailureRestartsTheProcess()
        {
            GameConfig config = Config();
            config.licenseStage1Fee = 100;
            config.licenseStage2Fee = 200;
            config.licenseStage1FailChance = 0.5f;
            config.licenseStage2FailChance = 0.5f;

            NotificationCenter notices = _host.AddComponent<NotificationCenter>();
            LiquorLicense license = _host.AddComponent<LiquorLicense>();
            license.config = config;
            license.notices = notices;

            GameWallet wallet = _host.AddComponent<GameWallet>();
            wallet.Restore(1000, 0f, 0f, 0f, 0f);

            Queue<float> rolls = new Queue<float>();
            // Stage 1: due-day roll then pass roll; stage 2: due-day roll then fail roll.
            rolls.Enqueue(0.9f);
            rolls.Enqueue(0.9f);
            rolls.Enqueue(0.9f);
            rolls.Enqueue(0.1f);
            license.RandomValue = delegate { return rolls.Count > 0 ? rolls.Dequeue() : 0.9f; };

            Assert.IsTrue(license.TryFileStage1(wallet, 1));
            license.OnBusinessDay(license.DueDay);
            Assert.AreEqual(LicenseStage.Stage1Approved, license.Stage);

            Assert.IsTrue(license.TryFileStage2(wallet, 2));
            license.OnBusinessDay(license.DueDay);
            Assert.AreEqual(LicenseStage.NotStarted, license.Stage);
            Assert.IsFalse(license.IsApproved);
        }

        [Test]
        public void Inventory_WithoutUnloadersTrucksStayInStalls()
        {
            ProductCatalog catalog = ScriptableObject.CreateInstance<ProductCatalog>();
            catalog.products.Add(new ProductInfo { id = "milk", displayName = "Milk", section = StoreSectionType.General, unitCost = 10f });

            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.Restore(null, 1, 1);
            EmployeeConfig wages = ScriptableObject.CreateInstance<EmployeeConfig>();
            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = wages;
            employees.sections = sections;
            employees.Restore(null);

            Inventory inventory = _host.AddComponent<Inventory>();
            inventory.catalog = catalog;
            inventory.sections = sections;
            inventory.employees = employees;

            inventory.ScheduleDelivery("milk", 8, 1);
            inventory.ProcessArrivals(1);
            Assert.AreEqual(1, inventory.ActiveTrucks);

            inventory.ProcessUnloading();
            Assert.AreEqual(0, inventory.StorageCount("milk"));

            Assert.IsTrue(employees.TryHire(EmployeeRole.Unloader));
            inventory.ProcessUnloading();
            Assert.AreEqual(8, inventory.StorageCount("milk"));
            Assert.AreEqual(0, inventory.ActiveTrucks);
        }

        [Test]
        public void Sales_RespectsStockAndCheckoutStaffing()
        {
            GameConfig config = Config();
            ProductCatalog catalog = ScriptableObject.CreateInstance<ProductCatalog>();
            catalog.products.Add(new ProductInfo
            {
                id = "milk", displayName = "Milk", section = StoreSectionType.General,
                kind = ProductKind.Shelf, salePrice = 20f, baseDailyDemand = 12f
            });

            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.Restore(null, 1, 1);
            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = ScriptableObject.CreateInstance<EmployeeConfig>();
            employees.sections = sections;
            employees.Restore(null);

            Inventory inventory = _host.AddComponent<Inventory>();
            inventory.catalog = catalog;
            inventory.sections = sections;
            inventory.employees = employees;
            inventory.AddShelf("milk", 5);

            GameWallet wallet = _host.AddComponent<GameWallet>();
            wallet.Restore(0, 0f, 0f, 0f, 0f);

            SalesProcessor sales = _host.AddComponent<SalesProcessor>();
            sales.config = config;
            sales.catalog = catalog;
            sales.inventory = inventory;
            sales.economy = wallet;
            sales.employees = employees;
            sales.sections = sections;

            sales.ProcessDay(1);
            Assert.AreEqual(0, sales.LastUnitsSold, "No checkout employee means no sales.");

            employees.TryHire(EmployeeRole.Checkout);
            employees.TryHire(EmployeeRole.Stocker);
            sales.ProcessDay(1);
            Assert.AreEqual(5, sales.LastUnitsSold, "Only the 5 units on the shelf can be sold.");
            Assert.AreEqual(100f, wallet.LastDayIncome, 0.01f);
        }

        [Test]
        public void Orders_SuggestionsExceedProjectedDemand()
        {
            GameConfig config = Config();
            ProductCatalog catalog = ScriptableObject.CreateInstance<ProductCatalog>();
            catalog.products.Add(new ProductInfo
            {
                id = "milk", displayName = "Milk", section = StoreSectionType.General,
                unitCost = 10f, salePrice = 20f, baseDailyDemand = 10f
            });
            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.Restore(null, 1, 1);
            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = ScriptableObject.CreateInstance<EmployeeConfig>();
            employees.sections = sections;
            Inventory inventory = _host.AddComponent<Inventory>();
            inventory.catalog = catalog;
            inventory.sections = sections;
            inventory.employees = employees;
            GameWallet wallet = _host.AddComponent<GameWallet>();
            wallet.Restore(1000, 0f, 0f, 0f, 0f);
            GameClock clock = _host.AddComponent<GameClock>();
            clock.config = config;
            clock.Restore(1, 0f);
            SalesProcessor sales = _host.AddComponent<SalesProcessor>();
            sales.config = config;
            sales.catalog = catalog;
            sales.sections = sections;
            sales.employees = employees;
            sales.inventory = inventory;

            OrderManager orders = _host.AddComponent<OrderManager>();
            orders.config = config;
            orders.catalog = catalog;
            orders.inventory = inventory;
            orders.economy = wallet;
            orders.sales = sales;
            orders.clock = clock;

            int suggestion = orders.SuggestQuantity("milk");
            Assert.Greater(suggestion, 10);
            Assert.IsTrue(orders.TryPlaceOrder("milk", suggestion));
            Assert.AreEqual(1, inventory.deliveries.Count);
            Assert.AreEqual(2, inventory.deliveries[0].arrivalDay);
        }

        [Test]
        public void GameManager_SaveRoundTripPreservesState()
        {
            GameConfig config = Config();
            SectionManager sections = _host.AddComponent<SectionManager>();
            sections.Restore(null, 2, 1);
            EmployeeManager employees = _host.AddComponent<EmployeeManager>();
            employees.config = ScriptableObject.CreateInstance<EmployeeConfig>();
            employees.sections = sections;
            employees.Restore(null);
            employees.TryHire(EmployeeRole.Checkout);

            GameClock clock = _host.AddComponent<GameClock>();
            clock.config = config;
            clock.Restore(4, 12f);
            GameWallet wallet = _host.AddComponent<GameWallet>();
            wallet.Restore(777, 50f, 20f, 300f, 100f);

            GameManager manager = _host.AddComponent<GameManager>();
            manager.config = config;
            manager.clock = clock;
            manager.economy = wallet;
            manager.sections = sections;
            manager.employees = employees;

            GameSaveData data = manager.CollectSave();
            Assert.AreEqual(777, data.money);
            Assert.AreEqual(4, data.day);
            Assert.AreEqual(2, data.checkoutLanes);
            Assert.AreEqual(1, CountRole(data, EmployeeRole.Checkout));

            GameManager other = new GameObject("Other").AddComponent<GameManager>();
            other.config = config;
            other.clock = new GameObject("Clock").AddComponent<GameClock>();
            other.economy = new GameObject("Wallet").AddComponent<GameWallet>();
            other.sections = new GameObject("Sections").AddComponent<SectionManager>();
            other.employees = new GameObject("Employees").AddComponent<EmployeeManager>();
            other.employees.config = ScriptableObject.CreateInstance<EmployeeConfig>();
            other.employees.sections = other.sections;

            other.ApplySave(data);
            Assert.AreEqual(777, other.economy.Balance);
            Assert.AreEqual(4, other.clock.Day);
            Assert.AreEqual(2, other.sections.CheckoutLanes);
            Assert.AreEqual(1, other.employees.Count(EmployeeRole.Checkout));

            Object.DestroyImmediate(other.gameObject);
            Object.DestroyImmediate(other.clock.gameObject);
            Object.DestroyImmediate(other.economy.gameObject);
            Object.DestroyImmediate(other.sections.gameObject);
            Object.DestroyImmediate(other.employees.gameObject);
        }

        static int CountRole(GameSaveData data, EmployeeRole role)
        {
            int count = 0;
            for (int i = 0; i < data.employees.Count; i++) if (data.employees[i].role == role) count += data.employees[i].count;
            return count;
        }
    }
}
