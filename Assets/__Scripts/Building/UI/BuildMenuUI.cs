using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Supermarket.UI;
using GameWallet = Supermarket.Economy.Economy;

namespace Supermarket.Building
{
  /// <summary>
  /// Drives the authored Build Menu Canvas prefab. Text formatting, event
  /// subscriptions and controller callbacks stay here; the prefab supplies the
  /// display hierarchy.
  /// </summary>
  public class BuildMenuUI : MonoBehaviour
  {
    [Header("References")]
    public PlaceableCatalog catalog;
    public BuildController controller;
    public SimpleWallet wallet;

    [Header("Authored Canvas")]
    public GameObject buildMenuPrefab;

    const float MessageDuration = 2f;

    static readonly Color NormalColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    static readonly Color SelectedColor = new Color(0.18f, 0.52f, 0.9f, 1f);
    static readonly Color HoverColor = new Color(0.24f, 0.27f, 0.33f, 1f);

    static readonly Color CurrencyOutlineColor = new Color(0.29f, 0.86f, 0.44f, 1f);
    static readonly Color NegativeRateColor = new Color(0.92f, 0.49f, 0.28f, 1f);

    static readonly Color BuildNormalColor = new Color(0.96f, 0.62f, 0.13f, 1f);
    static readonly Color BuildHoverColor = new Color(1f, 0.73f, 0.26f, 1f);
    static readonly Color BuildPressedColor = new Color(0.84f, 0.51f, 0.09f, 1f);
    static readonly Color BuildActiveColor = new Color(0.90f, 0.47f, 0.08f, 1f);
    static readonly Color BuildLabelColor = new Color(0.15f, 0.09f, 0.02f, 1f);

    readonly List<Button> _itemButtons = new List<Button>();
    readonly List<int> _buttonCatalogIndex = new List<int>();

    BuildMenuView _view;
    GameObject _menuInstance;
    GameWallet _economy;
    float _messageExpiry;
    int _lastBalance;
    bool _hasBalance;

    void Start()
    {
      if (controller == null) controller = FindFirstObjectByType<BuildController>();
      if (catalog == null && controller != null) catalog = controller.catalog;
      if (wallet == null) wallet = FindFirstObjectByType<SimpleWallet>();
      if (_economy == null) _economy = wallet as GameWallet;
      if (_economy == null) _economy = FindFirstObjectByType<GameWallet>();
      if (controller != null) controller.StateChanged += Refresh;
      if (wallet != null) wallet.BalanceChanged += OnBalanceChanged;
      if (_economy != null) _economy.StatsChanged += OnStatsChanged;

      if (!InstantiateView()) return;
      Refresh();
      UpdateCurrency();
      RefreshRate();
    }

    void OnDestroy()
    {
      if (controller != null) controller.StateChanged -= Refresh;
      if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
      if (_economy != null) _economy.StatsChanged -= OnStatsChanged;
    }

    void Update()
    {
      if (_view == null || _view.messageGroup == null) return;
      float remaining = _messageExpiry - Time.unscaledTime;
      _view.messageGroup.alpha = remaining <= 0f ? 0f : Mathf.Clamp01(remaining / 0.4f);
    }

    bool InstantiateView()
    {
      if (buildMenuPrefab == null)
      {
        Debug.LogError("BuildMenuUI needs an authored Build Menu Canvas prefab assigned.", this);
        enabled = false;
        return false;
      }

      EnsureEventSystem();

      _menuInstance = Instantiate(buildMenuPrefab, transform);
      _view = _menuInstance.GetComponent<BuildMenuView>();
      if (_view == null)
      {
        Debug.LogError("Authored Build Menu prefab is missing its BuildMenuView component.", this);
        enabled = false;
        return false;
      }

      if (_view.buildModeButton != null) _view.buildModeButton.onClick.AddListener(ToggleBuildMode);
      if (_view.rotateButton != null) _view.rotateButton.onClick.AddListener(RotateFromUI);
      if (_view.closeButton != null) _view.closeButton.onClick.AddListener(CloseFromUI);

      BuildHotbar();
      if (_view.buildBar != null) _view.buildBar.SetActive(false);
      return true;
    }

    void BuildHotbar()
    {
      if (_view == null || _view.hotbarItemTemplate == null || _view.hotbarContainer == null) return;
      if (catalog == null) return;

      for (int i = 0; i < catalog.Definitions.Count; i++)
      {
        PlaceableDefinition definition = catalog.Definitions[i];
        if (definition == null) continue;

        int index = i;
        GameObject item = Instantiate(_view.hotbarItemTemplate, _view.hotbarContainer);
        item.SetActive(true);

        BuildMenuItemView itemView = item.GetComponent<BuildMenuItemView>();
        if (itemView == null) continue;

        itemView.label.text = string.Format("{0}. {1}\n${2}", i + 1, definition.displayName, definition.cost);

        Button button = itemView.button != null ? itemView.button : item.GetComponent<Button>();
        if (button == null) continue;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectFromUI(index));

        _itemButtons.Add(button);
        _buttonCatalogIndex.Add(index);
      }
    }

    void OnBalanceChanged(int balance)
    {
      _hasBalance = true;
      _lastBalance = balance;

      if (_view != null && _view.currencyText != null) _view.currencyText.text = "$" + balance.ToString("N0");
    }

    void UpdateCurrency()
    {
      if (_view == null || _view.currencyText == null) return;

      int balance = wallet != null ? wallet.Balance : 0;
      if (_hasBalance && balance == _lastBalance) return;

      OnBalanceChanged(balance);
    }

    void OnStatsChanged()
    {
      RefreshRate();
    }

    void RefreshRate()
    {
      if (_view == null || _view.rateText == null) return;

      float rate = _economy == null ? 0f : _economy.NetPerRealHour;
      string value = "Net: " + (rate >= 0f ? "+" : "-") + "$" + Mathf.Abs(rate).ToString("0.0") + " / hour";
      if (_view.rateText.text != value) _view.rateText.text = value;
      _view.rateText.color = rate >= 0f ? CurrencyOutlineColor : NegativeRateColor;
    }

    public void ShowMessage(string message)
    {
      if (_view == null || _view.messageText == null) return;

      _view.messageText.text = message;
      _messageExpiry = Time.unscaledTime + MessageDuration;
      if (_view.messageGroup != null) _view.messageGroup.alpha = 1f;
    }

    void Refresh()
    {
      if (_view == null) return;

      bool buildMode = controller != null && controller.IsInBuildMode;

      if (_view.buildBar != null) _view.buildBar.SetActive(buildMode);
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

    static void ApplyBuildModeState(BuildMenuView view, bool buildMode)
    {
      if (view == null || view.buildModeButton == null) return;

      ColorBlock colors = view.buildModeButton.colors;
      colors.normalColor = buildMode ? BuildActiveColor : BuildNormalColor;
      colors.selectedColor = colors.normalColor;
      colors.highlightedColor = buildMode ? BuildPressedColor : BuildHoverColor;
      colors.pressedColor = BuildPressedColor;
      view.buildModeButton.colors = colors;

      if (view.buildModeLabel != null)
      {
        view.buildModeLabel.text = buildMode ? "Exit Build" : "Build";
      }
    }

    void ApplyBuildModeButtonState(bool buildMode)
    {
      ApplyBuildModeState(_view, buildMode);
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
