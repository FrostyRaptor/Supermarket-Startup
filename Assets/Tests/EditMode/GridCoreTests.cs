using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  public class GridCoreTests
  {
    GameObject _gridObject;
    BuildGrid _grid;

    [SetUp]
    public void SetUp()
    {
      _gridObject = new GameObject("TestGrid");
      _grid = _gridObject.AddComponent<BuildGrid>();
      _grid.size = new Vector2Int(16, 16);
      _grid.cellSize = 1f;
      _grid.origin = new Vector3(-8f, 0.75f, -8f);
    }

    [TearDown]
    public void TearDown()
    {
      if (_grid != null) _grid.ClearAll();
      if (_gridObject != null) Object.DestroyImmediate(_gridObject);
    }

    [Test]
    public void WorldToCoord_MapsCornersAndEdges()
    {
      Assert.AreEqual(new GridCoord(0, 0), _grid.WorldToCoord(new Vector3(-8f, 0.75f, -8f)));
      Assert.AreEqual(new GridCoord(0, 0), _grid.WorldToCoord(new Vector3(-7.001f, 0.75f, -7.001f)));
      Assert.AreEqual(new GridCoord(15, 15), _grid.WorldToCoord(new Vector3(7.999f, 0.75f, 7.999f)));
      Assert.AreEqual(new GridCoord(16, 16), _grid.WorldToCoord(new Vector3(8f, 0.75f, 8f)));

      Assert.IsTrue(_grid.IsInside(new GridCoord(15, 15)));
      Assert.IsFalse(_grid.IsInside(new GridCoord(16, 16)));
      Assert.IsFalse(_grid.IsInside(new GridCoord(-1, 0)));
    }

    [Test]
    public void CoordToWorldCenter_RoundTrips()
    {
      Assert.AreEqual(new Vector3(-7.5f, 0.75f, -7.5f), _grid.CoordToWorldCenter(new GridCoord(0, 0)));

      for (int x = 0; x < 16; x += 5)
      {
        for (int z = 0; z < 16; z += 5)
        {
          GridCoord cell = new GridCoord(x, z);
          Assert.AreEqual(cell, _grid.WorldToCoord(_grid.CoordToWorldCenter(cell)));
        }
      }
    }

    [Test]
    public void CoordToWorldCorner_IsMinimumCorner()
    {
      Assert.AreEqual(new Vector3(-8f, 0.75f, -8f), _grid.CoordToWorldCorner(new GridCoord(0, 0)));
      Assert.AreEqual(new Vector3(-6f, 0.75f, -5f), _grid.CoordToWorldCorner(new GridCoord(2, 3)));
    }

    [Test]
    public void SizeAndCellCount_ClampToOne()
    {
      _grid.size = new Vector2Int(0, -4);
      Assert.AreEqual(1, _grid.Width);
      Assert.AreEqual(1, _grid.Depth);
      Assert.AreEqual(1, _grid.CellCount);
    }
  }
}
