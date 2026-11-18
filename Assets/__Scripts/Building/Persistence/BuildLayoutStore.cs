using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Saves and restores the placed-object layout through an
  /// <see cref="IBuildLayoutStorage"/> backend (WebGL-safe PlayerPrefs by default).
  /// The payload is small, and PlayerPrefs avoids blocking file I/O and is flushed
  /// to IndexedDB when saved.
  /// </summary>
  public class BuildLayoutStore : MonoBehaviour
  {
    public const string DefaultFileName = "build-layout.json";
    public const int CurrentVersion = 1;

    const string KeyPrefix = "supermarket.build-layout.";

    [Header("References")]
    public BuildGrid grid;
    public PlaceableCatalog catalog;

    [Header("Persistence")]
    [SerializeField] string fileName = DefaultFileName;
    public bool loadOnStart = true;
    public bool saveOnChange = true;

    [Header("Optional Services")]
    [Tooltip("Optional. A component implementing IBuildLayoutStorage for custom backends/slots.")]
    public MonoBehaviour storageSource;

    readonly List<PlacedObject> _scratch = new List<PlacedObject>();
    readonly List<BuildLayout.BuildLayoutRecord> _validRecords = new List<BuildLayout.BuildLayoutRecord>();

    bool _dirty;
    bool _loading;
    IBuildLayoutStorage _storage;

    /// <summary>Storage backend used for persistence. Defaults to PlayerPrefs.</summary>
    public IBuildLayoutStorage Storage
    {
      get { return _storage ?? ResolveStorage(); }
      set { _storage = value; }
    }

    IBuildLayoutStorage ResolveStorage()
    {
      IBuildLayoutStorage storage = storageSource as IBuildLayoutStorage;
      if (storage == null) storage = PlayerPrefsBuildLayoutStorage.Instance;
      _storage = storage;
      return storage;
    }

    /// <summary>Key that holds this store's JSON payload in the configured storage backend.</summary>
    public string StorageKey
    {
      get { return KeyPrefix + fileName; }
    }

    public string FileName
    {
      get { return fileName; }
      set { fileName = string.IsNullOrEmpty(value) ? DefaultFileName : value; }
    }

    /// <summary>Path used by older builds that wrote a JSON file to persistentDataPath.</summary>
    public string LegacyFilePath
    {
      get { return Application.persistentDataPath + "/" + fileName; }
    }

    void Start()
    {
      if (grid == null) grid = FindFirstObjectByType<BuildGrid>();
      if (_storage == null) ResolveStorage();

      if (loadOnStart)
      {
        if (catalog == null) Debug.LogWarning("BuildLayoutStore: no catalog assigned; skipping layout load.");
        else Load();
      }

      if (grid != null)
      {
        grid.ObjectPlaced += OnObjectChanged;
        grid.ObjectRemoved += OnObjectChanged;
      }
    }

    void OnDestroy()
    {
      if (grid != null)
      {
        grid.ObjectPlaced -= OnObjectChanged;
        grid.ObjectRemoved -= OnObjectChanged;
      }
    }

    void OnApplicationQuit()
    {
      if (_dirty) Save();
    }

    void OnApplicationPause(bool paused)
    {
      if (paused && _dirty) Save();
    }

    void OnApplicationFocus(bool focused)
    {
      if (!focused && _dirty) Save();
    }

    void OnObjectChanged(PlacedObject placed)
    {
      if (_loading) return;

      _dirty = true;
      if (saveOnChange) Save();
    }

    /// <summary>Forces a save even when nothing changed. Returns true on success.</summary>
    public bool SaveNow()
    {
      _dirty = true;
      return Save();
    }

    /// <summary>Serializes the current layout to the storage backend. Returns true on success.</summary>
    public bool Save()
    {
      if (grid == null) return false;

      BuildLayout layout = new BuildLayout();
      layout.version = CurrentVersion;
      grid.GetAllPlacedObjects(_scratch);

      for (int i = 0; i < _scratch.Count; i++)
      {
        PlacedObject placed = _scratch[i];
        if (placed == null || placed.Definition == null) continue;

        layout.objects.Add(new BuildLayout.BuildLayoutRecord
        {
          id = placed.Definition.id,
          x = placed.Origin.X,
          z = placed.Origin.Z,
          rotation = placed.QuarterTurns
        });
      }

      try
      {
        IBuildLayoutStorage storage = Storage;
        storage.Save(StorageKey, JsonUtility.ToJson(layout, true));
        storage.Flush();
        _dirty = false;
        return true;
      }
      catch (Exception e)
      {
        // Bounded retry: clear the flag so we do not log every frame.
        _dirty = false;
        Debug.LogError(string.Format(
          "BuildLayoutStore: failed to save layout to storage key '{0}': {1}",
          StorageKey, e.Message));
        return false;
      }
    }

    /// <summary>Restores the saved layout. Corrupt data is quarantined and ignored.</summary>
    public bool Load()
    {
      if (grid == null || catalog == null) return false;

      IBuildLayoutStorage storage = Storage;
      if (!storage.Has(StorageKey)) return false;

      string json = storage.Load(StorageKey);
      if (string.IsNullOrEmpty(json)) return false;

      BuildLayout layout;
      try
      {
        layout = JsonUtility.FromJson<BuildLayout>(json);
      }
      catch (Exception e)
      {
        Quarantine(json, "could not parse saved layout: " + e.Message);
        return false;
      }

      if (layout == null || layout.objects == null)
      {
        Quarantine(json, "saved layout is empty or malformed.");
        return false;
      }

      if (layout.version != CurrentVersion)
      {
        Debug.LogWarning(string.Format(
          "BuildLayoutStore: unsupported layout version {0} (expected {1}); starting with an empty plot.",
          layout.version, CurrentVersion));
        return false;
      }

      _validRecords.Clear();
      for (int i = 0; i < layout.objects.Count; i++)
      {
        BuildLayout.BuildLayoutRecord record = layout.objects[i];
        if (record == null) continue;

        if (catalog.Get(record.id) == null)
        {
          Debug.LogWarning(string.Format(
            "BuildLayoutStore: unknown placeable id '{0}' - skipped.", record.id));
          continue;
        }

        _validRecords.Add(record);
      }

      _loading = true;
      try
      {
        grid.ClearAll();

        for (int i = 0; i < _validRecords.Count; i++)
        {
          BuildLayout.BuildLayoutRecord record = _validRecords[i];
          if (grid.Place(
                catalog.Get(record.id),
                new GridCoord(record.x, record.z),
                record.rotation) == null)
          {
            Debug.LogWarning(string.Format(
              "BuildLayoutStore: could not restore '{0}' at ({1}, {2}) rotation {3}.",
              record.id, record.x, record.z, record.rotation));
          }
        }
      }
      finally
      {
        _loading = false;
        _validRecords.Clear();
      }

      _dirty = false;
      return true;
    }

    void Quarantine(string json, string reason)
    {
      Debug.LogWarning("BuildLayoutStore: " + reason + " The bad payload was kept under '" +
                       StorageKey + ".corrupt'.");

      try
      {
        IBuildLayoutStorage storage = Storage;
        storage.Save(StorageKey + ".corrupt", json);
        storage.Delete(StorageKey);
        storage.Flush();
      }
      catch (Exception e)
      {
        Debug.LogWarning("BuildLayoutStore: could not quarantine saved layout: " + e.Message);
      }
    }
  }
}
