using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Player-facing build mode: selection, hover, ghost preview,
  /// placement, removal and rotation.
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

    [Header("Startup")]
    public bool startInBuildMode = false;

    readonly List<GridCoord> _validationBuffer = new List<GridCoord>();

    PlaceableDefinition _selectedDefinition;
    int _selectedIndex = -1;
    int _rotation;

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

    void Start()
    {
      if (grid == null) grid = FindFirstObjectByType<BuildGrid>();
      if (buildCamera == null) buildCamera = Camera.main;

      SetBuildMode(startInBuildMode);
      RaiseStateChanged();
    }

    void Update()
    {
      if (BuildInput.ToggleBuildModePressed)
      {
        SetBuildMode(!IsInBuildMode);
      }

      if (!IsInBuildMode) return;

      int number = BuildInput.NumberPressed();
      if (number > 0) SelectDefinition(number - 1);

      if (BuildInput.RotatePressed) RotateSelection();

      if (BuildInput.CancelPressed)
      {
        if (_selectedDefinition != null) ClearSelection();
        else SetBuildMode(false);
        return;
      }

      if (_selectedDefinition == null || grid == null)
      {
        HidePreview();
        return;
      }

      if (BuildInput.PointerOverUI)
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

      PlacementResult result = grid.CanPlace(_selectedDefinition, hoverCell, _rotation);
      if (result == PlacementResult.Valid && Economy != null && !Economy.CanAfford(_selectedDefinition.cost))
      {
        result = PlacementResult.InsufficientFunds;
      }

      Vector3 position = grid.GetPlacementPosition(_selectedDefinition, hoverCell, _rotation);
      bool valid = result == PlacementResult.Valid;

      if (ghost != null) ghost.Show(_selectedDefinition, hoverCell, _rotation, position, valid);
      if (overlay != null) overlay.SetHighlight(_selectedDefinition, hoverCell, _rotation, valid);

      if (BuildInput.PlacePressed)
      {
        TryPlace(hoverCell, result);
      }
      else if (BuildInput.RemovePressed)
      {
        TryRemove(hoverCell);
      }
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
      if (grid == null || _selectedDefinition == null) return false;

      PlacementResult result = grid.CanPlace(_selectedDefinition, cell, _rotation);
      return TryPlace(cell, result);
    }

    bool TryPlace(GridCoord cell, PlacementResult result)
    {
      if (result != PlacementResult.Valid)
      {
        Notify(ReasonText(result));
        return false;
      }

      if (_selectedDefinition == null || grid == null) return false;

      PlacedObject placed = grid.Place(_selectedDefinition, cell, _rotation);
      if (placed == null)
      {
        Notify("Could not place here");
        return false;
      }

      IBuildEconomy economy = Economy;
      if (economy != null && !economy.TrySpend(_selectedDefinition.cost))
      {
        grid.Remove(placed);
        Notify("Not enough money");
        return false;
      }

      Notify(string.Format("Placed {0}", _selectedDefinition.displayName));
      return true;
    }

    void TryRemove(GridCoord cell)
    {
      PlacedObject occupant = grid.GetOccupant(cell);
      if (occupant == null) return;

      string label = occupant.Definition != null ? occupant.Definition.displayName : "Object";
      if (grid.Remove(occupant)) Notify(string.Format("Removed {0}", label));
    }

    bool TryGetHoverCell(out GridCoord cell)
    {
      cell = GridCoord.Zero;

      Camera cam = buildCamera != null ? buildCamera : Camera.main;
      if (cam == null || grid == null) return false;

      Ray ray = cam.ScreenPointToRay(BuildInput.PointerPosition);
      Plane plane = new Plane(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));

      float distance;
      if (!plane.Raycast(ray, out distance)) return false;

      GridCoord coord = grid.WorldToCoord(ray.GetPoint(distance));
      if (!grid.IsInside(coord)) return false;

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

    static string ReasonText(PlacementResult result)
    {
      switch (result)
      {
        case PlacementResult.OutOfBounds:
          return "Outside the plot";
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
