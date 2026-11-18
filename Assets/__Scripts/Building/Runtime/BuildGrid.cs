using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Owns the rectangular build plot: coordinate conversion, occupancy,
  /// placement, removal and change events.
  ///
  /// Implements <see cref="IGridOccupancy"/> and supports an injectable
  /// <see cref="IPlacedObjectFactory"/> so spawning/hierarchy concerns can be
  /// decoupled from occupancy/state.
  /// </summary>
  public class BuildGrid : MonoBehaviour, IGridOccupancy
  {
    [Header("Grid")]
    public Vector2Int size = new Vector2Int(16, 16);
    public float cellSize = 1f;

    [Tooltip("World position of cell (0,0)'s minimum corner, on the floor surface.")]
    public Vector3 origin = new Vector3(-8f, 0.75f, -8f);

    [Header("Hierarchy")]
    public Transform placedRoot;

    [Header("Optional Services")]
    [Tooltip("Optional. A component implementing IPlacedObjectFactory for custom spawning/pooling.")]
    public MonoBehaviour objectFactorySource;

    PlacedObject[] _occupants;
    int _storageWidth;
    int _storageDepth;

    readonly List<GridCoord> _cellBuffer = new List<GridCoord>();
    readonly List<PlacedObject> _queryBuffer = new List<PlacedObject>();

    IPlacedObjectFactory _objectFactory;

    public event Action<PlacedObject> ObjectPlaced;
    public event Action<PlacedObject> ObjectRemoved;

    public Vector2Int Size
    {
      get { return new Vector2Int(Width, Depth); }
    }

    public float CellSize
    {
      get { return Mathf.Max(0.01f, cellSize); }
    }

    public Vector3 Origin
    {
      get { return origin; }
    }

    public int Width
    {
      get { return Mathf.Max(1, size.x); }
    }

    public int Depth
    {
      get { return Mathf.Max(1, size.y); }
    }

    public int CellCount
    {
      get { return Width * Depth; }
    }

    public Transform PlacedRoot
    {
      get { return placedRoot != null ? placedRoot : transform; }
    }

    /// <summary>Factory used to create/release placed instances. Defaults to the built-in factory.</summary>
    public IPlacedObjectFactory ObjectFactory
    {
      get { return _objectFactory ?? ResolveObjectFactory(); }
      set { _objectFactory = value; }
    }

    IPlacedObjectFactory ResolveObjectFactory()
    {
      IPlacedObjectFactory factory = objectFactorySource as IPlacedObjectFactory;
      if (factory == null) factory = DefaultPlacedObjectFactory.Instance;
      _objectFactory = factory;
      return factory;
    }

    void Awake()
    {
      EnsureStorage();
      if (_objectFactory == null) ResolveObjectFactory();
    }

    void OnValidate()
    {
      size = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
      cellSize = Mathf.Max(0.01f, cellSize);
    }

    void EnsureStorage()
    {
      int w = Width;
      int d = Depth;

      if (_occupants == null)
      {
        _occupants = new PlacedObject[w * d];
        _storageWidth = w;
        _storageDepth = d;
        return;
      }

      if (_storageWidth == w && _storageDepth == d) return;

      if (HasAnyOccupant())
      {
        Debug.LogWarning(string.Format(
          "BuildGrid: size cannot change from {0}x{1} to {2}x{3} while objects are placed; keeping the old size.",
          _storageWidth, _storageDepth, w, d));
        size = new Vector2Int(_storageWidth, _storageDepth);
        return;
      }

      _occupants = new PlacedObject[w * d];
      _storageWidth = w;
      _storageDepth = d;
    }

    bool HasAnyOccupant()
    {
      if (_occupants == null) return false;

      for (int i = 0; i < _occupants.Length; i++)
      {
        if (_occupants[i] != null) return true;
      }

      return false;
    }

    int Index(GridCoord cell)
    {
      return cell.Z * Width + cell.X;
    }

    public bool IsInside(GridCoord cell)
    {
      return cell.X >= 0 && cell.Z >= 0 && cell.X < Width && cell.Z < Depth;
    }

    public Vector3 CoordToWorldCorner(GridCoord cell)
    {
      return new Vector3(
        origin.x + cell.X * CellSize,
        origin.y,
        origin.z + cell.Z * CellSize);
    }

    public Vector3 CoordToWorldCenter(GridCoord cell)
    {
      return new Vector3(
        origin.x + (cell.X + 0.5f) * CellSize,
        origin.y,
        origin.z + (cell.Z + 0.5f) * CellSize);
    }

    public GridCoord WorldToCoord(Vector3 world)
    {
      int x = Mathf.FloorToInt((world.x - origin.x) / CellSize);
      int z = Mathf.FloorToInt((world.z - origin.z) / CellSize);
      return new GridCoord(x, z);
    }

    public Vector3 GetFootprintCenter(GridCoord originCell, Vector2Int rotatedSize)
    {
      float x = originCell.X + rotatedSize.x * 0.5f;
      float z = originCell.Z + rotatedSize.y * 0.5f;

      return new Vector3(
        origin.x + x * CellSize,
        origin.y,
        origin.z + z * CellSize);
    }

    public Vector3 GetPlacementPosition(PlaceableDefinition definition, GridCoord originCell, int turns)
    {
      if (definition == null) return origin;

      Vector2Int rotated = FootprintUtility.RotatedSize(definition.Footprint, turns);
      return GetFootprintCenter(originCell, rotated)
             + GetPlacementRotation(turns) * definition.pivotOffset;
    }

    public Quaternion GetPlacementRotation(int turns)
    {
      return Quaternion.Euler(0f, FootprintUtility.NormalizeTurns(turns) * 90f, 0f);
    }

    public PlacedObject GetOccupant(GridCoord cell)
    {
      EnsureStorage();
      if (!IsInside(cell)) return null;
      return _occupants[Index(cell)];
    }

    public PlacementResult CanPlace(PlaceableDefinition definition, GridCoord originCell, int turns)
    {
      EnsureStorage();
      return PlacementValidator.Validate((IGridOccupancy)this, definition, originCell, turns, _cellBuffer);
    }

    public PlacedObject Place(PlaceableDefinition definition, GridCoord originCell, int turns)
    {
      EnsureStorage();

      turns = FootprintUtility.NormalizeTurns(turns);
      if (CanPlace(definition, originCell, turns) != PlacementResult.Valid) return null;

      Vector3 position = GetPlacementPosition(definition, originCell, turns);
      Quaternion rotation = GetPlacementRotation(turns);

      IPlacedObjectFactory factory = ObjectFactory;
      PlacedObject placed = factory.Create(definition, originCell, turns, position, rotation, PlacedRoot);
      if (placed == null) return null;

      placed.Initialize(this, definition, originCell, turns);

      FootprintUtility.GetCellOffsets(definition.Footprint, turns, _cellBuffer);
      for (int i = 0; i < _cellBuffer.Count; i++)
      {
        GridCoord cell = originCell + _cellBuffer[i];
        _occupants[Index(cell)] = placed;
      }

      if (ObjectPlaced != null) ObjectPlaced(placed);
      return placed;
    }

    public bool Remove(PlacedObject placed)
    {
      if (placed == null) return false;
      if (!Unregister(placed)) return false;

      if (ObjectRemoved != null) ObjectRemoved(placed);
      ObjectFactory.Release(placed);
      return true;
    }

    public void ClearAll()
    {
      EnsureStorage();

      _queryBuffer.Clear();
      HashSet<PlacedObject> seen = new HashSet<PlacedObject>();
      for (int i = 0; i < _occupants.Length; i++)
      {
        PlacedObject placed = _occupants[i];
        if (placed != null && seen.Add(placed)) _queryBuffer.Add(placed);
      }

      for (int i = 0; i < _queryBuffer.Count; i++)
      {
        Remove(_queryBuffer[i]);
      }

      Array.Clear(_occupants, 0, _occupants.Length);
    }

    public void GetAllPlacedObjects(List<PlacedObject> results)
    {
      if (results == null) return;
      results.Clear();
      EnsureStorage();

      HashSet<PlacedObject> seen = new HashSet<PlacedObject>();
      for (int i = 0; i < _occupants.Length; i++)
      {
        PlacedObject placed = _occupants[i];
        if (placed != null && seen.Add(placed)) results.Add(placed);
      }
    }

    internal bool Unregister(PlacedObject placed)
    {
      EnsureStorage();
      if (placed == null) return false;

      bool found = false;
      for (int i = 0; i < _occupants.Length; i++)
      {
        if (_occupants[i] == placed)
        {
          _occupants[i] = null;
          found = true;
        }
      }

      if (found) placed.MarkReleased();
      return found;
    }

    internal void NotifyExternalDestroyed(PlacedObject placed)
    {
      if (Unregister(placed))
      {
        if (ObjectRemoved != null) ObjectRemoved(placed);
      }
    }

    void OnDrawGizmos()
    {
      float y = origin.y + 0.02f;
      float w = Width * CellSize;
      float d = Depth * CellSize;

      Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
      Vector3 a = new Vector3(origin.x, y, origin.z);
      Vector3 b = new Vector3(origin.x + w, y, origin.z);
      Vector3 c = new Vector3(origin.x + w, y, origin.z + d);
      Vector3 e = new Vector3(origin.x, y, origin.z + d);
      Gizmos.DrawLine(a, b);
      Gizmos.DrawLine(b, c);
      Gizmos.DrawLine(c, e);
      Gizmos.DrawLine(e, a);

      Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.25f);
      for (int x = 1; x < Width; x++)
      {
        float px = origin.x + x * CellSize;
        Gizmos.DrawLine(new Vector3(px, y, origin.z), new Vector3(px, y, origin.z + d));
      }

      for (int z = 1; z < Depth; z++)
      {
        float pz = origin.z + z * CellSize;
        Gizmos.DrawLine(new Vector3(origin.x, y, pz), new Vector3(origin.x + w, y, pz));
      }
    }
  }
}
