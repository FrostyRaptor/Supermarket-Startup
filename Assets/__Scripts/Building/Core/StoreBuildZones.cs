using System;
using System.Collections.Generic;
using Supermarket.Config;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>How a world-space build zone treats placements.</summary>
  public enum BuildZoneAccess
  {
    /// <summary>No placeables are allowed (walled sections such as bakery).</summary>
    Blocked,
    /// <summary>Only checkout lanes are allowed (checkout section).</summary>
    CheckoutOnly
  }

  /// <summary>
  /// A world-space XZ rectangle that is part of the store floor. Blocked zones
  /// are walled sections; checkout-only zones accept only checkout lanes.
  /// Bounds are half-open: min is inclusive, max is exclusive.
  /// </summary>
  [Serializable]
  public class BuildExclusionZone
  {
    public StoreSectionType section;
    public BuildZoneAccess access = BuildZoneAccess.Blocked;
    public Vector2 min;
    public Vector2 max;

    public BuildExclusionZone() { }

    public BuildExclusionZone(StoreSectionType section, float minX, float minZ, float maxX, float maxZ)
      : this(section, minX, minZ, maxX, maxZ, BuildZoneAccess.Blocked)
    {
    }

    public BuildExclusionZone(
      StoreSectionType section,
      float minX,
      float minZ,
      float maxX,
      float maxZ,
      BuildZoneAccess zoneAccess)
    {
      this.section = section;
      access = zoneAccess;
      min = new Vector2(minX, minZ);
      max = new Vector2(maxX, maxZ);
    }

    public bool IsBlocked
    {
      get { return access == BuildZoneAccess.Blocked; }
    }

    public bool ContainsWorld(Vector3 world)
    {
      return world.x >= min.x && world.x < max.x &&
             world.z >= min.y && world.z < max.y;
    }

    public bool ContainsCell(BuildGrid grid, GridCoord cell)
    {
      return grid != null && ContainsWorld(grid.CoordToWorldCenter(cell));
    }
  }

  /// <summary>
  /// Default build zones for the current __Scene_0 floor layout. The floor is
  /// six 16 m columns by sixteen 16 m rows, starting at (-8, -8). Walled
  /// sections are blocked; checkout is checkout-lane-only. Shared by editor
  /// setup and tests so the scene and EditMode checks agree.
  /// </summary>
  public static class StoreBuildZones
  {
    public static List<BuildExclusionZone> CreateDefaultZones()
    {
      const float left = -8f;
      const float right = 88f;
      const float front = -8f;
      const float back = 248f;
      const float tile = 16f;

      // Floor tile columns: [-8,8) [8,24) [24,40) [40,56) [56,72) [72,88)
      float x0 = left;
      float x1 = x0 + tile; // 8
      float x2 = x1 + tile; // 24
      float x3 = x2 + tile; // 40
      float x4 = x3 + tile; // 56
      float x5 = x4 + tile; // 72

      // Floor tile rows start at -8 and advance by 16 m.
      float bakeryBack = front + tile * 2f;   // 24
      float deliFront = front + tile * 3f;    // 40
      float deliBack = front + tile * 5f;     // 72
      float butcherBack = front + tile * 4f;  // 56
      float checkoutFront = front + tile * 4f; // 56
      float alcoholFront = front + tile * 12f; // 184
      float storageFront = front + tile * 13f; // 200

      return new List<BuildExclusionZone>
      {
        // Columns 0-1, rows 0-1: corner enclosed by the bakery walls.
        new BuildExclusionZone(StoreSectionType.Bakery, left, front, x2, bakeryBack),

        // Columns 4-5, rows 0-3: corner enclosed by the butcher walls.
        new BuildExclusionZone(StoreSectionType.Butcher, x4, front, right, butcherBack),

        // Column 2, rows 3-4: fully walled deli room.
        new BuildExclusionZone(StoreSectionType.Deli, x2, deliFront, x3, deliBack),

        // Columns 4-5 behind the butcher, plus column 3 at the back.
        new BuildExclusionZone(StoreSectionType.Storage, x4, butcherBack, right, back),
        new BuildExclusionZone(StoreSectionType.Storage, x3, storageFront, x4, back),

        // Column 0, rows 4-11: checkout. Only 4x3 checkout lanes may go here.
        new BuildExclusionZone(
          StoreSectionType.Checkout, left, checkoutFront, x1, alcoholFront,
          BuildZoneAccess.CheckoutOnly),

        // Column 0, rows 12-15: walled alcohol section.
        new BuildExclusionZone(StoreSectionType.Alcohol, left, alcoholFront, x1, back)
      };
    }

    /// <summary>Replaces a grid's mask with the default store layout.</summary>
    public static void ApplyTo(BuildGrid grid)
    {
      if (grid != null) grid.exclusionZones = CreateDefaultZones();
    }
  }
}
