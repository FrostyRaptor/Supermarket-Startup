using System.Collections.Generic;

namespace Supermarket.Building
{
  public static class PlacementValidator
  {
    /// <summary>
    /// Validates a footprint against grid bounds and current occupancy.
    /// The passed buffer is reused to avoid allocations.
    /// </summary>
    public static PlacementResult Validate(
      BuildGrid grid,
      PlaceableDefinition definition,
      GridCoord origin,
      int turns,
      List<GridCoord> cellBuffer)
    {
      return Validate((IGridOccupancy)grid, definition, origin, turns, cellBuffer);
    }

    /// <summary>
    /// Validates a footprint against grid bounds and current occupancy using the
    /// grid abstraction. The passed buffer is reused to avoid allocations.
    /// </summary>
    public static PlacementResult Validate(
      IGridOccupancy grid,
      PlaceableDefinition definition,
      GridCoord origin,
      int turns,
      List<GridCoord> cellBuffer)
    {
      if (grid == null || definition == null || definition.prefab == null || !definition.IsValid)
      {
        return PlacementResult.InvalidDefinition;
      }

      if (cellBuffer == null)
      {
        return PlacementResult.InvalidDefinition;
      }

      FootprintUtility.GetCellOffsets(definition.Footprint, turns, cellBuffer);

      for (int i = 0; i < cellBuffer.Count; i++)
      {
        GridCoord cell = origin + cellBuffer[i];
        if (!grid.IsInside(cell))
        {
          // Distinguish a walled store section from a point outside the plot.
          bool withinPlot = cell.X >= 0 && cell.Z >= 0 && cell.X < grid.Width && cell.Z < grid.Depth;
          return withinPlot ? PlacementResult.BlockedArea : PlacementResult.OutOfBounds;
        }
        if (grid.AreaAt(cell) != definition.allowedArea) return PlacementResult.WrongSection;
        if (grid.GetOccupant(cell) != null) return PlacementResult.Occupied;
      }

      return PlacementResult.Valid;
    }
  }
}
