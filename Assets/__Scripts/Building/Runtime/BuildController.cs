using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Player-facing build mode: selection, hover, ghost preview,
  /// placement, removal and rotation.
  ///
  /// Acts as the default orchestration facade over grid, input, economy and
  /// presentation services while preserving the existing scene/test API.
  /// </summary>
  public class BuildController : MonoBehaviour
  {
    [Header("References")]
    public BuildGrid grid;
    public PlaceableCatalog catalog;
    public Camera buildCamera;
    public BuildGhost ghost;
    public GridOverlay overlay;
    public BuildMenuUI menu;

    [Tooltip("Optional. A component implementing IBuildEconomy.")]
    public MonoBehaviour economySource;

    [Header("Optional Services")]
    [Tooltip("Optional. A component implementing IBuildInputProvider.")]
    public MonoBehaviour inputSource;

    [Header("Startup")]
    public bool startInBuildMode = false;

    readonly List<GridCoord> _validationBuffer = new List<GridCoord>();

    PlaceableDefinition _selectedDefinition;
    int _selectedIndex = -1;
    int _rotation;

    IBuildInputProvider _inputProvider;

    public event Action StateChanged;

    public bool IsInBuildMode { get; private set; }

    public PlaceableDefinition SelectedDefinition
    {
      get { return _selectedDefinition; }
    }

    public int SelectedIndex
    {
      get { return _selectedIndex; }
    }

    public int Rotation
    {
      get { return _rotation; }
    }

    public IBuildEconomy Economy
    {
      get { return economySource as IBuildEconomy; }
    }

    /// <summary>Grid abstraction for queries/placement operations.</summary>
    public IGridOccupancy Grid
    {
      get { return grid; }
    }

    /// <summary>Input provider seam (defaults to Input System/EventSystem).</summary>
    public IBuildInputProvider InputProvider
    {
      get { return ResolveInputProvider(); }
      set { _inputProvider = value; }
    }

    IBuildInputProvider ResolveInputProvider()
    {
      if (_inputProvider != null) return _inputProvider;

      IBuildInputProvider provider = inputSource as IBuildInputProvider;
      if (provider == null) provider = InputSystemBuildInputProvider.Instance;
      _inputProvider = provider;
      return provider;
    }

    void Start()
    {
      if (grid == null) grid = FindFirstObjectByType<BuildGrid>();
      if (buildCamera == null) buildCamera = Camera.main;
      if (_inputProvider == null) ResolveInputProvider();

      SetBuildMode(startInBuildMode);
      RaiseStateChanged();
    }

    void Update()
    {
      IBuildInputProvider input = InputProvider;

      if (input.ToggleBuildModePressed)
      {
        SetBuildMode(!IsInBuildMode);
      }

      if (!IsInBuildMode) return;

      int number = input.NumberPressed();
      if (number > 0) SelectDefinition(number - 1);

      if (input.RotatePressed) RotateSelection();

      if (input.CancelPressed)
      {
        if (_selectedDefinition != null) ClearSelection();
        else SetBuildMode(false);
        return;
      }

      if (_selectedDefinition == null || Grid == null)
      {
        HidePreview();
        return;
      }

      if (input.PointerOverUI)
      {
        HidePreview();
        return;
      }

      GridCoord hoverCell;
      if (!TryGetHoverCell(out hoverCell))
      {
        HidePreview();
        return;
      }

      PlacementResult result = EvaluatePlacement(hoverCell);

      Vector3 position = Grid.GetPlacementPosition(_selectedDefinition, hoverCell, _rotation);
      bool valid = result == PlacementResult.Valid;

      if (ghost != null) ghost.Show(_selectedDefinition, hoverCell, _rotation, position, valid);
      if (overlay != null) overlay.SetHighlight(_selectedDefinition, hoverCell, _rotation, valid);

      if (input.PlacePressed)
      {
        TryPlace(hoverCell, result);
      }
      else if (input.RemovePressed)
      {
        TryRemove(hoverCell);
      }
    }

    PlacementResult EvaluatePlacement(GridCoord cell)
    {
      IGridOccupancy g = Grid;
      if (g == null || _selectedDefinition == null) return PlacementResult.InvalidDefinition;

      // Preserve existing buffer-aware validation behavior and economy gating.
      PlacementResult result = PlacementValidator.Validate(g, _selectedDefinition, cell, _rotation, _validationBuffer);
      if (result == PlacementResult.Valid && Economy != null && !Economy.CanAfford(_selectedDefinition.cost))
      {
        return PlacementResult.InsufficientFunds;
      }

      return result;
    }

    public void SetBuildMode(bool on)
    {
      if (IsInBuildMode == on) return;
      IsInBuildMode = on;

      if (on)
      {
        if (catalog != null && catalog.Definitions.Count > 0)
        {
          _selectedIndex = Mathf.Clamp(_selectedIndex < 0 ? 0 : _selectedIndex, 0, catalog.Definitions.Count - 1);
          _selectedDefinition = catalog.Definitions[_selectedIndex];
        }
      }
      else
      {
        _selectedIndex = -1;
        _selectedDefinition = null;
      }

      _rotation = 0;

      if (ghost != null) ghost.Hide();
      if (overlay != null)
      {
        overlay.ClearHighlight();
        overlay.SetVisible(on);
      }

      RaiseStateChanged();
    }

    public void SelectDefinition(int index)
    {
      if (catalog == null || index < 0 || index >= catalog.Definitions.Count)
      {
        ClearSelection();
        return;
      }

      if (catalog.Definitions[index] == null)
      {
        ClearSelection();
        return;
      }

      _selectedIndex = index;
      _selectedDefinition = catalog.Definitions[index];
      _rotation = 0;

      if (ghost != null) ghost.Hide();
      if (overlay != null) overlay.ClearHighlight();
      RaiseStateChanged();
    }

    public void ClearSelection()
    {
      _selectedIndex = -1;
      _selectedDefinition = null;
      _rotation = 0;

      if (ghost != null) ghost.Hide();
      if (overlay != null) overlay.ClearHighlight();
      RaiseStateChanged();
    }

    public void RotateSelection()
    {
      if (_selectedDefinition == null || !_selectedDefinition.rotatable) return;

      _rotation = FootprintUtility.NormalizeTurns(_rotation + 1);
      RaiseStateChanged();
    }

    /// <summary>
    /// Validates and places the current selection at <paramref name="cell"/>.
    /// Exposed for tests and future AI/tutorial drivers.
    /// </summary>
    public bool TryPlaceSelected(GridCoord cell)
    {
      if (Grid == null || _selectedDefinition == null) return false;

      PlacementResult result = EvaluatePlacement(cell);
      return TryPlace(cell, result);
    }

    bool TryPlace(GridCoord cell, PlacementResult result)
    {
      if (result != PlacementResult.Valid)
      {
        Notify(ReasonText(result, _selectedDefinition));
        return false;
      }

      if (_selectedDefinition == null || Grid == null) return false;

      PlacedObject placed = Grid.Place(_selectedDefinition, cell, _rotation);
      if (placed == null)
      {
        Notify("Could not place here");
        return false;
      }

      IBuildEconomy economy = Economy;
      if (economy != null && !economy.TrySpend(_selectedDefinition.cost))
      {
        Grid.Remove(placed);
        Notify("Not enough money");
        return false;
      }

      Notify(string.Format("Placed {0}", _selectedDefinition.displayName));
      return true;
    }

    /// <summary>
    /// Removes the occupant at <paramref name="cell"/> and refunds its cost.
    /// Exposed for tests and future AI/tutorial drivers.
    /// </summary>
    public bool TryRemoveAt(GridCoord cell)
    {
      IGridOccupancy g = Grid;
      if (g == null) return false;

      PlacedObject occupant = g.GetOccupant(cell);
      if (occupant == null) return false;

      string label = occupant.Definition != null ? occupant.Definition.displayName : "Object";
      int refund = occupant.Definition != null ? occupant.Definition.cost : 0;
      if (g.Remove(occupant))
      {
        IBuildEconomy economy = Economy;
        if (economy != null && refund > 0) economy.Add(refund);
        Notify(string.Format("Removed {0}", label));
        return true;
      }

      return false;
    }

    void TryRemove(GridCoord cell)
    {
      TryRemoveAt(cell);
    }

    bool TryGetHoverCell(out GridCoord cell)
    {
      cell = GridCoord.Zero;

      Camera cam = buildCamera != null ? buildCamera : Camera.main;
      IGridOccupancy g = Grid;
      if (cam == null || g == null) return false;

      Ray ray = cam.ScreenPointToRay(InputProvider.PointerPosition);
      Plane plane = new Plane(Vector3.up, new Vector3(0f, g.Origin.y, 0f));

      float distance;
      if (!plane.Raycast(ray, out distance)) return false;

      GridCoord coord = g.WorldToCoord(ray.GetPoint(distance));
      if (!g.IsInside(coord)) return false;

      cell = coord;
      return true;
    }

    void HidePreview()
    {
      if (ghost != null) ghost.Hide();
      if (overlay != null) overlay.ClearHighlight();
    }

    void Notify(string message)
    {
      if (menu != null) menu.ShowMessage(message);
    }

    void RaiseStateChanged()
    {
      if (StateChanged != null) StateChanged();
    }

    static string ReasonText(PlacementResult result, PlaceableDefinition definition)
    {
      switch (result)
      {
        case PlacementResult.OutOfBounds:
          return "Outside the plot";
        case PlacementResult.BlockedArea:
          return "That section is not buildable";
        case PlacementResult.WrongSection:
          return definition != null && definition.allowedArea == PlaceableArea.Checkout
            ? "Checkout lanes can only be placed in the checkout section"
            : "Only checkout lanes can be placed in the checkout section";
        case PlacementResult.Occupied:
          return "That space is already taken";
        case PlacementResult.InsufficientFunds:
          return "Not enough money";
        case PlacementResult.InvalidDefinition:
          return "Invalid placeable";
        default:
          return "Can't place there";
      }
    }
  }
}
