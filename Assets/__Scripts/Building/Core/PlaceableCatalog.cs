using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  [CreateAssetMenu(menuName = "Supermarket/Building/Placeable Catalog", fileName = "PlaceableCatalog")]
  public class PlaceableCatalog : ScriptableObject
  {
    public List<PlaceableDefinition> definitions = new List<PlaceableDefinition>();

    Dictionary<string, PlaceableDefinition> _lookup;

    public IReadOnlyList<PlaceableDefinition> Definitions
    {
      get { return definitions; }
    }

    public PlaceableDefinition Get(string id)
    {
      if (string.IsNullOrEmpty(id)) return null;

      EnsureLookup();
      PlaceableDefinition definition;
      return _lookup.TryGetValue(id, out definition) ? definition : null;
    }

    public void EnsureLookup()
    {
      if (_lookup == null) RebuildLookup();
    }

    public void RebuildLookup()
    {
      _lookup = new Dictionary<string, PlaceableDefinition>();

      for (int i = 0; i < definitions.Count; i++)
      {
        PlaceableDefinition definition = definitions[i];
        if (definition == null || string.IsNullOrEmpty(definition.id)) continue;

        if (_lookup.ContainsKey(definition.id))
        {
          Debug.LogWarning(
            string.Format("PlaceableCatalog: duplicate definition id '{0}' on {1}. The later entry wins.",
              definition.id, definition.name));
        }

        _lookup[definition.id] = definition;
      }
    }

    void OnEnable()
    {
      _lookup = null;
    }

    void OnValidate()
    {
      _lookup = null;
    }
  }
}
