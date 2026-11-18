using UnityEngine;

namespace Supermarket.Building
{
  public enum PlaceableCategory
  {
    Furniture,
    Decor
  }

  [CreateAssetMenu(menuName = "Supermarket/Building/Placeable Definition", fileName = "Def_Placeable")]
  public class PlaceableDefinition : ScriptableObject
  {
    [Header("Identity")]
    public string id;
    public string displayName = "New Placeable";
    public PlaceableCategory category = PlaceableCategory.Furniture;

    [Header("Prefab")]
    public GameObject prefab;
    public Vector2Int footprint = Vector2Int.one;
    public Vector3 pivotOffset;

    [Header("Build Rules")]
    public int cost;
    public bool rotatable = true;

    [Header("UI")]
    public Sprite icon;

    public Vector2Int Footprint
    {
      get { return new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y)); }
    }

    public bool IsValid
    {
      get { return prefab != null && !string.IsNullOrWhiteSpace(id); }
    }

    void OnValidate()
    {
      footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));

      if (string.IsNullOrWhiteSpace(id)) id = name;
      if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
    }
  }
}
