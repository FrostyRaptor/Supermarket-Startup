using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Customers;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;
using GameWallet = Supermarket.Economy.Economy;

namespace Supermarket.UI
{
    /// <summary>
    /// Drives the authored Tycoon HUD Canvas prefab. The three tab bodies are
    /// static hierarchies in the prefab; this controller only shows the active
    /// one and writes the current game values into its authored text/buttons.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public GameManager game;
        public GameClock clock;
        public GameWallet economy;
        public NotificationCenter notices;
        public SectionManager sections;
        public SectionConfig sectionConfig;
        public EmployeeManager employees;
        public EmployeeConfig employeeConfig;
        public UpgradeSystem upgrades;
        public Inventory inventory;
        public OrderManager orders;
        public SalesProcessor sales;
        public ProgressionManager progression;
        public LiquorLicense license;
        public ProductCatalog catalog;

        [Header("Authored Canvas")]
        public GameObject hudPrefab;

        enum PanelKind { None, Employees, Upgrades, Orders }

        static readonly Color Dark = new Color(0.06f, 0.09f, 0.13f, 0.94f);
        static readonly Color Light = new Color(0.91f, 0.94f, 0.97f, 1f);
        static readonly Color Accent = new Color(0.32f, 0.68f, 0.42f, 1f);
        static readonly Color Warn = new Color(0.92f, 0.49f, 0.28f, 1f);
        static readonly Color Red = new Color(0.88f, 0.31f, 0.26f, 1f);
        static readonly Color Yellow = new Color(0.95f, 0.76f, 0.25f, 1f);
        static readonly Color Disabled = new Color(0.19f, 0.21f, 0.25f, 1f);
        static readonly Color DisabledText = new Color(0.55f, 0.58f, 0.62f, 1f);
        static readonly Color NeutralButton = new Color(0.3f, 0.33f, 0.38f, 1f);
        static readonly Color InactiveTab = new Color(0.24f, 0.28f, 0.34f, 1f);

        static readonly StoreSectionType[] UpgradeSections =
        {
            StoreSectionType.Storage, StoreSectionType.Butcher, StoreSectionType.Bakery,
            StoreSectionType.Deli, StoreSectionType.Alcohol
        };

        static readonly string[] EmployeeLabels = { "Cashier", "Unloader", "Stocker", "Butcher", "Baker", "Deli" };

        static readonly EmployeeRole[] EmployeeRoles =
        {
            EmployeeRole.Checkout, EmployeeRole.Unloader, EmployeeRole.Stocker,
            EmployeeRole.Butcher, EmployeeRole.Bakery, EmployeeRole.Deli
        };

        HUDView _view;
        GameObject _hudInstance;
        int _lastDay = -1;
        int _lastMinuteOfDay = -1;
        PanelKind _openPanel;
        readonly List<Toast> _toasts = new List<Toast>();
        readonly Dictionary<string, int> _cart = new Dictionary<string, int>();

        class Toast
        {
            public GameObject go;
            public CanvasGroup group;
            public float expiry;
        }

        void Awake()
        {
            if (game == null) game = FindFirstObjectByType<GameManager>();
            if (game != null)
            {
                if (clock == null) clock = game.clock;
                if (economy == null) economy = game.economy;
                if (notices == null) notices = game.notices;
                if (sections == null) sections = game.sections;
                if (employees == null) employees = game.employees;
                if (upgrades == null) upgrades = game.upgrades;
                if (inventory == null) inventory = game.inventory;
                if (orders == null) orders = game.orders;
                if (sales == null) sales = game.sales;
                if (progression == null) progression = game.progression;
                if (license == null) license = game.license;
            }
            if (employeeConfig == null && employees != null) employeeConfig = employees.config;
            if (catalog == null) catalog = FindFirstObjectByType<ProductCatalog>();
            if (catalog == null && orders != null) catalog = orders.catalog;
        }

        void Start()
        {
            EnsureEventSystem();
            if (!InstantiateView()) return;
            if (notices != null) notices.NoticeRaised += OnNotice;
            if (sections != null) sections.Changed += OnStateChangedUI;
            if (employees != null) employees.CountsChanged += OnStateChangedUI;
            if (upgrades != null) upgrades.Changed += OnStateChangedUI;
            RefreshStatus();
        }

