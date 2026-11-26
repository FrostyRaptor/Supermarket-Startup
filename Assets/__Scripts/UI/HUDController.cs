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
    /// Runtime-generated tycoon HUD. Keeps the existing build HUD and adds the
    /// day/rate tab, employee/order/section/upgrade panels and notices.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public GameManager game;
        public GameClock clock;
        public GameWallet economy;
        public NotificationCenter notices;
        public SectionManager sections;
        public EmployeeManager employees;
        public UpgradeSystem upgrades;
        public Inventory inventory;
        public OrderManager orders;
        public SalesProcessor sales;
        public ProgressionManager progression;
        public LiquorLicense license;
        public ProductCatalog catalog;

        enum PanelKind { None, Employees, Upgrades, Orders }

        const int FontSizeBoost = 15;

        static readonly Color Dark = new Color(0.06f, 0.09f, 0.13f, 0.94f);
        static readonly Color Light = new Color(0.91f, 0.94f, 0.97f, 1f);
        static readonly Color Accent = new Color(0.32f, 0.68f, 0.42f, 1f);
        static readonly Color Warn = new Color(0.92f, 0.49f, 0.28f, 1f);
        static readonly Color Disabled = new Color(0.19f, 0.21f, 0.25f, 1f);
        static readonly Color DisabledText = new Color(0.55f, 0.58f, 0.62f, 1f);

        GameObject _root;
        Text _dayText;
        int _lastDay = -1;
        int _lastMinuteOfDay = -1;
        PanelKind _openPanel;
        GameObject _modal;
        RectTransform _modalContent;
        RectTransform _toastContainer;
        readonly List<Toast> _toasts = new List<Toast>();
        readonly Dictionary<string, InputField> _orderInputs = new Dictionary<string, InputField>();
        Font _font;

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
            if (catalog == null) catalog = FindFirstObjectByType<ProductCatalog>();
            if (catalog == null && orders != null) catalog = orders.catalog;
        }

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildUI();
            if (notices != null) notices.NoticeRaised += OnNotice;
            if (sections != null) sections.Changed += OnStateChangedUI;
            if (employees != null) employees.CountsChanged += OnStateChangedUI;
            RefreshStatus();
        }

        void OnDestroy()
        {
            if (notices != null) notices.NoticeRaised -= OnNotice;
            if (sections != null) sections.Changed -= OnStateChangedUI;
            if (employees != null) employees.CountsChanged -= OnStateChangedUI;
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
                if (remain < 1f) t.group.alpha = remain;
            }
        }

        void BuildUI()
        {
            _root = new GameObject("Tycoon HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Top-left day / time tab.
            GameObject status = Panel("Day & Time", _root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 72f));
            _dayText = Label(status.transform, "Day", "", 24, TextAnchor.MiddleLeft, Light,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-2f, -2f), FontStyle.Bold);
            RectTransform dayRect = _dayText.rectTransform;
            dayRect.offsetMin = new Vector2(14f, 4f);
            dayRect.offsetMax = new Vector2(-10f, -4f);

            // Bottom-left store manager button; all management panels live in its tabs.
            MakeHudButton("Store", _root.transform, new Vector2(0f, 0f), new Vector2(24f, 24f),
                OpenStore, new Color(0.18f, 0.36f, 0.58f, 1f));

            // Toast area, under the top-right currency counter.
            GameObject toasts = new GameObject("Notices", typeof(RectTransform));
            toasts.transform.SetParent(_root.transform, false);
            _toastContainer = toasts.GetComponent<RectTransform>();
            _toastContainer.anchorMin = new Vector2(1f, 1f);
            _toastContainer.anchorMax = new Vector2(1f, 1f);
            _toastContainer.pivot = new Vector2(1f, 1f);
            _toastContainer.anchoredPosition = new Vector2(-24f, -184f);
            _toastContainer.sizeDelta = new Vector2(480f, 520f);
        }

        void RefreshStatus()
        {
            if (_dayText == null) return;

            int day = clock == null ? 1 : clock.Day;
            int minuteOfDay = Mathf.FloorToInt((clock == null ? 0f : clock.DayProgress) * 24f * 60f);
            if (day == _lastDay && minuteOfDay == _lastMinuteOfDay) return;

            _lastDay = day;
            _lastMinuteOfDay = minuteOfDay;
            string time = clock == null ? "12:00 AM" : clock.TimeText;
            _dayText.text = "Day " + day + "   " + time;
        }

        void OpenStore() { ShowPanel(PanelKind.Employees); }

        void ShowPanel(PanelKind kind)
        {
            _openPanel = kind;
            if (_modal != null) Destroy(_modal);
            _orderInputs.Clear();

            _modal = new GameObject("Modal", typeof(RectTransform), typeof(Image));
            _modal.transform.SetParent(_root.transform, false);
            RectTransform modalRect = _modal.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            Image dim = _modal.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);

            GameObject window = Panel("Window", _modal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 860f));
            GameObject header = Panel("Header", window.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0f, 84f));
            Label(header.transform, "Title", "Store Manager", 26, TextAnchor.MiddleLeft, Light,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-100f, 0f), FontStyle.Bold);
            MakeWindowButton("X", header.transform, new Vector2(1f, 0.5f), new Vector2(-42f, 0f), ClosePanel, new Vector2(64f, 58f), Warn);

            GameObject tabs = Panel("Tabs", window.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(0f, 58f));
            MakeWindowButton("Employees", tabs.transform, new Vector2(0.5f, 0.5f), new Vector2(-350f, 0f),
                delegate { ShowPanel(PanelKind.Employees); }, new Vector2(320f, 48f),
                kind == PanelKind.Employees ? Accent : new Color(0.24f, 0.28f, 0.34f, 1f));
            MakeWindowButton("Upgrades", tabs.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                delegate { ShowPanel(PanelKind.Upgrades); }, new Vector2(320f, 48f),
                kind == PanelKind.Upgrades ? Accent : new Color(0.24f, 0.28f, 0.34f, 1f));
            MakeWindowButton("Orders", tabs.transform, new Vector2(0.5f, 0.5f), new Vector2(350f, 0f),
                delegate { ShowPanel(PanelKind.Orders); }, new Vector2(320f, 48f),
                kind == PanelKind.Orders ? Accent : new Color(0.24f, 0.28f, 0.34f, 1f));

            GameObject body = Panel("Body", window.transform, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.offsetMin = new Vector2(20f, 20f);
            bodyRect.offsetMax = new Vector2(-20f, -150f);
            body.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 0.55f);
            body.AddComponent<RectMask2D>();

            ScrollRect scroll = body.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = bodyRect;
            _modalContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _modalContent.SetParent(body.transform, false);
            _modalContent.anchorMin = new Vector2(0f, 1f);
            _modalContent.anchorMax = new Vector2(1f, 1f);
            _modalContent.pivot = new Vector2(0.5f, 1f);
            _modalContent.anchoredPosition = Vector2.zero;
            _modalContent.sizeDelta = new Vector2(0f, 3200f);
            scroll.content = _modalContent;

            switch (kind)
            {
                case PanelKind.Employees: BuildEmployeesPanel(); break;
                case PanelKind.Upgrades: BuildUpgradesPanel(); break;
                case PanelKind.Orders: BuildOrdersPanel(); break;
            }

            scroll.verticalNormalizedPosition = 1f;
        }

        void ClosePanel()
        {
            _openPanel = PanelKind.None;
            if (_modal != null) Destroy(_modal);
            _modal = null;
        }

        void BuildEmployeesPanel()
        {
            if (employees == null) return;
            float y = -10f;
            AddEmployeeGroup(ref y, "Checkout & Sales", EmployeeRole.Checkout);
            AddEmployeeGroup(ref y, "Storage", EmployeeRole.Unloader, EmployeeRole.Stocker);
            AddEmployeeGroup(ref y, "Butcher", EmployeeRole.Butcher);
            AddEmployeeGroup(ref y, "Bakery", EmployeeRole.Bakery);
            AddEmployeeGroup(ref y, "Deli", EmployeeRole.Deli);

            Label(_modalContent, "Wages", "Daily wages: $" + employees.DailyWages, 22, TextAnchor.MiddleLeft, Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 28f), new Vector2(0f, 56f), FontStyle.Bold);
        }

        void AddEmployeeGroup(ref float y, string heading, params EmployeeRole[] roles)
        {
            Heading(ref y, heading);
            if (roles == null) return;
            for (int i = 0; i < roles.Length; i++)
            {
                EmployeeRole role = roles[i];
                int max = employees.Max(role);
                string label = role + "   " + employees.Count(role) + " / " + max + "   $" + Wage(role) + "/day";
                Row(y, label,
                    delegate { employees.TryFire(role); RefreshPanel(); },
                    delegate { employees.TryHire(role); RefreshPanel(); },
                    "-", "+");
                y -= 86f;
            }
        }

        int Wage(EmployeeRole role)
        {
            EmployeeConfig cfg = FindFirstObjectByType<EmployeeConfig>();
            return cfg == null ? 0 : cfg.WageFor(role);
        }

        void BuildOrdersPanel()
        {
            if (catalog == null || orders == null || inventory == null) return;
            float y = -6f;
            for (int i = 0; i < catalog.products.Count; i++)
            {
                ProductInfo p = catalog.products[i];
                if (p == null) continue;
                int suggestion = orders.SuggestQuantity(p.id);

                GameObject row = Panel("Product " + p.id, _modalContent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 62f));
                Label(row.transform, "Name", p.displayName + "  (suggest " + suggestion + ")  $" + p.unitCost.ToString("0.00") + " each",
                    18, TextAnchor.MiddleLeft, Light, Vector2.zero, new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero, FontStyle.Normal);

                GameObject inputObj = new GameObject("Qty", typeof(RectTransform), typeof(Image), typeof(InputField));
                inputObj.transform.SetParent(row.transform, false);
                RectTransform inputRect = inputObj.GetComponent<RectTransform>();
                inputRect.anchorMin = new Vector2(0.64f, 0.10f);
                inputRect.anchorMax = new Vector2(0.78f, 0.90f);
                inputRect.offsetMin = Vector2.zero;
                inputRect.offsetMax = Vector2.zero;
                inputObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.94f);
                InputField field = inputObj.GetComponent<InputField>();
                field.textComponent = Label(inputObj.transform, "Value", suggestion.ToString(), 18, TextAnchor.MiddleCenter,
                    new Color(0.1f, 0.1f, 0.1f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Normal);
                field.text = suggestion.ToString();
                field.contentType = InputField.ContentType.IntegerNumber;
                _orderInputs[p.id] = field;

                string id = p.id;
                MakeWindowButton("Order", row.transform, new Vector2(0.92f, 0.5f), Vector2.zero,
                    delegate { PlaceOrder(id); }, new Vector2(150f, 54f), Accent);
                y -= 70f;
            }
        }

        void PlaceOrder(string productId)
        {
            InputField field;
            int qty = 0;
            if (_orderInputs.TryGetValue(productId, out field)) int.TryParse(field.text, out qty);
            if (qty > 0 && orders.TryPlaceOrder(productId, qty)) RefreshPanel();
        }

        void BuildSectionAccess(ref float y)
        {
            Heading(ref y, "Section Access");
            StoreSectionType[] lockable =
            {
                StoreSectionType.Butcher, StoreSectionType.Bakery, StoreSectionType.Deli,
                StoreSectionType.Checkout, StoreSectionType.Alcohol
            };
            for (int i = 0; i < lockable.Length; i++)
            {
                StoreSectionType type = lockable[i];
                bool unlocked = IsSectionUnlocked(type);
                string label = type + (unlocked ? "  - owned" : "  - unlock $" + (sections == null ? 0 : sections.UnlockCost(type)));
                GameObject row = Panel("Section " + type, _modalContent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 58f));
                Label(row.transform, "Label", label, 18, TextAnchor.MiddleLeft, unlocked ? Light : DisabledText,
                    Vector2.zero, new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero, FontStyle.Normal);
                if (unlocked)
                {
                    MakeWindowButton("Owned", row.transform, new Vector2(0.88f, 0.5f), Vector2.zero, null,
                        new Vector2(160f, 52f), Disabled, false);
                }
                else
                {
                    StoreSectionType captured = type;
                    MakeWindowButton("Unlock", row.transform, new Vector2(0.88f, 0.5f), Vector2.zero,
                        delegate { if (progression != null) progression.TryUnlockSection(captured); RefreshPanel(); },
                        new Vector2(160f, 52f), Accent);
                }
                y -= 66f;
            }

            if (sections != null)
            {
                Label(_modalContent, "Expand", "Checkout lanes placed: " + sections.CheckoutLanes +
                    " (build mode B)     Loading zones: " + sections.LoadingZones + " / " + sections.MaxLoadingZones, 18,
                    TextAnchor.MiddleLeft, Light, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 26f), new Vector2(0f, 52f), FontStyle.Normal);
                y -= 56f;
                Row(y, "Add loading zone  $300",
                    delegate { sections.TryExpandLoadingZone(); RefreshPanel(); }, null, "Buy", "+");
                y -= 86f;
            }

            if (license != null)
            {
                Label(_modalContent, "License", "Liquor license: " + license.StatusText, 20, TextAnchor.MiddleLeft,
                    license.IsApproved ? Accent : Warn, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, y - 26f), new Vector2(0f, 52f), FontStyle.Bold);
                y -= 56f;
                if (license.CanFileStage1)
                {
                    Row(y, "File background check  $" + license.Stage1Fee,
                        delegate { license.TryFileStage1(economy, clock == null ? 1 : clock.BusinessDay); RefreshPanel(); },
                        null, "File", "+");
                    y -= 86f;
                }
                if (license.CanFileStage2)
                {
                    Row(y, "File final paperwork  $" + license.Stage2Fee,
                        delegate { license.TryFileStage2(economy, clock == null ? 1 : clock.BusinessDay); RefreshPanel(); },
                        null, "File", "+");
                    y -= 86f;
                }
            }
        }

        void BuildUpgradesPanel()
        {
            float y = -10f;
            BuildSectionAccess(ref y);
            Heading(ref y, "Upgrade Paths");

            SectionHeading(ref y, "Checkout & Sales", StoreSectionType.Checkout);
            AddUpgradeRow(ref y, "Checkout lane 1", UpgradeSystem.CheckoutKeyPrefix + "0",
                upgrades == null ? null : upgrades.LaneProfile, StoreSectionType.Checkout);

            SectionHeading(ref y, "Storage", StoreSectionType.Storage);
            AddUpgradeRow(ref y, "Loading zone 1", UpgradeSystem.LoadingZoneKeyPrefix + "0",
                upgrades == null ? null : upgrades.LoadingZoneProfile, StoreSectionType.Storage);
            AddUpgradeRow(ref y, "Storage shelf 1", UpgradeSystem.StorageShelfKeyPrefix + "0",
                upgrades == null ? null : upgrades.StorageShelfProfile, StoreSectionType.Storage);

            SectionHeading(ref y, "Butcher", StoreSectionType.Butcher);
            AddUpgradeRow(ref y, "Butcher equipment", UpgradeSystem.ButcherKey,
                upgrades == null ? null : upgrades.ButcherProfile, StoreSectionType.Butcher);

            SectionHeading(ref y, "Bakery", StoreSectionType.Bakery);
            AddUpgradeRow(ref y, "Bakery equipment", UpgradeSystem.BakeryKey,
                upgrades == null ? null : upgrades.BakeryProfile, StoreSectionType.Bakery);

            SectionHeading(ref y, "Deli", StoreSectionType.Deli);
            AddUpgradeRow(ref y, "Deli equipment", UpgradeSystem.DeliKey,
                upgrades == null ? null : upgrades.DeliProfile, StoreSectionType.Deli);

            SectionHeading(ref y, "General Shelves & Fixtures", StoreSectionType.General);
            AddFixtureGroup(ref y, false);

            SectionHeading(ref y, "Alcohol Shelves", StoreSectionType.Alcohol);
            AddFixtureGroup(ref y, true);
        }

        void AddFixtureGroup(ref float y, bool alcohol)
        {
            if (upgrades == null) return;
            bool found = false;
            foreach (KeyValuePair<string, UpgradeableFixture> pair in upgrades.Fixtures)
            {
                UpgradeableFixture f = pair.Value;
                if (f == null || IsAlcoholFixture(f) != alcohol) continue;
                found = true;
                string key = pair.Key;
                string name = f.Placed != null && f.Placed.Definition != null ? f.Placed.Definition.displayName : "Fixture";
                AddFixtureRow(ref y, name, key, f, alcohol ? StoreSectionType.Alcohol : StoreSectionType.General);
            }
            if (!found)
            {
                Label(_modalContent, "Empty", alcohol ? "No alcohol shelves placed yet." : "No shelves or fixtures placed yet.",
                    18, TextAnchor.MiddleLeft, DisabledText, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, y - 26f), new Vector2(0f, 52f), FontStyle.Normal);
                y -= 56f;
            }
        }

        void AddUpgradeRow(ref float y, string label, string key, UpgradeProfile profile, StoreSectionType section)
        {
            bool locked = !IsSectionUnlocked(section);
            int level = upgrades == null ? 1 : upgrades.GetLevel(key);
            string cost = profile == null ? "-" : "$" + profile.CostForNextLevel(level);
            string detail = locked ? "  - section locked" : "   Lv " + level + " / 5   next " + cost;
            Row(y, label + detail,
                locked ? (Action)null : delegate { upgrades.TryUpgradeFixed(key, profile, label); RefreshPanel(); },
                null, locked ? "Locked" : "Upgrade", "+", locked);
            y -= 86f;
        }

        void AddFixtureRow(ref float y, string label, string key, UpgradeableFixture fixture, StoreSectionType section)
        {
            bool locked = !IsSectionUnlocked(section);
            string cost = fixture.profile == null ? "-" : "$" + fixture.profile.CostForNextLevel(fixture.level);
            string detail = locked ? "  - section locked" : "   Lv " + fixture.level + " / 5   " + fixture.stock + "/" + fixture.Capacity + "   next " + cost;
            Row(y, label + detail,
                locked ? (Action)null : delegate { fixture.TryUpgrade(economy); RefreshPanel(); },
                null, locked ? "Locked" : "Upgrade", "+", locked);
            y -= 86f;
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

        void Heading(ref float y, string title)
        {
            Heading(ref y, title, Accent);
        }

        void Heading(ref float y, string title, Color color)
        {
            Label(_modalContent, "Heading", title, 20, TextAnchor.MiddleLeft, color,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 20f), new Vector2(0f, 40f), FontStyle.Bold);
            y -= 42f;
        }

        void SectionHeading(ref float y, string title, StoreSectionType type)
        {
            bool unlocked = IsSectionUnlocked(type);
            Heading(ref y, title + (unlocked ? "" : "  (locked)"), unlocked ? Accent : DisabledText);
        }

        void Row(float y, string label, Action left, Action right, string leftText, string rightText, bool locked = false)
        {
            GameObject row = Panel("Row", _modalContent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 78f));
            Label(row.transform, "Label", label, 18, TextAnchor.MiddleLeft, locked ? DisabledText : Light,
                Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, FontStyle.Normal);
            if (left != null)
                MakeWindowButton(leftText, row.transform, new Vector2(0.68f, 0.5f), Vector2.zero, left,
                    new Vector2(140f, 54f), locked ? Disabled : new Color(0.3f, 0.33f, 0.38f, 1f), !locked);
            else if (locked)
                MakeWindowButton(leftText, row.transform, new Vector2(0.68f, 0.5f), Vector2.zero, null,
                    new Vector2(140f, 54f), Disabled, false);
            if (right != null)
                MakeWindowButton(rightText, row.transform, new Vector2(0.88f, 0.5f), Vector2.zero, right,
                    new Vector2(140f, 54f), Accent, !locked);
        }

        void RefreshPanel()
        {
            if (_openPanel == PanelKind.None) return;
            ShowPanel(_openPanel);
        }

        void OnNotice(string message, NoticeKind kind)
        {
            if (_toastContainer == null) return;
            GameObject toast = Panel("Toast", _toastContainer, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -_toasts.Count * 76f), new Vector2(0f, 72f));
            Image image = toast.GetComponent<Image>();
            image.color = kind == NoticeKind.Warning ? new Color(0.45f, 0.18f, 0.12f, 0.96f)
                : kind == NoticeKind.Success ? new Color(0.10f, 0.32f, 0.18f, 0.96f) : Dark;
            Label(toast.transform, "Message", message, 16, TextAnchor.MiddleLeft, Light,
                Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f), FontStyle.Normal);
            Toast t = new Toast { go = toast, group = toast.AddComponent<CanvasGroup>(), expiry = Time.unscaledTime + 6f };
            _toasts.Add(t);
        }

        GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = Dark;
            image.raycastTarget = true;
            return go;
        }

        Text Label(Transform parent, string name, string content, int size, TextAnchor anchor, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta, FontStyle style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;
            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size + FontSizeBoost;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        void MakeHudButton(string label, Transform parent, Vector2 anchor, Vector2 position, Action onClick, Color color)
        {
            MakeButton(label, parent, anchor, position, onClick, new Vector2(190f, 72f), color);
        }

        Button MakeWindowButton(string label, Transform parent, Vector2 anchor, Vector2 position, Action onClick,
            Vector2 size, Color color, bool enabled = true)
        {
            return MakeButton(label, parent, anchor, position, onClick, size, color, enabled);
        }

        Button MakeButton(string label, Transform parent, Vector2 anchor, Vector2 position, Action onClick,
            Vector2 size, Color color, bool enabled = true)
        {
            GameObject go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(
                anchor.x > 0.5f ? 1f : anchor.x < 0.5f ? 0f : 0.5f,
                anchor.y > 0.5f ? 1f : anchor.y < 0.5f ? 0f : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = enabled ? color : Disabled;
            Button button = go.GetComponent<Button>();
            button.interactable = enabled;
            ColorBlock block = button.colors;
            block.normalColor = Color.white;
            block.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            block.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = block;
            if (onClick != null) button.onClick.AddListener(delegate { onClick(); });
            Label(go.transform, "Label", label, 19, TextAnchor.MiddleCenter,
                enabled ? Color.white : DisabledText, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            return button;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetAsLastSibling();
        }
    }
}
