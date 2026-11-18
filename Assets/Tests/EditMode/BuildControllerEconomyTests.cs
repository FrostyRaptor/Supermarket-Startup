using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  public class BuildControllerEconomyTests
  {
    GameObject _gridObject;
    BuildGrid _grid;
    GameObject _controllerObject;
    BuildController _controller;
    GameObject _walletObject;
    SimpleWallet _wallet;
    PlaceableCatalog _catalog;
    PlaceableDefinition _definition;
    GameObject _prefab;

    [SetUp]
    public void SetUp()
    {
      _gridObject = new GameObject("TestGrid");
      _grid = _gridObject.AddComponent<BuildGrid>();
      _grid.size = new Vector2Int(16, 16);
      _grid.cellSize = 1f;
      _grid.origin = new Vector3(-8f, 0.75f, -8f);

      _prefab = new GameObject("TestPrefab");
      _prefab.AddComponent<PlacedObject>();

      _definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
      _definition.id = "test_1x1";
      _definition.displayName = "Test";
      _definition.prefab = _prefab;
      _definition.footprint = Vector2Int.one;
      _definition.cost = 100;

      _catalog = ScriptableObject.CreateInstance<PlaceableCatalog>();
      _catalog.definitions.Add(_definition);
      _catalog.RebuildLookup();

      _walletObject = new GameObject("TestWallet");
      _wallet = _walletObject.AddComponent<SimpleWallet>();
      _wallet.Initialize(1000);

      _controllerObject = new GameObject("TestController");
      _controller = _controllerObject.AddComponent<BuildController>();
      _controller.grid = _grid;
      _controller.catalog = _catalog;
      _controller.economySource = _wallet;
      _controller.SelectDefinition(0);
    }

    [TearDown]
    public void TearDown()
    {
      if (_controllerObject != null) Object.DestroyImmediate(_controllerObject);
      if (_grid != null) _grid.ClearAll();
      if (_gridObject != null) Object.DestroyImmediate(_gridObject);
      if (_walletObject != null) Object.DestroyImmediate(_walletObject);
      if (_prefab != null) Object.DestroyImmediate(_prefab);
      if (_definition != null) Object.DestroyImmediate(_definition);
      if (_catalog != null) Object.DestroyImmediate(_catalog);
    }

    [Test]
    public void ValidPlacement_SpendsExactlyOnce()
    {
      Assert.IsTrue(_controller.TryPlaceSelected(new GridCoord(0, 0)));

      Assert.AreEqual(900, _wallet.Balance);
      Assert.IsNotNull(_grid.GetOccupant(new GridCoord(0, 0)));
    }

    [Test]
    public void InvalidPlacement_DoesNotSpend()
    {
      Assert.IsNotNull(_grid.Place(_definition, new GridCoord(0, 0), 0));

      Assert.IsFalse(_controller.TryPlaceSelected(new GridCoord(0, 0)));

      Assert.AreEqual(1000, _wallet.Balance);
    }

    [Test]
    public void InsufficientFunds_LeavesNoObjectAndNoCharge()
    {
      _wallet.Initialize(50);

      Assert.IsFalse(_controller.TryPlaceSelected(new GridCoord(2, 2)));

      Assert.IsNull(_grid.GetOccupant(new GridCoord(2, 2)));
      Assert.AreEqual(50, _wallet.Balance);
    }

    [Test]
    public void CanAfford_IsNonDestructive()
    {
      Assert.IsTrue(_wallet.CanAfford(1000));
      Assert.IsFalse(_wallet.CanAfford(1001));
      Assert.AreEqual(1000, _wallet.Balance);
    }
  }
}
