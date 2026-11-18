using System.Collections.Generic;
using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  public class BuildGridPlacementTests
  {
    GameObject _gridObject;
    BuildGrid _grid;
    readonly List<Object> _cleanup = new List<Object>();

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

      for (int i = 0; i < _cleanup.Count; i++)
      {
        if (_cleanup[i] != null) Object.DestroyImmediate(_cleanup[i]);
      }

      _cleanup.Clear();

      if (_gridObject != null) Object.DestroyImmediate(_gridObject);
    }

    PlaceableDefinition CreateDefinition(Vector2Int footprint)
    {
      GameObject prefab = new GameObject("TestPrefab");
      prefab.AddComponent<PlacedObject>();
      _cleanup.Add(prefab);

      PlaceableDefinition definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
      definition.id = string.Format("test_{0}x{1}_{2}", footprint.x, footprint.y, _cleanup.Count);
      definition.displayName = "Test";
      definition.prefab = prefab;
      definition.footprint = footprint;
      _cleanup.Add(definition);

      return definition;
    }

    [Test]
    public void Place_MarksAllFootprintCells()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));
      PlacedObject placed = _grid.Place(definition, new GridCoord(0, 0), 0);

      Assert.IsNotNull(placed);
      Assert.AreSame(placed, _grid.GetOccupant(new GridCoord(0, 0)));
      Assert.AreSame(placed, _grid.GetOccupant(new GridCoord(1, 0)));
      Assert.IsNull(_grid.GetOccupant(new GridCoord(2, 0)));
      Assert.IsNull(_grid.GetOccupant(new GridCoord(0, 1)));
    }

    [Test]
    public void Place_RejectsOverlap()
    {
      PlaceableDefinition first = CreateDefinition(Vector2Int.one);
      PlaceableDefinition second = CreateDefinition(Vector2Int.one);

      Assert.IsNotNull(_grid.Place(first, new GridCoord(3, 3), 0));
      Assert.AreEqual(PlacementResult.Occupied, _grid.CanPlace(second, new GridCoord(3, 3), 0));
      Assert.IsNull(_grid.Place(second, new GridCoord(3, 3), 0));
    }

    [Test]
    public void Place_RejectsPartialOutOfBounds()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));

      Assert.AreEqual(PlacementResult.OutOfBounds, _grid.CanPlace(definition, new GridCoord(15, 0), 0));
      Assert.IsNull(_grid.Place(definition, new GridCoord(15, 0), 0));
    }

    [Test]
    public void Place_RejectsInvalidDefinition()
    {
      PlaceableDefinition definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
      definition.id = "no_prefab";
      _cleanup.Add(definition);

      Assert.AreEqual(PlacementResult.InvalidDefinition, _grid.CanPlace(definition, new GridCoord(0, 0), 0));
      Assert.IsNull(_grid.Place(definition, new GridCoord(0, 0), 0));
    }

    [Test]
    public void Rotation_SwapsFootprintExtent()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));
      PlacedObject placed = _grid.Place(definition, new GridCoord(0, 0), 1);

      Assert.IsNotNull(placed);
      Assert.AreSame(placed, _grid.GetOccupant(new GridCoord(0, 0)));
      Assert.AreSame(placed, _grid.GetOccupant(new GridCoord(0, 1)));
      Assert.IsNull(_grid.GetOccupant(new GridCoord(1, 0)));
    }

    [Test]
    public void PlacementPosition_RotatesPivotOffset()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));
      definition.pivotOffset = new Vector3(1f, 0f, 0f);

      Vector3 unrotated = _grid.GetPlacementPosition(definition, new GridCoord(0, 0), 0);
      Assert.AreEqual(-6f, unrotated.x, 1e-4f);
      Assert.AreEqual(-7.5f, unrotated.z, 1e-4f);

      Vector3 rotated = _grid.GetPlacementPosition(definition, new GridCoord(0, 0), 1);
      Assert.AreEqual(-7.5f, rotated.x, 1e-4f);
      Assert.AreEqual(-8f, rotated.z, 1e-4f);
    }

    [Test]
    public void Remove_FreesAllCells()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));
      PlacedObject placed = _grid.Place(definition, new GridCoord(4, 4), 0);

      Assert.IsTrue(_grid.Remove(placed));
      Assert.IsNull(_grid.GetOccupant(new GridCoord(4, 4)));
      Assert.IsNull(_grid.GetOccupant(new GridCoord(5, 4)));
      Assert.AreEqual(PlacementResult.Valid, _grid.CanPlace(definition, new GridCoord(4, 4), 0));
    }

    [Test]
    public void SizeChange_IsRefusedWhileObjectsArePlaced()
    {
      PlaceableDefinition definition = CreateDefinition(Vector2Int.one);
      Assert.IsNotNull(_grid.Place(definition, new GridCoord(0, 0), 0));

      _grid.size = new Vector2Int(8, 8);
      _grid.GetOccupant(new GridCoord(7, 7));

      Assert.AreEqual(new Vector2Int(16, 16), _grid.Size);
    }

    [Test]
    public void ClearAll_RemovesEverything()
    {
      PlaceableDefinition definition = CreateDefinition(Vector2Int.one);
      _grid.Place(definition, new GridCoord(0, 0), 0);
      _grid.Place(definition, new GridCoord(5, 5), 0);

      _grid.ClearAll();

      List<PlacedObject> placed = new List<PlacedObject>();
      _grid.GetAllPlacedObjects(placed);
      Assert.AreEqual(0, placed.Count);
      Assert.IsNull(_grid.GetOccupant(new GridCoord(0, 0)));
    }
  }
}
