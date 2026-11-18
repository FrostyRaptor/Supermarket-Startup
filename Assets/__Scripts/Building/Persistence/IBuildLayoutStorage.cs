namespace Supermarket.Building
{
  /// <summary>
  /// Storage backend for serialized build layout payloads.
  /// Keeps BuildLayoutStore logic independent from PlayerPrefs/files.
  /// </summary>
  public interface IBuildLayoutStorage
  {
    bool Has(string key);
    string Load(string key);
    void Save(string key, string payload);
    void Delete(string key);

    /// <summary>Persists pending writes when the backend buffers (e.g. PlayerPrefs).</summary>
    void Flush();
  }

  /// <summary>
  /// WebGL-safe PlayerPrefs-backed storage (flushed to IndexedDB on Save).
  /// </summary>
  public sealed class PlayerPrefsBuildLayoutStorage : IBuildLayoutStorage
  {
    public static readonly PlayerPrefsBuildLayoutStorage Instance = new PlayerPrefsBuildLayoutStorage();

    PlayerPrefsBuildLayoutStorage() { }

    public bool Has(string key)
    {
      return UnityEngine.PlayerPrefs.HasKey(key);
    }

    public string Load(string key)
    {
      return UnityEngine.PlayerPrefs.GetString(key, string.Empty);
    }

    public void Save(string key, string payload)
    {
      UnityEngine.PlayerPrefs.SetString(key, payload ?? string.Empty);
    }

    public void Delete(string key)
    {
      UnityEngine.PlayerPrefs.DeleteKey(key);
    }

    public void Flush()
    {
      UnityEngine.PlayerPrefs.Save();
    }
  }
}
