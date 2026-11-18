using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Creates and destroys placed-object instances for the build system.
  /// Decouples occupancy/state from spawning and hierarchy concerns.
  /// </summary>
  public interface IPlacedObjectFactory
  {
    PlacedObject Create(
      PlaceableDefinition definition,
      GridCoord originCell,
      int turns,
      Vector3 worldPosition,
      Quaternion worldRotation,
      Transform parent);

    void Release(PlacedObject placed);
  }

  /// <summary>
  /// Default factory matching the original BuildGrid behavior.
  /// </summary>
  public sealed class DefaultPlacedObjectFactory : IPlacedObjectFactory
  {
    public static readonly DefaultPlacedObjectFactory Instance = new DefaultPlacedObjectFactory();

    DefaultPlacedObjectFactory() { }

    public PlacedObject Create(
      PlaceableDefinition definition,
      GridCoord originCell,
      int turns,
      Vector3 worldPosition,
      Quaternion worldRotation,
      Transform parent)
    {
      if (definition == null || definition.prefab == null) return null;

      GameObject instance = Object.Instantiate(definition.prefab, worldPosition, worldRotation, parent);
      instance.name = string.Format("{0} {1}", definition.displayName, originCell);

      PlacedObject placed = instance.GetComponent<PlacedObject>();
      if (placed == null) placed = instance.AddComponent<PlacedObject>();
      return placed;
    }

    public void Release(PlacedObject placed)
    {
      if (placed == null) return;

      GameObject go = placed.gameObject;
      if (Application.isPlaying) Object.Destroy(go);
      else Object.DestroyImmediate(go);
    }
  }
}
