using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Read/query and occupancy contract for a rectangular build grid.
  /// Allows validators, controllers, and persistence to depend on an abstraction.
  /// </summary>
  public interface IGridOccupancy
  {
    event Action<PlacedObject> ObjectPlaced;
    event Action<PlacedObject> ObjectRemoved;

    Vector2Int Size { get; }
    float CellSize { get; }
    Vector3 Origin { get; }
    int Width { get; }
    int Depth { get; }

    bool IsInside(GridCoord cell);
    Vector3 CoordToWorldCorner(GridCoord cell);
    Vector3 CoordToWorldCenter(GridCoord cell);
    GridCoord WorldToCoord(Vector3 world);
    Vector3 GetFootprintCenter(GridCoord originCell, Vector2Int rotatedSize);
    Vector3 GetPlacementPosition(PlaceableDefinition definition, GridCoord originCell, int turns);
    Quaternion GetPlacementRotation(int turns);

    PlacedObject GetOccupant(GridCoord cell);
    PlacementResult CanPlace(PlaceableDefinition definition, GridCoord originCell, int turns);

    PlacedObject Place(PlaceableDefinition definition, GridCoord originCell, int turns);
    bool Remove(PlacedObject placed);
    void ClearAll();
    void GetAllPlacedObjects(List<PlacedObject> results);
  }
}
