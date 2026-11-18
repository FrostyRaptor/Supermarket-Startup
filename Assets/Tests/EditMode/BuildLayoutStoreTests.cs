using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  public class BuildLayoutStoreTests
  {
    GameObject _gridObject;
    BuildGrid _grid;
    GameObject _storeObject;
    BuildLayoutStore _store;
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
      _definition.id = "test_2x1";
      _definition.displayName = "Test";
      _definition.prefab = _prefab;
      _definition.footprint = new Vector2Int(2, 1);

      _catalog = ScriptableObject.CreateInstance<PlaceableCatalog>();
      _catalog.definitions.Add(_definition);
      _catalog.RebuildLookup();

      _storeObject = new GameObject("TestStore");
      _store = _storeObject.AddComponent<BuildLayoutStore>();
      _store.grid = _grid;
      _store.catalog = _catalog;
      _store.FileName = "test-layout-" + System.Guid.NewGuid().ToString("N");
      _store.saveOnChange = false;
      _store.loadOnStart = false;
    }

    [TearDown]
    public void TearDown()
    {
      PlayerPrefs.DeleteKey(_store.StorageKey);
      PlayerPrefs.DeleteKey(_store.StorageKey + ".corrupt");
      PlayerPrefs.Save();

      if (_storeObject != null) Object.DestroyImmediate(_storeObject);
      if (_grid != null) _grid.ClearAll();
      if (_gridObject != null) Object.DestroyImmediate(_gridObject);
      if (_prefab != null) Object.DestroyImmediate(_prefab);
      if (_definition != null) Object.DestroyImmediate(_definition);
      if (_catalog != null) Object.DestroyImmediate(_catalog);
    }

    [Test]
    public void SaveThenLoad_RestoresLayout()
    {
      PlacedObject placed = _grid.Place(_definition, new GridCoord(3, 4), 1);
      Assert.IsNotNull(placed);
      Assert.IsTrue(_store.Save());

      _grid.ClearAll();
      Assert.IsNull(_grid.GetOccupant(new GridCoord(3, 4)));

      Assert.IsTrue(_store.Load());

      PlacedObject restored = _grid.GetOccupant(new GridCoord(3, 4));
      Assert.IsNotNull(restored);
      Assert.AreEqual(1, restored.QuarterTurns);
      Assert.AreSame(restored, _grid.GetOccupant(new GridCoord(3, 5)));
    }

    [Test]
    public void Load_CorruptJson_QuarantinesAndLeavesGridUntouched()
    {
      PlacedObject placed = _grid.Place(_definition, new GridCoord(1, 1), 0);
      PlayerPrefs.SetString(_store.StorageKey, "{ this is not valid json");
      PlayerPrefs.Save();

      Assert.IsFalse(_store.Load());

      Assert.AreSame(placed, _grid.GetOccupant(new GridCoord(1, 1)));
      Assert.IsTrue(PlayerPrefs.HasKey(_store.StorageKey + ".corrupt"));
      Assert.IsFalse(PlayerPrefs.HasKey(_store.StorageKey));
    }

    [Test]
    public void Load_UnsupportedVersion_IsRejected()
    {
      PlayerPrefs.SetString(_store.StorageKey, "{\"version\":99,\"objects\":[]}");
      PlayerPrefs.Save();

      Assert.IsFalse(_store.Load());
    }

    [Test]
    public void Load_SkipsUnknownIds_AndRestoresKnownOnes()
    {
      PlayerPrefs.SetString(_store.StorageKey,
        "{\"version\":1,\"objects\":[" +
        "{\"id\":\"missing\",\"x\":1,\"z\":1,\"rotation\":0}," +
        "{\"id\":\"test_2x1\",\"x\":4,\"z\":4,\"rotation\":0}]}");
      PlayerPrefs.Save();

      Assert.IsTrue(_store.Load());

      Assert.IsNull(_grid.GetOccupant(new GridCoord(1, 1)));
      Assert.IsNotNull(_grid.GetOccupant(new GridCoord(4, 4)));
    }

    [Test]
    public void SaveNow_RoundTripsAfterClear()
    {
      _grid.Place(_definition, new GridCoord(6, 6), 0);

      Assert.IsTrue(_store.SaveNow());
      Assert.IsTrue(PlayerPrefs.HasKey(_store.StorageKey));

      _grid.ClearAll();
      Assert.IsTrue(_store.Load());
      Assert.IsNotNull(_grid.GetOccupant(new GridCoord(6, 6)));
    }
  }
}
