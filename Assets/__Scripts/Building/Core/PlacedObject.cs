using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Lives on a placed prefab instance. Tracks which definition produced it and
  /// which grid cells it owns. Registration is owned by BuildGrid.
  /// </summary>
  public class PlacedObject : MonoBehaviour
  {
    [SerializeField] PlaceableDefinition definition;
    [SerializeField] Vector2Int originCell;
    [SerializeField] int quarterTurns;

    BuildGrid _grid;
    bool _registered;

    public PlaceableDefinition Definition
    {
      get { return definition; }
    }

    public GridCoord Origin
    {
      get { return new GridCoord(originCell.x, originCell.y); }
    }

    public int QuarterTurns
    {
      get { return quarterTurns; }
    }

    public BuildGrid Grid
    {
      get { return _grid; }
    }

    public bool IsRegistered
    {
      get { return _registered; }
    }

    public void Initialize(BuildGrid grid, PlaceableDefinition def, GridCoord origin, int turns)
    {
      _grid = grid;
      definition = def;
      originCell = new Vector2Int(origin.X, origin.Z);
      quarterTurns = FootprintUtility.NormalizeTurns(turns);
      _registered = true;

      if (grid != null) transform.rotation = grid.GetPlacementRotation(quarterTurns);
    }

    internal void MarkReleased()
    {
      _registered = false;
    }

    void OnDestroy()
    {
      // If something destroyed this object outside of BuildGrid.Remove,
      // make sure the grid does not keep stale occupancy references.
      if (_registered && _grid != null) _grid.NotifyExternalDestroyed(this);
    }
  }
}