        void OnDestroy()
        {
            if (notices != null) notices.NoticeRaised -= OnNotice;
            if (sections != null) sections.Changed -= OnStateChangedUI;
            if (employees != null) employees.CountsChanged -= OnStateChangedUI;
            if (upgrades != null) upgrades.Changed -= OnStateChangedUI;
        }

        void OnStateChangedUI()
        {
            RefreshPanel();
        }

        void Update()
        {
            RefreshStatus();
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                Toast t = _toasts[i];
                if (t.go == null) { _toasts.RemoveAt(i); continue; }
                float remain = t.expiry - Time.unscaledTime;
                if (remain <= 0f)
                {
                    Destroy(t.go);
                    _toasts.RemoveAt(i);
                    continue;
                }
                if (remain < 1f && t.group != null) t.group.alpha = remain;
            }
        }

        bool InstantiateView()
        {
            if (hudPrefab == null)
            {
                Debug.LogError("HUDController needs an authored HUD Canvas prefab assigned.", this);
                enabled = false;
                return false;
            }

            _hudInstance = Instantiate(hudPrefab, transform);
            _view = _hudInstance.GetComponent<HUDView>();
            if (_view == null)
            {
                Debug.LogError("Authored HUD prefab is missing its HUDView component.", this);
                enabled = false;
                return false;
            }

            if (_view.storeButton != null) _view.storeButton.onClick.AddListener(OpenStore);
            if (_view.modalCloseButton != null) _view.modalCloseButton.onClick.AddListener(ClosePanel);
            if (_view.employeesTab != null) _view.employeesTab.onClick.AddListener(delegate { ShowPanel(PanelKind.Employees); });
            if (_view.upgradesTab != null) _view.upgradesTab.onClick.AddListener(delegate { ShowPanel(PanelKind.Upgrades); });
            if (_view.ordersTab != null) _view.ordersTab.onClick.AddListener(delegate { ShowPanel(PanelKind.Orders); });
            if (_view.orders != null)
            {
                if (_view.orders.topSubmitButton != null) _view.orders.topSubmitButton.onClick.AddListener(SubmitCart);
                if (_view.orders.bottomSubmitButton != null) _view.orders.bottomSubmitButton.onClick.AddListener(SubmitCart);
            }

            ShowStaticPanel(PanelKind.None);
            if (_view.modalRoot != null) _view.modalRoot.SetActive(false);
            return true;
        }

        void RefreshStatus()
        {
            if (_view == null || _view.dayText == null) return;

            int day = clock == null ? 1 : clock.Day;
            int minuteOfDay = Mathf.FloorToInt((clock == null ? 0f : clock.DayProgress) * 24f * 60f);
            if (day == _lastDay && minuteOfDay == _lastMinuteOfDay) return;

            _lastDay = day;
            _lastMinuteOfDay = minuteOfDay;
            string time = clock == null ? "12:00 AM" : clock.TimeText;
            _view.dayText.text = "Day " + day + "   " + time;
        }

        void OpenStore() { ShowPanel(PanelKind.Employees); }

        void ShowPanel(PanelKind kind)
        {
            if (_view == null || _view.modalRoot == null) return;

            _openPanel = kind;
            _view.modalRoot.SetActive(true);
            ApplyTabColors(kind);
            ShowStaticPanel(kind);
            RefreshPanel();
            if (_view.modalScroll != null) _view.modalScroll.verticalNormalizedPosition = 1f;
        }

        void ClosePanel()
        {
            _openPanel = PanelKind.None;
            if (_view != null && _view.modalRoot != null) _view.modalRoot.SetActive(false);
        }

        void ShowStaticPanel(PanelKind kind)
        {
            if (_view == null) return;

            if (_view.employeesPanel != null) _view.employeesPanel.SetActive(kind == PanelKind.Employees);
            if (_view.upgradesPanel != null) _view.upgradesPanel.SetActive(kind == PanelKind.Upgrades);
            if (_view.ordersPanel != null) _view.ordersPanel.SetActive(kind == PanelKind.Orders);
        }

        void ApplyTabColors(PanelKind kind)
        {
            SetTabColor(_view.employeesTab, kind == PanelKind.Employees);
            SetTabColor(_view.upgradesTab, kind == PanelKind.Upgrades);
            SetTabColor(_view.ordersTab, kind == PanelKind.Orders);
        }

        static void SetTabColor(Button button, bool selected)
        {
            if (button == null) return;
            Image image = button.targetGraphic as Image;
            if (image == null) image = button.GetComponent<Image>();
            if (image != null) image.color = selected ? Accent : InactiveTab;
        }

        void RefreshPanel()
        {
            if (_view == null || _openPanel == PanelKind.None) return;

            switch (_openPanel)
            {
                case PanelKind.Employees: RefreshEmployeesPanel(); break;
                case PanelKind.Upgrades: RefreshUpgradesPanel(); break;
                case PanelKind.Orders: RefreshOrdersPanel(); break;
            }
        }

        void RefreshEmployeesPanel()
        {
            HudEmployeesView view = _view.employees;
            if (view == null) return;

            HudRowView[] rows = view.rows;
            for (int i = 0; i < EmployeeRoles.Length; i++)
            {
                if (rows == null || i >= rows.Length) break;
                HudRowView row = rows[i];
                if (row == null) continue;

                EmployeeRole role = EmployeeRoles[i];
                string label = employees == null
                    ? EmployeeLabels[i]
                    : EmployeeLabels[i] + "   " + employees.Count(role) + " / " + employees.Max(role) +
                      "   $" + Wage(role) + "/day";
                SetRowLabel(row, label, true);

                ShowButton(row.leftButton);
                ConfigureButton(row.leftButton, row.leftLabel, "-", NeutralButton,
                    employees == null ? null : delegate { employees.TryFire(role); RefreshPanel(); }, employees != null);

                ShowButton(row.rightButton);
                ConfigureButton(row.rightButton, row.rightLabel, "+", Accent,
                    employees == null ? null : delegate { employees.TryHire(role); RefreshPanel(); }, employees != null);
            }

            if (view.wagesText != null)
            {
                view.wagesText.text = "Daily wages: $" + (employees == null ? 0 : employees.DailyWages) + " / day";
            }
        }

        int Wage(EmployeeRole role)
        {
            return WageFor(employeeConfig, role);
        }

        /// <summary>Reads the configured daily wage for one staffable role.</summary>
        public static int WageFor(EmployeeConfig config, EmployeeRole role)
        {
            return config == null ? 0 : config.WageFor(role);
        }

        void RefreshOrdersPanel()
        {
            HudOrdersView view = _view.orders;
            if (view == null) return;

            PruneCart();

            int total = CartTotal();
            SetOrderSummary(view.topTotalText, view.topSubmitButton, total);
            SetOrderSummary(view.bottomTotalText, view.bottomSubmitButton, total);

            if (view.rows == null) return;

            for (int i = 0; i < view.rows.Length; i++)
            {
                HudOrderRowView row = view.rows[i];
                if (row == null) continue;

                bool available = catalog != null && orders != null &&
                    i < catalog.products.Count && catalog.products[i] != null;
                if (!available)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }
                row.gameObject.SetActive(true);

                ProductInfo product = catalog.products[i];
                bool canOrder = orders.CanOrderProduct(product) && IsSectionUnlocked(product.section);
                if (!canOrder && _cart.ContainsKey(product.id)) _cart.Remove(product.id);

                if (row.nameText != null)
                {
                    row.nameText.text = product.displayName;
                    row.nameText.color = canOrder ? Light : DisabledText;
                }
                if (row.costText != null)
                {
                    row.costText.text = "$" + product.unitCost.ToString("0.00");
                    row.costText.color = canOrder ? Light : DisabledText;
                }

                int suggestion = orders.SuggestQuantity(product.id);
                if (row.suggestionText != null)
                {
                    row.suggestionText.text = suggestion.ToString();
                    row.suggestionText.color = canOrder ? Light : DisabledText;
                }
                if (row.quantityInput != null)
                {
                    row.quantityInput.text = suggestion.ToString();
                    row.quantityInput.interactable = canOrder;
                }
                if (row.background != null) row.background.color = canOrder ? Dark : Disabled;

                string id = product.id;
                InputField input = row.quantityInput;
                if (row.orderButton != null)
                {
                    row.orderButton.interactable = canOrder;
                    Image image = row.orderButton.targetGraphic as Image;
                    if (image == null) image = row.orderButton.GetComponent<Image>();
                    if (image != null) image.color = canOrder ? Accent : Disabled;
                    row.orderButton.onClick.RemoveAllListeners();
                    if (canOrder) row.orderButton.onClick.AddListener(delegate { AddToCart(id, input); });
                }

            }
        }

        void SetOrderSummary(Text totalText, Button submitButton, int total)
        {
            bool affordable = economy == null || economy.CanAfford(total);
            if (totalText != null)
            {
                totalText.text = "Total Cost: $" + total;
                totalText.color = TotalCostColor(affordable);
            }
            if (submitButton != null)
            {
                ConfigureButton(submitButton, ButtonLabel(submitButton), "Buy", Accent,
                    SubmitCart, _cart.Count > 0 && affordable);
            }
        }

        void AddToCart(string productId, InputField field)
        {
            int quantity = 0;
            if (field != null) int.TryParse(field.text, out quantity);
            if (quantity <= 0) return;

            int current;
            _cart.TryGetValue(productId, out current);
            _cart[productId] = current + quantity;
            RefreshPanel();
        }

        void SubmitCart()
        {
            if (_cart.Count == 0 || orders == null) return;
            int total = CartTotal();
            if (economy != null && !economy.CanAfford(total)) return;

            Dictionary<string, int> snapshot = new Dictionary<string, int>(_cart);
            foreach (KeyValuePair<string, int> pair in snapshot)
                orders.TryPlaceOrder(pair.Key, pair.Value);

            _cart.Clear();
            RefreshPanel();
        }

        int CartTotal()
        {
            if (orders == null) return 0;
            int total = 0;
            foreach (KeyValuePair<string, int> pair in _cart)
                total += orders.OrderCost(pair.Key, pair.Value);
            return total;
        }

        void PruneCart()
        {
            if (_cart.Count == 0 || catalog == null || orders == null) return;
            List<string> ids = new List<string>(_cart.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                ProductInfo product = catalog.Get(ids[i]);
                if (product == null || !orders.CanOrderProduct(product) || !IsSectionUnlocked(product.section))
                    _cart.Remove(ids[i]);
            }
        }

        void RefreshUpgradesPanel()
        {
            HudUpgradesView view = _view.upgrades;
            if (view == null) return;

            for (int i = 0; i < UpgradeSections.Length; i++)
            {
                StoreSectionType section = UpgradeSections[i];
                bool unlocked = IsSectionUnlocked(section);
                HudRowView title = view.sectionRows != null && i < view.sectionRows.Length ? view.sectionRows[i] : null;
                RefreshSectionTitle(title, section, unlocked);
            }

            RefreshStorageSection(view, IsSectionUnlocked(StoreSectionType.Storage));
            RefreshSectionPair(view.butcherRows, "Equipment Upgrade", UpgradeSystem.ButcherKey,
                upgrades == null ? null : upgrades.ButcherProfile, UpgradeSystem.ButcherStorageKey,
                upgrades == null ? null : upgrades.ButcherStorageProfile, StoreSectionType.Butcher);
            RefreshSectionPair(view.bakeryRows, "Equipment Upgrade", UpgradeSystem.BakeryKey,
                upgrades == null ? null : upgrades.BakeryProfile, UpgradeSystem.BakeryStorageKey,
                upgrades == null ? null : upgrades.BakeryStorageProfile, StoreSectionType.Bakery);
            RefreshSectionPair(view.deliRows, "Equipment Upgrade", UpgradeSystem.DeliKey,
                upgrades == null ? null : upgrades.DeliProfile, UpgradeSystem.DeliStorageKey,
                upgrades == null ? null : upgrades.DeliStorageProfile, StoreSectionType.Deli);
            RefreshAlcoholSection(view, IsSectionUnlocked(StoreSectionType.Alcohol));
        }

        void RefreshSectionTitle(HudRowView row, StoreSectionType section, bool unlocked)
        {
            if (row == null) return;

            if (row.label != null)
            {
                row.label.text = section.ToString();
                row.label.color = TitleColor(section, unlocked);
            }
            HideButton(row.leftButton);

            if (row.priceText != null)
            {
                row.priceText.gameObject.SetActive(!unlocked);
                row.priceText.text = "$" + (sections == null ? 0 : sections.UnlockCost(section));
                row.priceText.color = unlocked ? DisabledText : Light;
            }

            if (unlocked)
            {
                HideButton(row.rightButton);
                return;
            }

            ShowButton(row.rightButton);
            StoreSectionType captured = section;
            ConfigureButton(row.rightButton, row.rightLabel, "Buy", Accent, delegate
            {
                if (progression != null) progression.TryUnlockSection(captured);
                else if (sections != null) sections.TryUnlock(captured);
                RefreshPanel();
            }, true);
        }

        Color TitleColor(StoreSectionType section, bool unlocked)
        {
            if (section == StoreSectionType.Alcohol && license != null && !license.IsApproved &&
                license.Stage != LicenseStage.NotStarted)
                return Yellow;
            return unlocked ? Accent : Red;
        }

        void RefreshStorageSection(HudUpgradesView view, bool unlocked)
        {
            if (view.storageInfoText != null)
            {
                view.storageInfoText.gameObject.SetActive(true);
                view.storageInfoText.text = "Loading zones: " + (sections == null ? 1 : sections.LoadingZones) + " / " +
                    (sections == null ? 3 : sections.MaxLoadingZones);
                view.storageInfoText.color = unlocked ? Light : DisabledText;
            }

            HudRowView[] rows = view.storageRows;
            if (rows == null) return;

            int zones = sections == null ? 1 : sections.LoadingZones;
            int maxZones = sections == null ? 3 : sections.MaxLoadingZones;
            int expandCost = sectionConfig == null ? 300 : Mathf.Max(0, sectionConfig.loadingZoneCost);
            if (rows.Length > 0)
            {
                HudRowView expand = rows[0];
                RefreshPurchaseRow(expand, "Add loading zone", expandCost, unlocked && zones < maxZones,
                    Accent, delegate { if (sections != null) sections.TryExpandLoadingZone(); RefreshPanel(); });
            }

            UpgradeProfile profile = upgrades == null ? null : upgrades.LoadingZoneProfile;
            if (rows.Length > 1)
                ApplyUpgradeRow(rows[1], "Loading Zone 1 Upgrade", UpgradeSystem.LoadingZoneKeyPrefix + "0", profile, unlocked);
            if (rows.Length > 2)
                ApplyUpgradeRow(rows[2], "Loading Zone 2 Upgrade", UpgradeSystem.LoadingZoneKeyPrefix + "1", profile,
                    unlocked && zones >= 2);
            if (rows.Length > 3)
                ApplyUpgradeRow(rows[3], "Loading Zone 3 Upgrade", UpgradeSystem.LoadingZoneKeyPrefix + "2", profile,
                    unlocked && zones >= 3);
            if (rows.Length > 4)
                ApplyUpgradeRow(rows[4], "Storage Shelves Upgrade", UpgradeSystem.StorageShelfKeyPrefix + "0",
                    upgrades == null ? null : upgrades.StorageShelfProfile, unlocked);
        }

        void RefreshSectionPair(HudRowView[] rows, string equipmentLabel, string equipmentKey, UpgradeProfile equipmentProfile,
            string storageKey, UpgradeProfile storageProfile, StoreSectionType section)
        {
            if (rows == null) return;
            bool unlocked = IsSectionUnlocked(section);
            if (rows.Length > 0) ApplyUpgradeRow(rows[0], equipmentLabel, equipmentKey, equipmentProfile, unlocked);
            if (rows.Length > 1) ApplyUpgradeRow(rows[1], "Storage Upgrade", storageKey, storageProfile, unlocked);
        }

        void ApplyUpgradeRow(HudRowView row, string label, string key, UpgradeProfile profile, bool enabled)
        {
            if (row == null) return;
            row.gameObject.SetActive(true);

            int level = upgrades == null ? 1 : upgrades.GetLevel(key);
            bool maxed = upgrades == null || profile == null || level >= UpgradeProfile.MaxLevel;
            bool canUpgrade = enabled && !maxed && upgrades != null && profile != null;

            SetRowLabel(row, label + "   Lv " + level + " / " + UpgradeProfile.MaxLevel, enabled);
            SetPrice(row.priceText, profile == null ? "-" : "$" + profile.CostForNextLevel(level), enabled, !maxed);
            HideButton(row.rightButton);
            ConfigureButton(row.leftButton, row.leftLabel, maxed ? "Max" : (canUpgrade ? "Buy" : "Locked"), NeutralButton,
                canUpgrade ? delegate { upgrades.TryUpgradeFixed(key, profile, label); RefreshPanel(); } : null, canUpgrade);
        }

        void RefreshPurchaseRow(HudRowView row, string label, int cost, bool enabled, Color color, Action onClick,
            string buttonText = "Buy")
        {
            if (row == null) return;
            row.gameObject.SetActive(true);
            SetRowLabel(row, label, enabled);
            SetPrice(row.priceText, "$" + Mathf.Max(0, cost), enabled, true);
            HideButton(row.rightButton);
            ConfigureButton(row.leftButton, row.leftLabel, enabled ? buttonText : "Locked", color,
                enabled ? onClick : null, enabled);
        }

        void RefreshAlcoholSection(HudUpgradesView view, bool unlocked)
        {
            if (view.licenseStatusText != null)
            {
                view.licenseStatusText.gameObject.SetActive(true);
                view.licenseStatusText.text = "Liquor License: " + LicenseStatus(license);
                view.licenseStatusText.color = LicenseStatusColor(unlocked);
            }

            int day = clock == null ? 1 : clock.BusinessDay;
            RefreshPurchaseRow(view.licenseStage1Row, "File Background Check", license == null ? 0 : license.Stage1Fee,
                !unlocked && license != null && license.CanFileStage1, Accent,
                delegate { if (license != null) license.TryFileStage1(economy, day); RefreshPanel(); }, "File");
            RefreshPurchaseRow(view.licenseStage2Row, "File Final Paperwork", license == null ? 0 : license.Stage2Fee,
                !unlocked && license != null && license.CanFileStage2, Accent,
                delegate { if (license != null) license.TryFileStage2(economy, day); RefreshPanel(); }, "File");

            bool canPurchase = !unlocked && (license == null || license.IsApproved);
            RefreshPurchaseRow(view.alcoholSectionPurchaseRow, "Section Purchase",
                sections == null ? 0 : sections.UnlockCost(StoreSectionType.Alcohol), canPurchase, Accent,
                delegate
                {
                    if (progression != null) progression.TryUnlockSection(StoreSectionType.Alcohol);
                    else if (sections != null) sections.TryUnlock(StoreSectionType.Alcohol);
                    RefreshPanel();
                });

            ApplyFixtureGroup(view.alcoholFixtureRows, view.alcoholEmptyText, CollectFixtures(true),
                "No alcohol shelves placed yet.", StoreSectionType.Alcohol, unlocked);
        }

        Color LicenseStatusColor(bool unlocked)
        {
            if (unlocked || (license != null && license.IsApproved)) return Accent;
            if (license != null && license.Stage != LicenseStage.NotStarted) return Yellow;
            return Red;
        }

        /// <summary>Maps the license state machine to the three displayed statuses.</summary>
        public static string LicenseStatus(LiquorLicense license)
        {
            if (license == null || license.Stage == LicenseStage.NotStarted) return "Incomplete";
            if (license.IsApproved) return "Complete";
            return "In Progress";
        }

        /// <summary>Orders total color required by UILayout.md.</summary>
        public static Color TotalCostColor(bool affordable) { return affordable ? Accent : Red; }

        List<KeyValuePair<string, UpgradeableFixture>> CollectFixtures(bool alcohol)
        {
            List<KeyValuePair<string, UpgradeableFixture>> result = new List<KeyValuePair<string, UpgradeableFixture>>();
            if (upgrades == null) return result;

            foreach (KeyValuePair<string, UpgradeableFixture> pair in upgrades.Fixtures)
            {
                if (pair.Value == null || IsAlcoholFixture(pair.Value) != alcohol) continue;
                result.Add(pair);
            }
            return result;
        }

        void ApplyFixtureGroup(HudRowView[] rows, Text emptyText,
            List<KeyValuePair<string, UpgradeableFixture>> fixtures, string emptyMessage,
            StoreSectionType section, bool sectionUnlocked)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(fixtures.Count == 0);
                emptyText.text = emptyMessage;
                emptyText.color = DisabledText;
            }
            if (rows == null) return;

            for (int i = 0; i < rows.Length; i++)
            {
                HudRowView row = rows[i];
                if (row == null) continue;
                if (i >= fixtures.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                KeyValuePair<string, UpgradeableFixture> pair = fixtures[i];
                UpgradeableFixture fixture = pair.Value;
                bool locked = upgrades == null || !sectionUnlocked;
                row.gameObject.SetActive(true);

                string name = fixture.Placed != null && fixture.Placed.Definition != null
                    ? fixture.Placed.Definition.displayName : "Fixture";
                string cost = fixture.profile == null ? "-" : "$" + fixture.profile.CostForNextLevel(fixture.level);
                string detail = locked ? "  - section locked"
                    : "   Lv " + fixture.level + " / 5   " + fixture.stock + "/" + fixture.Capacity + "   next " + cost;
                SetRowLabel(row, name + detail, !locked);
                SetPrice(row.priceText, cost, !locked, true);
                HideButton(row.rightButton);
                ConfigureButton(row.leftButton, row.leftLabel, locked ? "Locked" : "Upgrade", NeutralButton,
                    locked ? null : delegate { fixture.TryUpgrade(economy); RefreshPanel(); }, !locked);
            }
        }

        bool IsSectionUnlocked(StoreSectionType type)
        {
            return sections == null || sections.IsUnlocked(type);
        }

        static bool IsAlcoholFixture(UpgradeableFixture fixture)
        {
            if (fixture == null) return false;
            string id = fixture.key;
            if (fixture.Placed != null && fixture.Placed.Definition != null) id = fixture.Placed.Definition.id;
            return !string.IsNullOrEmpty(id) && id.StartsWith("alcohol", StringComparison.OrdinalIgnoreCase);
        }

        static void SetRowLabel(HudRowView row, string text, bool enabled)
        {
            if (row == null || row.label == null) return;
            row.label.text = text;
            row.label.color = enabled ? Light : DisabledText;
        }

        static void SetPrice(Text price, string text, bool enabled, bool visible)
        {
            if (price == null) return;
            price.gameObject.SetActive(visible);
            price.text = text;
            price.color = enabled ? Light : DisabledText;
        }

        static Text ButtonLabel(Button button)
        {
            if (button == null) return null;
            Transform label = button.transform.Find("Label");
            return label == null ? null : label.GetComponent<Text>();
        }

        static void ShowButton(Button button)
        {
            if (button != null) button.gameObject.SetActive(true);
        }

        static void HideButton(Button button)
        {
            if (button != null) button.gameObject.SetActive(false);
        }

        static void ConfigureButton(Button button, Text label, string text, Color color, Action onClick, bool enabled)
        {
            if (button == null) return;

            button.interactable = enabled;
            Image image = button.targetGraphic as Image;
            if (image == null) image = button.GetComponent<Image>();
            if (image != null) image.color = enabled ? color : Disabled;

            if (label != null)
            {
                label.text = text;
                label.color = enabled ? Color.white : DisabledText;
            }

            button.onClick.RemoveAllListeners();
            if (onClick != null && enabled) button.onClick.AddListener(delegate { onClick(); });
        }

        void OnNotice(string message, NoticeKind kind)
        {
            if (_view == null || _view.noticesContainer == null || _view.toastTemplate == null) return;

            GameObject toast = Instantiate(_view.toastTemplate, _view.noticesContainer);
            toast.SetActive(true);
            RectTransform rect = toast.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -_toasts.Count * 76f);
                rect.sizeDelta = new Vector2(0f, 72f);
            }

            HudToastView toastView = toast.GetComponent<HudToastView>();
            if (toastView != null)
            {
                if (toastView.background != null)
                {
                    toastView.background.color = kind == NoticeKind.Warning ? new Color(0.45f, 0.18f, 0.12f, 0.96f)
                        : kind == NoticeKind.Success ? new Color(0.10f, 0.32f, 0.18f, 0.96f) : Dark;
                }
                if (toastView.message != null) toastView.message.text = message;
            }

            CanvasGroup group = toastView != null ? toastView.group : null;
            if (group == null) group = toast.AddComponent<CanvasGroup>();
            Toast t = new Toast { go = toast, group = group, expiry = Time.unscaledTime + 6f };
            _toasts.Add(t);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetAsLastSibling();
        }
    }
}
