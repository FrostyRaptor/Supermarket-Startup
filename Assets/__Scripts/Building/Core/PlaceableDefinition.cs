using UnityEngine;
using Supermarket.Config;

namespace Supermarket.Building
{
  public enum PlaceableCategory
  {
    Furniture,
    Decor
  }

  /// <summary>Store area whose cells accept this placeable.</summary>
  public enum PlaceableArea
  {
    General,
    Checkout
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
    [Tooltip("Only cells of this area accept this placeable. Checkout lanes use Checkout.")]
    public PlaceableArea allowedArea = PlaceableArea.General;
    public bool rotatable = true;

    [Header("UI")]
    public Sprite icon;

    [Header("Upgrades")]
    [Tooltip("Optional. When set, placed instances gain an UpgradeableFixture with this curve.")]
    public UpgradeProfile upgradeProfile;

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
