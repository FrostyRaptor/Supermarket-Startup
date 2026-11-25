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

        enum PanelKind { None, Employees, Orders, Sections, Upgrades }

        static readonly Color Dark = new Color(0.06f, 0.09f, 0.13f, 0.94f);
        static readonly Color Light = new Color(0.91f, 0.94f, 0.97f, 1f);
        static readonly Color Accent = new Color(0.32f, 0.68f, 0.42f, 1f);
        static readonly Color Warn = new Color(0.92f, 0.49f, 0.28f, 1f);

        GameObject _root;
        Text _dayText;
        Text _rateText;
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

            // Top-left day / rate tab.
            GameObject status = Panel("Day & Rate", _root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(320f, 96f));
            _dayText = Label(status.transform, "Day", "", 24, TextAnchor.MiddleLeft, Light,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-2f, -2f), FontStyle.Bold);
            _rateText = Label(status.transform, "Rate", "", 17, TextAnchor.MiddleLeft, Accent,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-2f, -2f), FontStyle.Normal);
            RectTransform dayRect = _dayText.rectTransform;
            dayRect.offsetMin = new Vector2(14f, 46f);
            dayRect.offsetMax = new Vector2(-10f, -2f);
            RectTransform rateRect = _rateText.rectTransform;
            rateRect.offsetMin = new Vector2(14f, 8f);
            rateRect.offsetMax = new Vector2(-10f, -42f);

            // Bottom-left employees button.
            MakeHudButton("Employees", _root.transform, new Vector2(0f, 0f), new Vector2(24f, 24f),
                OpenEmployees, new Color(0.18f, 0.36f, 0.58f, 1f));

            // Above the build button: order manager plus section/upgrade access.
            MakeHudButton("Orders", _root.transform, new Vector2(1f, 0f), new Vector2(-24f, 96f), OpenOrders, Accent);
            MakeHudButton("Sections", _root.transform, new Vector2(1f, 0f), new Vector2(-214f, 96f), OpenSections,
                new Color(0.42f, 0.35f, 0.62f, 1f));
            MakeHudButton("Upgrades", _root.transform, new Vector2(1f, 0f), new Vector2(-404f, 96f), OpenUpgrades,
                new Color(0.55f, 0.42f, 0.24f, 1f));

            // Toast area, under the top-right currency counter.
            GameObject toasts = new GameObject("Notices", typeof(RectTransform));
            toasts.transform.SetParent(_root.transform, false);
            _toastContainer = toasts.GetComponent<RectTransform>();
            _toastContainer.anchorMin = new Vector2(1f, 1f);
            _toastContainer.anchorMax = new Vector2(1f, 1f);
            _toastContainer.pivot = new Vector2(1f, 1f);
            _toastContainer.anchoredPosition = new Vector2(-24f, -100f);
            _toastContainer.sizeDelta = new Vector2(420f, 520f);
        }

        void RefreshStatus()
        {
            if (_dayText == null) return;
            string date = clock == null ? "01/01/2026" : clock.DateText;
            int day = clock == null ? 1 : clock.Day;
            _dayText.text = "Day " + day + "   " + date;

            if (economy == null) { _rateText.text = "Net: $0.0 / real min"; return; }
            float rate = economy.NetPerRealMinute;
            _rateText.text = "Net: " + (rate >= 0f ? "+" : "-") + "$" + Mathf.Abs(rate).ToString("0.0") + " / real min";
            _rateText.color = rate >= 0f ? Accent : Warn;
        }

        void OpenEmployees() { ShowPanel(PanelKind.Employees); }
        void OpenOrders() { ShowPanel(PanelKind.Orders); }
        void OpenSections() { ShowPanel(PanelKind.Sections); }
        void OpenUpgrades() { ShowPanel(PanelKind.Upgrades); }

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
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 700f));
            GameObject header = Panel("Header", window.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(0f, 68f));
            Label(header.transform, "Title", Title(kind), 26, TextAnchor.MiddleLeft, Light,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-100f, 0f), FontStyle.Bold);
            MakeWindowButton("X", header.transform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), ClosePanel, new Vector2(52f, 44f), Warn);

            GameObject body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(window.transform, false);
            _modalContent = body.GetComponent<RectTransform>();
            _modalContent.anchorMin = new Vector2(0f, 0f);
            _modalContent.anchorMax = new Vector2(1f, 1f);
            _modalContent.offsetMin = new Vector2(20f, 20f);
            _modalContent.offsetMax = new Vector2(-20f, -80f);

            switch (kind)
            {
                case PanelKind.Employees: BuildEmployeesPanel(); break;
                case PanelKind.Orders: BuildOrdersPanel(); break;
                case PanelKind.Sections: BuildSectionsPanel(); break;
                case PanelKind.Upgrades: BuildUpgradesPanel(); break;
            }
        }

        string Title(PanelKind kind)
        {
            switch (kind)
            {
                case PanelKind.Employees: return "Employees";
                case PanelKind.Orders: return "Order Manager";
                case PanelKind.Sections: return "Sections & License";
                case PanelKind.Upgrades: return "Upgrades";
                default: return "";
            }
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
            EmployeeRole[] roles =
            {
                EmployeeRole.Checkout, EmployeeRole.Butcher, EmployeeRole.Bakery,
                EmployeeRole.Deli, EmployeeRole.Unloader, EmployeeRole.Stocker
            };
            float y = -10f;
            for (int i = 0; i < roles.Length; i++)
            {
                EmployeeRole role = roles[i];
                int max = employees.Max(role);
                string label = role + "   " + employees.Count(role) + " / " + max + "   $" + Wage(role) + "/day";
                Row(y, label,
                    delegate { employees.TryFire(role); RefreshPanel(); },
                    delegate { employees.TryHire(role); RefreshPanel(); },
                    "-", "+");
                y -= 76f;
            }
            Label(_modalContent, "Wages", "Daily wages: $" + employees.DailyWages, 22, TextAnchor.MiddleLeft, Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(0f, 44f), FontStyle.Bold);
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
                inputRect.anchorMin = new Vector2(0.64f, 0.18f);
                inputRect.anchorMax = new Vector2(0.78f, 0.82f);
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
                    delegate { PlaceOrder(id); }, new Vector2(130f, 44f), Accent);
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

        void BuildSectionsPanel()
        {
            float y = -8f;
            StoreSectionType[] lockable =
            {
                StoreSectionType.Butcher, StoreSectionType.Bakery, StoreSectionType.Deli,
                StoreSectionType.Checkout, StoreSectionType.Alcohol
            };
            for (int i = 0; i < lockable.Length; i++)
            {
                StoreSectionType type = lockable[i];
                bool unlocked = sections != null && sections.IsUnlocked(type);
                string label = type + (unlocked ? "  - unlocked" : "  - unlock $" + (sections == null ? 0 : sections.UnlockCost(type)));
                GameObject row = Panel("Section " + type, _modalContent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 58f));
                Label(row.transform, "Label", label, 18, TextAnchor.MiddleLeft, Light,
                    Vector2.zero, new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero, FontStyle.Normal);
                if (!unlocked)
                {
                    StoreSectionType captured = type;
                    MakeWindowButton("Unlock", row.transform, new Vector2(0.88f, 0.5f), Vector2.zero,
                        delegate { if (progression != null) progression.TryUnlockSection(captured); RefreshPanel(); },
                        new Vector2(150f, 42f), Accent);
                }
                y -= 66f;
            }

            if (sections != null)
            {
                Label(_modalContent, "Expand", "Checkout lanes placed: " + sections.CheckoutLanes +
                    " (build mode B)     Loading zones: " + sections.LoadingZones + " / " + sections.MaxLoadingZones, 18,
                    TextAnchor.MiddleLeft, Light, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(0f, 44f), FontStyle.Normal);
                y -= 48f;
                Row(y, "Add loading zone  $" + (sections == null ? 0 : 300),
                    delegate { sections.TryExpandLoadingZone(); RefreshPanel(); }, null, "Buy", "+");
                y -= 70f;
            }

            if (license != null)
            {
                Label(_modalContent, "License", "Liquor license: " + license.StatusText, 20, TextAnchor.MiddleLeft,
                    license.IsApproved ? Accent : Warn, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, y), new Vector2(0f, 44f), FontStyle.Bold);
                y -= 48f;
                if (license.CanFileStage1)
                {
                    Row(y, "File background check  $" + license.Stage1Fee,
                        delegate { license.TryFileStage1(economy, clock == null ? 1 : clock.BusinessDay); RefreshPanel(); },
                        null, "File", "+");
                    y -= 62f;
                }
                if (license.CanFileStage2)
                {
                    Row(y, "File final paperwork  $" + license.Stage2Fee,
                        delegate { license.TryFileStage2(economy, clock == null ? 1 : clock.BusinessDay); RefreshPanel(); },
                        null, "File", "+");
                }
            }
        }

        void BuildUpgradesPanel()
        {
            if (upgrades == null) return;
            float y = -8f;
            AddUpgradeRow(ref y, "Checkout lane 1", UpgradeSystem.CheckoutKeyPrefix + "0", upgrades.LaneProfile);
            AddUpgradeRow(ref y, "Loading zone 1", UpgradeSystem.LoadingZoneKeyPrefix + "0", upgrades.LoadingZoneProfile);
            AddUpgradeRow(ref y, "Storage shelf 1", UpgradeSystem.StorageShelfKeyPrefix + "0", upgrades.StorageShelfProfile);
            AddUpgradeRow(ref y, "Butcher equipment", UpgradeSystem.ButcherKey, upgrades.ButcherProfile);
            AddUpgradeRow(ref y, "Bakery equipment", UpgradeSystem.BakeryKey, upgrades.BakeryProfile);
            AddUpgradeRow(ref y, "Deli equipment", UpgradeSystem.DeliKey, upgrades.DeliProfile);

            foreach (KeyValuePair<string, UpgradeableFixture> pair in upgrades.Fixtures)
            {
                UpgradeableFixture f = pair.Value;
                if (f == null) continue;
                string key = pair.Key;
                string name = f.Placed != null && f.Placed.Definition != null ? f.Placed.Definition.displayName : "Fixture";
                AddFixtureRow(ref y, name, key, f);
            }
        }

        void AddUpgradeRow(ref float y, string label, string key, UpgradeProfile profile)
        {
            int level = upgrades.GetLevel(key);
            string cost = profile == null ? "-" : "$" + profile.CostForNextLevel(level);
            Row(y, label + "   Lv " + level + " / 5   next " + cost,
                delegate { upgrades.TryUpgradeFixed(key, profile, label); RefreshPanel(); }, null, "Upgrade", "+");
            y -= 64f;
        }

        void AddFixtureRow(ref float y, string label, string key, UpgradeableFixture fixture)
        {
            string cost = fixture.profile == null ? "-" : "$" + fixture.profile.CostForNextLevel(fixture.level);
            Row(y, label + "   Lv " + fixture.level + " / 5   " + fixture.stock + "/" + fixture.Capacity + "   next " + cost,
                delegate { fixture.TryUpgrade(economy); RefreshPanel(); }, null, "Upgrade", "+");
            y -= 64f;
        }

        void Row(float y, string label, Action left, Action right, string leftText, string rightText)
        {
            GameObject row = Panel("Row", _modalContent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 58f));
            Label(row.transform, "Label", label, 18, TextAnchor.MiddleLeft, Light,
                Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, FontStyle.Normal);
            if (left != null)
                MakeWindowButton(leftText, row.transform, new Vector2(0.68f, 0.5f), Vector2.zero, left, new Vector2(120f, 42f), new Color(0.3f, 0.33f, 0.38f, 1f));
            if (right != null)
                MakeWindowButton(rightText, row.transform, new Vector2(0.88f, 0.5f), Vector2.zero, right, new Vector2(120f, 42f), Accent);
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
                new Vector2(0.5f, 1f), new Vector2(0f, -_toasts.Count * 68f), new Vector2(0f, 60f));
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
            text.fontSize = size;
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
            MakeButton(label, parent, anchor, position, onClick, new Vector2(170f, 58f), color);
        }

        void MakeWindowButton(string label, Transform parent, Vector2 anchor, Vector2 position, Action onClick,
            Vector2 size, Color color)
        {
            MakeButton(label, parent, anchor, position, onClick, size, color);
        }

        void MakeButton(string label, Transform parent, Vector2 anchor, Vector2 position, Action onClick,
            Vector2 size, Color color)
        {
            GameObject go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor.x > 0.5f ? new Vector2(1f, 0f) : anchor.x < 0.5f ? new Vector2(0f, 0f) : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            Button button = go.GetComponent<Button>();
            ColorBlock block = button.colors;
            block.normalColor = Color.white;
            block.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            block.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = block;
            button.onClick.AddListener(delegate { onClick(); });
            Label(go.transform, "Label", label, 19, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetAsLastSibling();
        }
    }
}
