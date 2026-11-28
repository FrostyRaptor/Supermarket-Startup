using System;
using System.Collections.Generic;
using NUnit.Framework;
using Supermarket.Building;
using UnityEditor;
using UnityEngine;

namespace Supermarket.Tests
{
  public class DefaultBuildLayoutTests
  {
    GameObject _gridObject;
    GameObject _storeObject;
    BuildLayoutStore _store;

    [TearDown]
    public void TearDown()
    {
      if (_store != null)
      {
        PlayerPrefs.DeleteKey(_store.StorageKey);
        PlayerPrefs.DeleteKey(_store.StorageKey + ".corrupt");
        PlayerPrefs.Save();
      }

      if (_storeObject != null) UnityEngine.Object.DestroyImmediate(_storeObject);
      if (_gridObject != null) UnityEngine.Object.DestroyImmediate(_gridObject);
    }

    [Test]
    public void DefaultLayout_PlacesEveryShippedItem()
    {
      PlaceableCatalog catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(
        "Assets/_ScriptableObjects/Building/PlaceableCatalog.asset");
      Assert.IsNotNull(catalog);

      _gridObject = new GameObject("DefaultLayoutTestGrid");
      BuildGrid grid = _gridObject.AddComponent<BuildGrid>();
      grid.size = new Vector2Int(96, 256);
      grid.cellSize = 1f;
      grid.origin = new Vector3(-8f, 0.75f, -8f);
      grid.exclusionZones = StoreBuildZones.CreateDefaultZones();

      _storeObject = new GameObject("DefaultLayoutTestStore");
      _store = _storeObject.AddComponent<BuildLayoutStore>();
      _store.grid = grid;
      _store.catalog = catalog;
      _store.FileName = "default-layout-test-" + Guid.NewGuid().ToString("N");
      _store.loadOnStart = false;
      _store.saveOnChange = false;

      Assert.IsTrue(_store.ApplyDefaultLayout(false));

      List<PlacedObject> placed = new List<PlacedObject>();
      grid.GetAllPlacedObjects(placed);
      Assert.AreEqual(200, placed.Count);
      Assert.IsNotNull(grid.GetOccupant(new GridCoord(20, 1)));
      Assert.IsNotNull(grid.GetOccupant(new GridCoord(7, 77)));
    }
  }
}
