using Supermarket.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Supermarket.EditorTools
{
    /// <summary>
    /// Creates missing uGUI Canvas prefabs and validates the authored HUD prefab.
    /// The HUD prefab is the editable source of truth: its tab bodies are normal
    /// Canvas GameObjects that designers can move, restyle, add to, and wire in
    /// the Unity Inspector. This utility never rebuilds or overwrites it.
    /// </summary>
    public static class AuthoredUICreator
    {
        public const string HudPrefabPath = "Assets/_Prefabs/UI/TycoonHUD.prefab";
        public const string BuildMenuPrefabPath = "Assets/_Prefabs/UI/BuildMenu.prefab";

        static readonly Color CurrencyOutline = new Color(0.29f, 0.86f, 0.44f, 1f);
        static readonly Color BillPaper = new Color(0.84f, 0.94f, 0.79f, 1f);
        static readonly Color BillInset = new Color(0.22f, 0.56f, 0.33f, 0.55f);
        static readonly Color BillMark = new Color(1f, 1f, 1f, 0.95f);

        static readonly Color NormalColor = new Color(0.16f, 0.18f, 0.22f, 1f);
        static readonly Color SelectedColor = new Color(0.18f, 0.52f, 0.9f, 1f);
        static readonly Color HoverColor = new Color(0.24f, 0.27f, 0.33f, 1f);

        static readonly Color BuildNormal = new Color(0.96f, 0.62f, 0.13f, 1f);
        static readonly Color BuildHover = new Color(1f, 0.73f, 0.26f, 1f);
        static readonly Color BuildPressed = new Color(0.84f, 0.51f, 0.09f, 1f);
        static readonly Color BuildLabel = new Color(0.15f, 0.09f, 0.02f, 1f);

        [MenuItem("Tools/Supermarket/UI/Create Authored Canvas Prefabs")]
        public static void CreatePrefabsMenu()
        {
            CreatePrefabs(false);
        }

        /// <summary>Creates missing prefabs and validates the authored HUD prefab.</summary>
        public static void CreatePrefabs(bool overwrite = false)
        {
            EnsureFolder("Assets/_Prefabs");
            EnsureFolder("Assets/_Prefabs/UI");
            ValidateHudPrefab();
            CreateBuildMenuPrefab(overwrite);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void ValidateHudPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("TycoonHUD.prefab is missing. Author the Canvas and tab bodies in the Unity Editor.", prefab);
                return;
            }

            HUDView view = prefab.GetComponent<HUDView>();
            if (view == null)
            {
                Debug.LogError("TycoonHUD.prefab is missing its HUDView component.", prefab);
                return;
            }

            if (view.employeesPanel == null || view.upgradesPanel == null || view.ordersPanel == null)
            {
                Debug.LogError("TycoonHUD.prefab is missing one or more static tab body GameObjects.", prefab);
                return;
            }

            Debug.Log("TycoonHUD.prefab retained. Edit its Canvas hierarchy directly; tab body visibility is driven by HUDController.", prefab);
        }

        static void CreateBuildMenuPrefab(bool overwrite)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(BuildMenuPrefabPath) != null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = new GameObject("Build Menu", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BuildMenuView));

            ConfigureCanvas(root, 100);
            BuildMenuView view = root.GetComponent<BuildMenuView>();

            GameObject bar = CreatePanel("Build Bar", root.transform, new Color(0.055f, 0.065f, 0.085f, 0.94f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(-32f, 370f));
            VerticalLayoutGroup vertical = bar.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(14, 14, 10, 10);
            vertical.spacing = 6f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            view.buildBar = bar;

            GameObject hintsRow = CreateLayoutRow("HintsRow", bar.transform, 38f, 10f);
            Text hintsText = CreateText("Hints", hintsRow.transform,
                "B close  ·  LMB place  ·  RMB remove  ·  R rotate  ·  Esc cancel", 32, TextAnchor.MiddleLeft,
                Color.white, FontStyle.Normal, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            LayoutElement hintsLayout = hintsText.gameObject.AddComponent<LayoutElement>();
            hintsLayout.flexibleWidth = 1f;
            hintsLayout.preferredHeight = 38f;
            view.hintsText = hintsText;

            GameObject messageRow = CreateLayoutRow("MessageRow", bar.transform, 38f, 0f);
            Text messageText = CreateText("Message", messageRow.transform, "", 33, TextAnchor.MiddleCenter,
                Color.white, FontStyle.Normal, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            LayoutElement messageLayout = messageText.gameObject.AddComponent<LayoutElement>();
            messageLayout.flexibleWidth = 1f;
            messageLayout.preferredHeight = 38f;
            CanvasGroup messageGroup = messageText.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
            view.messageText = messageText;
            view.messageGroup = messageGroup;

            GameObject itemsRow = CreateLayoutRow("Items", bar.transform, 160f, 8f);
            view.hotbarContainer = itemsRow.GetComponent<RectTransform>();

            GameObject actionsRow = CreateLayoutRow("Actions", bar.transform, 50f, 8f);
            GameObject rotateButton = CreateButton("Rotate", actionsRow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(170f, 50f), Color.white, BuildButtonColors(), "Rotate (R)", 32, Color.white);
            AddLayoutSize(rotateButton, 170f, 50f);
            GameObject closeButton = CreateButton("Close", actionsRow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(170f, 50f), Color.white, BuildButtonColors(), "Close (B)", 32, Color.white);
            AddLayoutSize(closeButton, 170f, 50f);
            view.rotateButton = rotateButton.GetComponent<Button>();
            view.closeButton = closeButton.GetComponent<Button>();

            GameObject currency = Node("Currency Counter", root.transform);
            SetRect(currency, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(312f, 140f));
            AddBorder(currency.transform, CurrencyOutline);

            GameObject bill = CreatePanel("Dollar Bill", currency.transform, BillPaper,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(6f, -8f), new Vector2(54f, 42f), false);
            GameObject inset = CreatePanel("Bill Inset", bill.transform, BillInset,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
            RectTransform insetRect = inset.GetComponent<RectTransform>();
            insetRect.offsetMin = new Vector2(3f, 3f);
            insetRect.offsetMax = new Vector2(-3f, -3f);
            CreateText("Bill Mark", bill.transform, "$", 34, TextAnchor.MiddleCenter, BillMark,
                FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text amount = CreateText("Amount", currency.transform, "$0", 39, TextAnchor.MiddleCenter, Color.white,
                FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(66f, 80f), new Vector2(-4f, -6f));
            Shadow amountShadow = amount.gameObject.AddComponent<Shadow>();
            amountShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            amountShadow.effectDistance = new Vector2(1f, -1f);
            view.currencyText = amount;

            view.rateText = CreateText("Net Rate", currency.transform, "", 30, TextAnchor.MiddleCenter, CurrencyOutline,
                FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -68f));

            GameObject buildButton = CreateButton("Build Mode Button", root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f), new Vector2(200f, 78f), Color.white, BuildModeColors(), "Build", 37, BuildLabel,
                new Vector2(8f, 6f), new Vector2(-8f, -6f));
            view.buildModeButton = buildButton.GetComponent<Button>();
            view.buildModeLabel = buildButton.transform.Find("Label").GetComponent<Text>();

            GameObject templates = Node("Templates", root.transform);
            templates.SetActive(false);
            GameObject itemTemplate = CreatePanel("HotbarItem", templates.transform, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(170f, 160f));
            Button itemButton = itemTemplate.AddComponent<Button>();
            itemButton.targetGraphic = itemTemplate.GetComponent<Image>();
            itemButton.transition = Selectable.Transition.ColorTint;
            itemButton.colors = BuildButtonColors();
            AddLayoutSize(itemTemplate, 170f, 160f);
            BuildMenuItemView itemView = itemTemplate.AddComponent<BuildMenuItemView>();
            itemView.button = itemButton;
            itemView.background = itemTemplate.GetComponent<Image>();
            itemView.label = CreateText("Label", itemTemplate.transform, "1. Item\n$0", 33, TextAnchor.MiddleCenter,
                Color.white, FontStyle.Normal, Vector2.zero, Vector2.one, new Vector2(6f, 4f), new Vector2(-6f, -4f));
            view.hotbarItemTemplate = itemTemplate;

            bar.SetActive(false);
            ApplyFont(root, font);

            PrefabUtility.SaveAsPrefabAsset(root, BuildMenuPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("Created missing Build Menu Canvas prefab at " + BuildMenuPrefabPath);
        }

        static void ConfigureCanvas(GameObject root, int sortingOrder)
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        static GameObject Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, bool raycast = true)
        {
            GameObject go = Node(name, parent);
            SetRect(go, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return go;
        }

        static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor,
            Color color, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = Node(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static GameObject CreateButton(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size, Color color, ColorBlock colors, string label, int fontSize, Color labelColor,
            Vector2? labelOffsetMin = null, Vector2? labelOffsetMax = null)
        {
            GameObject go = CreatePanel(name, parent, color, anchor, anchor, pivot, position, size);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;

            Vector2 min = labelOffsetMin ?? Vector2.zero;
            Vector2 max = labelOffsetMax ?? Vector2.zero;
            CreateText("Label", go.transform, label, fontSize, TextAnchor.MiddleCenter, labelColor,
                FontStyle.Bold, Vector2.zero, Vector2.one, min, max);
            return go;
        }

        static GameObject CreateLayoutRow(string name, Transform parent, float preferredHeight, float spacing)
        {
            GameObject row = Node(name, parent);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.minHeight = preferredHeight;
            return row;
        }

        static void AddLayoutSize(GameObject go, float width, float height)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
        }

        static void AddBorder(Transform parent, Color color)
        {
            CreateBorder(parent, "Outline Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 2f), color);
            CreateBorder(parent, "Outline Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 2f), color);
            CreateBorder(parent, "Outline Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(2f, 0f), color);
            CreateBorder(parent, "Outline Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(2f, 0f), color);
        }

        static void CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject go = CreatePanel(name, parent, color, anchorMin, anchorMax, pivot, position, size, false);
            go.GetComponent<Image>().raycastTarget = false;
        }

        static ColorBlock BuildButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = NormalColor;
            colors.highlightedColor = HoverColor;
            colors.pressedColor = SelectedColor;
            colors.selectedColor = NormalColor;
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            return colors;
        }

        static ColorBlock BuildModeColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = BuildNormal;
            colors.highlightedColor = BuildHover;
            colors.pressedColor = BuildPressed;
            colors.selectedColor = BuildNormal;
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            return colors;
        }

        static void ApplyFont(GameObject root, Font font)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++) texts[i].font = font;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string child = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
