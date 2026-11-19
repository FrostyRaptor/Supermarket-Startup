using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Supermarket.Building
{
  /// <summary>
  /// Runtime-generated build HUD: a persistent currency counter and
  /// build-mode toggle plus the build-mode hotbar built from the catalog.
  /// </summary>
  public class BuildMenuUI : MonoBehaviour
  {
    [Header("References")]
    public PlaceableCatalog catalog;
    public BuildController controller;
    public SimpleWallet wallet;

    const float MessageDuration = 2f;
    const float BorderThickness = 2f;
    const float HudMargin = 24f;

    static readonly Color NormalColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    static readonly Color SelectedColor = new Color(0.18f, 0.52f, 0.9f, 1f);
    static readonly Color HoverColor = new Color(0.24f, 0.27f, 0.33f, 1f);

    static readonly Color CurrencyOutlineColor = new Color(0.29f, 0.86f, 0.44f, 1f);
    static readonly Color BillPaperColor = new Color(0.84f, 0.94f, 0.79f, 1f);
    static readonly Color BillInsetColor = new Color(0.22f, 0.56f, 0.33f, 0.55f);
    static readonly Color BillMarkColor = new Color(1f, 1f, 1f, 0.95f);

    static readonly Color BuildNormalColor = new Color(0.96f, 0.62f, 0.13f, 1f);
    static readonly Color BuildHoverColor = new Color(1f, 0.73f, 0.26f, 1f);
    static readonly Color BuildPressedColor = new Color(0.84f, 0.51f, 0.09f, 1f);
    static readonly Color BuildActiveColor = new Color(0.90f, 0.47f, 0.08f, 1f);
    static readonly Color BuildLabelColor = new Color(0.15f, 0.09f, 0.02f, 1f);

    static Sprite _uiSprite;

    readonly List<Button> _itemButtons = new List<Button>();
    readonly List<int> _buttonCatalogIndex = new List<int>();

    GameObject _canvasRoot;
    GameObject _buildBar;
    Button _buildModeButton;
    Text _buildModeLabel;
    Text _messageText;
    CanvasGroup _messageGroup;
    Text _currencyText;
    float _messageExpiry;
    int _lastBalance;
    bool _hasBalance;

    void Start()
    {
      if (controller == null) controller = FindFirstObjectByType<BuildController>();
      if (catalog == null && controller != null) catalog = controller.catalog;
      if (wallet == null) wallet = FindFirstObjectByType<SimpleWallet>();
      if (controller != null) controller.StateChanged += Refresh;
      if (wallet != null) wallet.BalanceChanged += OnBalanceChanged;

      BuildUI();
      Refresh();
      UpdateCurrency();
    }

    void OnDestroy()
    {
      if (controller != null) controller.StateChanged -= Refresh;
      if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
    }

    void Update()
    {
      if (_messageGroup != null)
      {
        float remaining = _messageExpiry - Time.unscaledTime;
        _messageGroup.alpha = remaining <= 0f ? 0f : Mathf.Clamp01(remaining / 0.4f);
      }
    }

    void OnBalanceChanged(int balance)
    {
      _hasBalance = true;
      _lastBalance = balance;

      if (_currencyText != null) _currencyText.text = "$" + balance.ToString("N0");
    }

    void UpdateCurrency()
    {
      if (_currencyText == null) return;

      int balance = wallet != null ? wallet.Balance : 0;
      if (_hasBalance && balance == _lastBalance) return;

      OnBalanceChanged(balance);
    }

    public void ShowMessage(string message)
    {
      if (_messageText == null) return;

      _messageText.text = message;
      _messageExpiry = Time.unscaledTime + MessageDuration;
      if (_messageGroup != null) _messageGroup.alpha = 1f;
    }

    void Refresh()
    {
      if (_canvasRoot == null) return;

      bool buildMode = controller != null && controller.IsInBuildMode;

      if (_buildBar != null) _buildBar.SetActive(buildMode);
      ApplyBuildModeButtonState(buildMode);

      int selected = controller != null ? controller.SelectedIndex : -1;
      int selectedButton = FindButtonIndex(_buttonCatalogIndex, selected);
      for (int i = 0; i < _itemButtons.Count; i++)
      {
        ApplyButtonState(_itemButtons[i], i == selectedButton);
      }
    }

    /// <summary>Maps a catalog index to its hotbar button, tolerating null catalog entries.</summary>
    public static int FindButtonIndex(IReadOnlyList<int> catalogIndices, int catalogIndex)
    {
      if (catalogIndices == null || catalogIndex < 0) return -1;

      for (int i = 0; i < catalogIndices.Count; i++)
      {
        if (catalogIndices[i] == catalogIndex) return i;
      }

      return -1;
    }

    void BuildUI()
    {
      EnsureEventSystem();

      _canvasRoot = new GameObject(
        "Build Menu",
        typeof(RectTransform),
        typeof(Canvas),
        typeof(CanvasScaler),
        typeof(GraphicRaycaster));
      _canvasRoot.transform.SetParent(transform, false);

      Canvas canvas = _canvasRoot.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = 100;

      CanvasScaler scaler = _canvasRoot.GetComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920f, 1080f);
      scaler.matchWidthOrHeight = 0.5f;

      _buildBar = CreateBuildBar(_canvasRoot.transform);
      CreateCurrencyCounter(_canvasRoot.transform);
      CreateBuildModeButton(_canvasRoot.transform);

      _buildBar.SetActive(false);
    }

    GameObject CreateBuildBar(Transform parent)
    {
      GameObject panel = new GameObject(
        "Build Bar",
        typeof(RectTransform),
        typeof(Image),
        typeof(VerticalLayoutGroup));
      panel.transform.SetParent(parent, false);

      RectTransform panelRect = panel.GetComponent<RectTransform>();
      panelRect.anchorMin = new Vector2(0f, 0f);
      panelRect.anchorMax = new Vector2(1f, 0f);
      panelRect.pivot = new Vector2(0.5f, 0f);
      panelRect.anchoredPosition = new Vector2(0f, 16f);
      panelRect.sizeDelta = new Vector2(-32f, 214f);

      Image panelImage = panel.GetComponent<Image>();
      panelImage.color = new Color(0.055f, 0.065f, 0.085f, 0.94f);

      VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
      layout.padding = new RectOffset(14, 14, 10, 10);
      layout.spacing = 6f;
      layout.childAlignment = TextAnchor.MiddleCenter;
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = false;

      GameObject header = CreateRow(panel.transform, 22f, 10f);
      Text hints = CreateText(
        header.transform,
        "Hints",
        "B close  ·  LMB place  ·  RMB remove  ·  R rotate  ·  Esc cancel",
        17,
        TextAnchor.MiddleLeft);
      LayoutElement hintsLayout = hints.gameObject.AddComponent<LayoutElement>();
      hintsLayout.flexibleWidth = 1f;
      hintsLayout.preferredHeight = 22f;

      GameObject messageRow = CreateRow(panel.transform, 22f, 0f);
      _messageText = CreateText(messageRow.transform, "Message", "", 18, TextAnchor.MiddleCenter);
      LayoutElement messageLayout = _messageText.gameObject.AddComponent<LayoutElement>();
      messageLayout.flexibleWidth = 1f;
      messageLayout.preferredHeight = 22f;

      _messageGroup = _messageText.gameObject.AddComponent<CanvasGroup>();
      _messageGroup.alpha = 0f;

      GameObject items = CreateRow(panel.transform, 92f, 8f);
      if (catalog != null)
      {
        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
          PlaceableDefinition definition = catalog.Definitions[i];
          if (definition == null) continue;

          int index = i;
          string label = string.Format("{0}. {1}\n${2}", i + 1, definition.displayName, definition.cost);
          Button button = CreateButton(items.transform, "Item_" + definition.id, label, 18, 170f, 92f);
          button.onClick.AddListener(() => SelectFromUI(index));
          _itemButtons.Add(button);
          _buttonCatalogIndex.Add(index);
        }
      }

      GameObject actions = CreateRow(panel.transform, 34f, 8f);
      Button rotateButton = CreateButton(actions.transform, "Rotate", "Rotate (R)", 17, 130f, 34f);
      rotateButton.onClick.AddListener(RotateFromUI);

      Button closeButton = CreateButton(actions.transform, "Close", "Close (B)", 17, 130f, 34f);
      closeButton.onClick.AddListener(CloseFromUI);

      return panel;
    }

    void CreateCurrencyCounter(Transform parent)
    {
      GameObject root = new GameObject("Currency Counter", typeof(RectTransform));
      root.transform.SetParent(parent, false);

      RectTransform rootRect = root.GetComponent<RectTransform>();
      rootRect.anchorMin = new Vector2(1f, 1f);
      rootRect.anchorMax = new Vector2(1f, 1f);
      rootRect.pivot = new Vector2(1f, 1f);
      rootRect.anchoredPosition = new Vector2(-HudMargin, -HudMargin);
      rootRect.sizeDelta = new Vector2(230f, 62f);

      AddOutline(root.transform, CurrencyOutlineColor);

      GameObject billObject = new GameObject("Dollar Bill", typeof(RectTransform), typeof(Image));
      billObject.transform.SetParent(root.transform, false);

      RectTransform billRect = billObject.GetComponent<RectTransform>();
      billRect.anchorMin = new Vector2(0f, 0.5f);
      billRect.anchorMax = new Vector2(0f, 0.5f);
      billRect.pivot = new Vector2(0f, 0.5f);
      billRect.anchoredPosition = new Vector2(10f, 0f);
      billRect.sizeDelta = new Vector2(54f, 32f);

      Image billImage = billObject.GetComponent<Image>();
      ApplyRoundedSprite(billImage);
      billImage.color = BillPaperColor;
      billImage.raycastTarget = false;

      GameObject insetObject = new GameObject("Bill Inset", typeof(RectTransform), typeof(Image));
      insetObject.transform.SetParent(billObject.transform, false);

      RectTransform insetRect = insetObject.GetComponent<RectTransform>();
      insetRect.anchorMin = Vector2.zero;
      insetRect.anchorMax = Vector2.one;
      insetRect.offsetMin = new Vector2(3f, 3f);
      insetRect.offsetMax = new Vector2(-3f, -3f);

      Image insetImage = insetObject.GetComponent<Image>();
      ApplyRoundedSprite(insetImage);
      insetImage.color = BillInsetColor;
      insetImage.raycastTarget = false;

      Text billMark = CreateText(billObject.transform, "Bill Mark", "$", 19, TextAnchor.MiddleCenter);
      billMark.fontStyle = FontStyle.Bold;
      billMark.color = BillMarkColor;

      RectTransform billMarkRect = billMark.rectTransform;
      billMarkRect.anchorMin = Vector2.zero;
      billMarkRect.anchorMax = Vector2.one;
      billMarkRect.offsetMin = Vector2.zero;
      billMarkRect.offsetMax = Vector2.zero;

      _currencyText = CreateText(root.transform, "Amount", "$0", 24, TextAnchor.MiddleCenter);
      _currencyText.fontStyle = FontStyle.Bold;
      _currencyText.color = Color.white;

      RectTransform amountRect = _currencyText.rectTransform;
      amountRect.anchorMin = Vector2.zero;
      amountRect.anchorMax = Vector2.one;
      amountRect.offsetMin = Vector2.zero;
      amountRect.offsetMax = Vector2.zero;

      Shadow amountShadow = _currencyText.gameObject.AddComponent<Shadow>();
      amountShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
      amountShadow.effectDistance = new Vector2(1f, -1f);
    }

    void CreateBuildModeButton(Transform parent)
    {
      GameObject go = new GameObject(
        "Build Mode Button",
        typeof(RectTransform),
        typeof(Image),
        typeof(Button));
      go.transform.SetParent(parent, false);

      RectTransform rect = go.GetComponent<RectTransform>();
      rect.anchorMin = new Vector2(1f, 0f);
      rect.anchorMax = new Vector2(1f, 0f);
      rect.pivot = new Vector2(1f, 0f);
      rect.anchoredPosition = new Vector2(-HudMargin, HudMargin);
      rect.sizeDelta = new Vector2(180f, 64f);

      Image image = go.GetComponent<Image>();
      ApplyRoundedSprite(image);
      image.color = Color.white;

      _buildModeButton = go.GetComponent<Button>();
      _buildModeButton.targetGraphic = image;
      _buildModeButton.transition = Selectable.Transition.ColorTint;
      _buildModeButton.onClick.AddListener(ToggleBuildMode);

      ColorBlock colors = _buildModeButton.colors;
      colors.normalColor = BuildNormalColor;
      colors.highlightedColor = BuildHoverColor;
      colors.pressedColor = BuildPressedColor;
      colors.selectedColor = BuildNormalColor;
      colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
      colors.colorMultiplier = 1f;
      colors.fadeDuration = 0.06f;
      _buildModeButton.colors = colors;

      _buildModeLabel = CreateText(go.transform, "Label", "Build", 22, TextAnchor.MiddleCenter);
      _buildModeLabel.fontStyle = FontStyle.Bold;
      _buildModeLabel.color = BuildLabelColor;

      RectTransform labelRect = _buildModeLabel.rectTransform;
      labelRect.anchorMin = Vector2.zero;
      labelRect.anchorMax = Vector2.one;
      labelRect.offsetMin = new Vector2(8f, 6f);
      labelRect.offsetMax = new Vector2(-8f, -6f);
    }

    void ApplyBuildModeButtonState(bool buildMode)
    {
      if (_buildModeButton == null) return;

      ColorBlock colors = _buildModeButton.colors;
      colors.normalColor = buildMode ? BuildActiveColor : BuildNormalColor;
      colors.selectedColor = colors.normalColor;
      colors.highlightedColor = buildMode ? BuildPressedColor : BuildHoverColor;
      colors.pressedColor = BuildPressedColor;
      _buildModeButton.colors = colors;

      if (_buildModeLabel != null)
      {
        _buildModeLabel.text = buildMode ? "Exit Build" : "Build";
      }
    }

    static void AddOutline(Transform parent, Color color)
    {
      CreateBorderStrip(
        parent, "Outline Top",
        new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
        new Vector2(0f, 0f), new Vector2(0f, BorderThickness), color);

      CreateBorderStrip(
        parent, "Outline Bottom",
        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
        new Vector2(0f, 0f), new Vector2(0f, BorderThickness), color);

      CreateBorderStrip(
        parent, "Outline Left",
        new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
        new Vector2(0f, 0f), new Vector2(BorderThickness, 0f), color);

      CreateBorderStrip(
        parent, "Outline Right",
        new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
        new Vector2(0f, 0f), new Vector2(BorderThickness, 0f), color);
    }

    static void CreateBorderStrip(
      Transform parent,
      string name,
      Vector2 anchorMin,
      Vector2 anchorMax,
      Vector2 pivot,
      Vector2 anchoredPosition,
      Vector2 sizeDelta,
      Color color)
    {
      GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
      go.transform.SetParent(parent, false);

      RectTransform rect = go.GetComponent<RectTransform>();
      rect.anchorMin = anchorMin;
      rect.anchorMax = anchorMax;
      rect.pivot = pivot;
      rect.anchoredPosition = anchoredPosition;
      rect.sizeDelta = sizeDelta;

      Image image = go.GetComponent<Image>();
      image.color = color;
      image.raycastTarget = false;
    }

    static void ApplyRoundedSprite(Image image)
    {
      Sprite sprite = GetUiSprite();
      if (sprite == null) return;

      image.sprite = sprite;
      image.type = Image.Type.Sliced;
    }

    static Sprite GetUiSprite()
    {
      if (_uiSprite == null)
      {
        _uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
      }

      return _uiSprite;
    }

    void ToggleBuildMode()
    {
      if (controller != null) controller.SetBuildMode(!controller.IsInBuildMode);
    }

    void SelectFromUI(int index)
    {
      if (controller != null) controller.SelectDefinition(index);
    }

    void RotateFromUI()
    {
      if (controller != null) controller.RotateSelection();
    }

    void CloseFromUI()
    {
      if (controller != null) controller.SetBuildMode(false);
    }

    static void ApplyButtonState(Button button, bool selected)
    {
      ColorBlock colors = button.colors;
      colors.normalColor = selected ? SelectedColor : NormalColor;
      colors.selectedColor = colors.normalColor;
      colors.highlightedColor = selected ? SelectedColor : HoverColor;
      colors.pressedColor = selected ? SelectedColor : HoverColor;
      button.colors = colors;
    }

    static GameObject CreateRow(Transform parent, float preferredHeight, float spacing)
    {
      GameObject row = new GameObject(
        "Row",
        typeof(RectTransform),
        typeof(HorizontalLayoutGroup),
        typeof(LayoutElement));
      row.transform.SetParent(parent, false);

      HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
      layout.spacing = spacing;
      layout.childAlignment = TextAnchor.MiddleCenter;
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = false;
      layout.childForceExpandHeight = true;

      LayoutElement element = row.GetComponent<LayoutElement>();
      element.preferredHeight = preferredHeight;
      element.minHeight = preferredHeight;

      return row;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
    {
      GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
      go.transform.SetParent(parent, false);

      Text text = go.GetComponent<Text>();
      text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      text.fontSize = fontSize;
      text.text = content;
      text.alignment = anchor;
      text.color = Color.white;
      text.raycastTarget = false;
      text.horizontalOverflow = HorizontalWrapMode.Overflow;
      text.verticalOverflow = VerticalWrapMode.Overflow;

      return text;
    }

    static Button CreateButton(
      Transform parent,
      string name,
      string label,
      int fontSize,
      float width,
      float height)
    {
      GameObject go = new GameObject(
        name,
        typeof(RectTransform),
        typeof(Image),
        typeof(Button),
        typeof(LayoutElement));
      go.transform.SetParent(parent, false);

      LayoutElement element = go.GetComponent<LayoutElement>();
      element.preferredWidth = width;
      element.minWidth = width;
      element.preferredHeight = height;
      element.minHeight = height;

      Image image = go.GetComponent<Image>();
      image.color = Color.white;

      Button button = go.GetComponent<Button>();
      button.targetGraphic = image;
      button.transition = Selectable.Transition.ColorTint;

      ColorBlock colors = button.colors;
      colors.normalColor = NormalColor;
      colors.highlightedColor = HoverColor;
      colors.pressedColor = SelectedColor;
      colors.selectedColor = NormalColor;
      colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
      colors.colorMultiplier = 1f;
      colors.fadeDuration = 0.05f;
      button.colors = colors;

      Text text = CreateText(go.transform, "Label", label, fontSize, TextAnchor.MiddleCenter);
      RectTransform textRect = text.rectTransform;
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.offsetMin = new Vector2(6f, 4f);
      textRect.offsetMax = new Vector2(-6f, -4f);

      return button;
    }

    static void EnsureEventSystem()
    {
      EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
      if (eventSystem == null)
      {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        return;
      }

      // Replace a legacy input module if one was added elsewhere in the scene.
      if (eventSystem.GetComponent<InputSystemUIInputModule>() != null) return;

      StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
      if (legacyModule != null)
      {
        legacyModule.enabled = false;
        Object.Destroy(legacyModule);
      }

      eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }
  }
}
