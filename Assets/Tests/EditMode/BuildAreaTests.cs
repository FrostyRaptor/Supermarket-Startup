using System.Collections.Generic;
using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  /// <summary>
  /// The build grid covers the store floor, blocks walled sections, and keeps
  /// checkout lanes confined to the checkout section. These checks use the same
  /// mask as scene setup.
  /// </summary>
  public class BuildAreaTests
  {
    GameObject _gridObject;
    BuildGrid _grid;
    readonly List<Object> _cleanup = new List<Object>();

    [SetUp]
    public void SetUp()
    {
      _gridObject = new GameObject("TestStoreGrid");
      _grid = _gridObject.AddComponent<BuildGrid>();
      _grid.size = new Vector2Int(96, 256);
      _grid.cellSize = 1f;
      _grid.origin = new Vector3(-8f, 0.75f, -8f);
      StoreBuildZones.ApplyTo(_grid);
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

    PlaceableDefinition CreateDefinition(
      Vector2Int footprint,
      PlaceableArea allowedArea = PlaceableArea.General)
    {
      GameObject prefab = new GameObject("TestPrefab");
      prefab.AddComponent<PlacedObject>();
      _cleanup.Add(prefab);

      PlaceableDefinition definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
      definition.id = "test_area_" + _cleanup.Count;
      definition.displayName = "Test";
      definition.prefab = prefab;
      definition.footprint = footprint;
      definition.allowedArea = allowedArea;
      _cleanup.Add(definition);
      return definition;
    }

    static GridCoord CellAt(float worldX, float worldZ)
    {
      return new GridCoord(Mathf.FloorToInt(worldX + 8f), Mathf.FloorToInt(worldZ + 8f));
    }

    [Test]
    public void Grid_CoversWholeShopFloor()
    {
      Assert.AreEqual(96, _grid.Width);
      Assert.AreEqual(256, _grid.Depth);
      Assert.AreEqual(new GridCoord(0, 0), _grid.WorldToCoord(new Vector3(-8f, 0.75f, -8f)));
      Assert.AreEqual(new GridCoord(96, 256), _grid.WorldToCoord(new Vector3(88f, 0.75f, 248f)));
    }

    [Test]
    public void WalledSections_AreNotBuildable()
    {
      Assert.IsFalse(_grid.IsInside(CellAt(-4f, -4f)), "bakery");
      Assert.IsFalse(_grid.IsInside(CellAt(60f, 0f)), "butcher");
      Assert.IsFalse(_grid.IsInside(CellAt(30f, 55f)), "deli");
      Assert.IsFalse(_grid.IsInside(CellAt(70f, 100f)), "storage");
      Assert.IsFalse(_grid.IsInside(CellAt(45f, 210f)), "storage back column");
      Assert.IsFalse(_grid.IsInside(CellAt(0f, 200f)), "alcohol");
    }

    [Test]
    public void GeneralAndCheckout_AreInsidePlot()
    {
      Assert.IsTrue(_grid.IsInside(CellAt(20f, 30f)), "general near entrance");
      Assert.IsTrue(_grid.IsInside(CellAt(20f, 100f)), "general mid store");
      Assert.IsTrue(_grid.IsInside(CellAt(0f, 100f)), "checkout lane area");
      Assert.IsTrue(_grid.IsInside(CellAt(40f, 150f)), "general aisle");
    }

    [Test]
    public void AreaAt_SplitsCheckoutFromGeneral()
    {
      Assert.AreEqual(PlaceableArea.Checkout, _grid.AreaAt(CellAt(0f, 100f)));
      Assert.AreEqual(PlaceableArea.Checkout, _grid.AreaAt(CellAt(-7f, 60f)));
      Assert.AreEqual(PlaceableArea.General, _grid.AreaAt(CellAt(8f, 100f)));
      Assert.AreEqual(PlaceableArea.General, _grid.AreaAt(CellAt(20f, 100f)));
      Assert.AreEqual(PlaceableArea.General, _grid.AreaAt(CellAt(0f, 50f)));
      Assert.AreEqual(PlaceableArea.General, _grid.AreaAt(CellAt(0f, 190f)));
    }

    [Test]
    public void Placement_InExcludedSection_IsRejected()
    {
      PlaceableDefinition definition = CreateDefinition(Vector2Int.one);

      Assert.AreEqual(PlacementResult.BlockedArea, _grid.CanPlace(definition, CellAt(-4f, -4f), 0));
      Assert.IsNull(_grid.Place(definition, CellAt(-4f, -4f), 0));
    }

    [Test]
    public void Placement_SpanningIntoExcludedSection_IsRejected()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));
      GridCoord origin = CellAt(55.5f, 12.5f); // last general cell before the butcher wall

      Assert.IsTrue(_grid.IsInside(origin));
      Assert.IsFalse(_grid.IsInside(origin + new GridCoord(1, 0)));
      Assert.AreEqual(PlacementResult.BlockedArea, _grid.CanPlace(definition, origin, 0));
      Assert.IsNull(_grid.Place(definition, origin, 0));
    }

    [Test]
    public void Placement_OnGeneralFloor_StillWorks()
    {
      PlaceableDefinition definition = CreateDefinition(new Vector2Int(2, 1));
      GridCoord origin = CellAt(20f, 100f);

      PlacedObject placed = _grid.Place(definition, origin, 0);

      Assert.IsNotNull(placed);
      Assert.AreSame(placed, _grid.GetOccupant(origin));
      Assert.AreSame(placed, _grid.GetOccupant(origin + new GridCoord(1, 0)));
    }

    [Test]
    public void CheckoutLane_4x3_PlacesInsideCheckoutOnly()
    {
      // Plan: checkout lanes are 4 x 3 cells.
      PlaceableDefinition lane = CreateDefinition(new Vector2Int(4, 3), PlaceableArea.Checkout);
      GridCoord checkoutOrigin = CellAt(-7.5f, 60f);

      Assert.AreEqual(PlacementResult.Valid, _grid.CanPlace(lane, checkoutOrigin, 0));
      Assert.AreEqual(PlacementResult.Valid, _grid.CanPlace(lane, checkoutOrigin, 1));
      Assert.IsNotNull(_grid.Place(lane, checkoutOrigin, 0));

      Assert.AreEqual(PlacementResult.WrongSection, _grid.CanPlace(lane, CellAt(20f, 100f), 0));
      Assert.IsNull(_grid.Place(lane, CellAt(20f, 100f), 0));
    }

    [Test]
    public void CheckoutLane_CannotSpanOutOfCheckout()
    {
      PlaceableDefinition lane = CreateDefinition(new Vector2Int(4, 3), PlaceableArea.Checkout);
      GridCoord origin = CellAt(6f, 100f); // four cells wide crosses x = 8

      Assert.AreEqual(PlaceableArea.Checkout, _grid.AreaAt(origin));
      Assert.AreEqual(PlaceableArea.General, _grid.AreaAt(origin + new GridCoord(2, 0)));
      Assert.AreEqual(PlacementResult.WrongSection, _grid.CanPlace(lane, origin, 0));
      Assert.IsNull(_grid.Place(lane, origin, 0));
    }

    [Test]
    public void GeneralItems_CannotBePlacedInCheckout()
    {
      PlaceableDefinition shelf = CreateDefinition(new Vector2Int(2, 1));

      Assert.AreEqual(PlacementResult.WrongSection, _grid.CanPlace(shelf, CellAt(0f, 100f), 0));
      Assert.IsNull(_grid.Place(shelf, CellAt(0f, 100f), 0));

      // Spanning from general into checkout is still rejected.
      GridCoord boundary = CellAt(7f, 100f);
      Assert.AreEqual(PlacementResult.WrongSection, _grid.CanPlace(shelf, boundary, 0));
      Assert.AreEqual(PlacementResult.Valid, _grid.CanPlace(shelf, CellAt(20f, 100f), 0));
    }
  }
}
