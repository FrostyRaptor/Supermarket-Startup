using System.Collections.Generic;
using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  public class FootprintUtilityTests
  {
    [Test]
    public void NormalizeTurns_WrapsAround()
    {
      Assert.AreEqual(0, FootprintUtility.NormalizeTurns(0));
      Assert.AreEqual(1, FootprintUtility.NormalizeTurns(1));
      Assert.AreEqual(3, FootprintUtility.NormalizeTurns(-1));
      Assert.AreEqual(0, FootprintUtility.NormalizeTurns(4));
      Assert.AreEqual(1, FootprintUtility.NormalizeTurns(5));
    }

    [TestCase(2, 3, 0, 2, 3)]
    [TestCase(2, 3, 1, 3, 2)]
    [TestCase(2, 3, 2, 2, 3)]
    [TestCase(2, 3, 3, 3, 2)]
    [TestCase(1, 1, 1, 1, 1)]
    public void RotatedSize_SwapsOnOddTurns(
      int width,
      int depth,
      int turns,
      int expectedWidth,
      int expectedDepth)
    {
      Vector2Int size = FootprintUtility.RotatedSize(new Vector2Int(width, depth), turns);
      Assert.AreEqual(expectedWidth, size.x);
      Assert.AreEqual(expectedDepth, size.y);
    }

    [Test]
    public void GetCellOffsets_ProducesUniqueFullRotatedRectangle()
    {
      List<GridCoord> cells = new List<GridCoord>();

      for (int turns = 0; turns < 4; turns++)
      {
        FootprintUtility.GetCellOffsets(new Vector2Int(2, 3), turns, cells);

        Assert.AreEqual(6, cells.Count, "Cell count at turn " + turns);

        HashSet<GridCoord> unique = new HashSet<GridCoord>(cells);
        Assert.AreEqual(6, unique.Count, "Duplicate cells at turn " + turns);

        Vector2Int size = FootprintUtility.RotatedSize(new Vector2Int(2, 3), turns);
        for (int i = 0; i < cells.Count; i++)
        {
          Assert.That(cells[i].X, Is.InRange(0, size.x - 1));
          Assert.That(cells[i].Z, Is.InRange(0, size.y - 1));
        }
      }
    }

    [Test]
    public void GetCellOffsets_AnchorsAtMinimumCorner()
    {
      List<GridCoord> cells = new List<GridCoord>();
      Vector2Int footprint = new Vector2Int(2, 1);

      FootprintUtility.GetCellOffsets(footprint, 0, cells);
      CollectionAssert.AreEquivalent(
        new[] { new GridCoord(0, 0), new GridCoord(1, 0) },
        cells,
        "Turn 0");

      FootprintUtility.GetCellOffsets(footprint, 1, cells);
      CollectionAssert.AreEquivalent(
        new[] { new GridCoord(0, 0), new GridCoord(0, 1) },
        cells,
        "Turn 1");

      FootprintUtility.GetCellOffsets(footprint, 2, cells);
      CollectionAssert.AreEquivalent(
        new[] { new GridCoord(0, 0), new GridCoord(1, 0) },
        cells,
        "Turn 2");

      FootprintUtility.GetCellOffsets(footprint, 3, cells);
      CollectionAssert.AreEquivalent(
        new[] { new GridCoord(0, 0), new GridCoord(0, 1) },
        cells,
        "Turn 3");
    }

    [Test]
    public void GetCellOffsets_ClampsZeroFootprintToOneCell()
    {
      List<GridCoord> cells = new List<GridCoord>();
      FootprintUtility.GetCellOffsets(Vector2Int.zero, 0, cells);
      Assert.AreEqual(1, cells.Count);
      Assert.AreEqual(new GridCoord(0, 0), cells[0]);
    }
  }
}
