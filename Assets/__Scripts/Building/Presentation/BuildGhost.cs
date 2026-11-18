using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Spawns a preview instance of the selected placeable and tints it
  /// green (valid) or red (invalid).
  /// </summary>
  public class BuildGhost : MonoBehaviour
  {
    [Header("Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Preview")]
    [Tooltip("Layer applied to the preview instance so it never blocks the build raycast.")]
    public string previewLayerName = "Ignore Raycast";

    GameObject _instance;
    Renderer[] _renderers;
    PlaceableDefinition _currentDefinition;

    public void Show(
      PlaceableDefinition definition,
      GridCoord origin,
      int turns,
      Vector3 worldPosition,
      bool valid)
    {
      if (definition == null || definition.prefab == null)
      {
        Hide();
        return;
      }

      if (_instance == null || _currentDefinition != definition)
      {
        Rebuild(definition);
      }

      if (_instance == null) return;

      _instance.SetActive(true);
      _instance.transform.SetPositionAndRotation(
        worldPosition,
        Quaternion.Euler(0f, FootprintUtility.NormalizeTurns(turns) * 90f, 0f));

      Material material = valid ? validMaterial : invalidMaterial;
      if (material == null) return;

      for (int i = 0; i < _renderers.Length; i++)
      {
        if (_renderers[i] != null) _renderers[i].sharedMaterial = material;
      }
    }

    public void Hide()
    {
      if (_instance != null) _instance.SetActive(false);
    }

    void Rebuild(PlaceableDefinition definition)
    {
      Clear();

      _currentDefinition = definition;
      if (definition == null || definition.prefab == null) return;

      _instance = Instantiate(definition.prefab, transform);
      _instance.name = "Ghost Preview";

      int previewLayer = LayerMask.NameToLayer(previewLayerName);
      if (previewLayer < 0) previewLayer = 2; // Built-in "Ignore Raycast" layer.
      SetLayerRecursively(_instance);

      Collider[] colliders = _instance.GetComponentsInChildren<Collider>(true);
      for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

      // Preview instances must never run gameplay scripts.
      MonoBehaviour[] behaviours = _instance.GetComponentsInChildren<MonoBehaviour>(true);
      for (int i = 0; i < behaviours.Length; i++) behaviours[i].enabled = false;

      _renderers = _instance.GetComponentsInChildren<Renderer>(true);

      void SetLayerRecursively(GameObject go)
      {
        go.layer = previewLayer;
        for (int i = 0; i < go.transform.childCount; i++)
        {
          SetLayerRecursively(go.transform.GetChild(i).gameObject);
        }
      }
    }

    void Clear()
    {
      if (_instance != null)
      {
        if (Application.isPlaying) Destroy(_instance);
        else DestroyImmediate(_instance);
      }

      _instance = null;
      _renderers = new Renderer[0];
      _currentDefinition = null;
    }

    void OnDestroy()
    {
      Clear();
    }
  }
}
