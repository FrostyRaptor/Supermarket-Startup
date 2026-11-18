using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Footprint rotation math. A footprint is expressed as (width along X, depth along Z),
  /// anchored at its minimum corner. Rotations are clockwise when viewed from above,
  /// matching Quaternion.Euler(0, turns * 90, 0).
  /// </summary>
  public static class FootprintUtility
  {
    public static int NormalizeTurns(int turns)
    {
      return ((turns % 4) + 4) % 4;
    }

    public static Vector2Int RotatedSize(Vector2Int footprint, int turns)
    {
      int w = Mathf.Max(1, footprint.x);
      int h = Mathf.Max(1, footprint.y);
      return NormalizeTurns(turns) % 2 == 0
        ? new Vector2Int(w, h)
        : new Vector2Int(h, w);
    }

    /// <summary>
    /// Fills <paramref name="results"/> with the cell offsets occupied by the footprint,
    /// relative to its minimum corner.
    /// </summary>
    public static void GetCellOffsets(Vector2Int footprint, int turns, List<GridCoord> results)
    {
      if (results == null) throw new ArgumentNullException(nameof(results));

      results.Clear();

      int w = Mathf.Max(1, footprint.x);
      int h = Mathf.Max(1, footprint.y);
      int normalized = NormalizeTurns(turns);

      for (int x = 0; x < w; x++)
      {
        for (int z = 0; z < h; z++)
        {
          GridCoord cell;
          switch (normalized)
          {
            case 1:
              cell = new GridCoord(z, w - 1 - x);
              break;
            case 2:
              cell = new GridCoord(w - 1 - x, h - 1 - z);
              break;
            case 3:
              cell = new GridCoord(h - 1 - z, x);
              break;
            default:
              cell = new GridCoord(x, z);
              break;
          }

          results.Add(cell);
        }
      }
    }
  }
}
