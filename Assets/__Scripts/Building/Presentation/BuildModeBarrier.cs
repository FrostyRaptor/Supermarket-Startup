using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Marks a section's wall geometry as a build-mode-only visual barrier:
  /// the renderers are hidden outside build mode and the colliders are always
  /// disabled, so the section never reads as a physical wall. Storage and deli
  /// keep their physical walls and do not use this component.
  /// </summary>
  [DisallowMultipleComponent]
  public class BuildModeBarrier : MonoBehaviour
  {
    [Tooltip("Controller that owns build mode. Found automatically when left empty.")]
    public BuildController controller;

    Renderer[] _renderers = new Renderer[0];
    Collider[] _colliders = new Collider[0];
    bool _subscribed;

    void Awake()
    {
      CacheParts();
      SetCollidersEnabled(false);
    }

    void Start()
    {
      ResolveController();
      Apply();
    }

    void OnEnable()
    {
      ResolveController();
      Apply();
    }

    void OnDisable()
    {
      Unsubscribe();
    }

    void OnDestroy()
    {
      Unsubscribe();
    }

    void CacheParts()
    {
      _renderers = GetComponentsInChildren<Renderer>(true);
      _colliders = GetComponentsInChildren<Collider>(true);
    }

    void ResolveController()
    {
      if (controller == null) controller = FindFirstObjectByType<BuildController>();
      if (_subscribed || controller == null) return;

      controller.StateChanged += Apply;
      _subscribed = true;
    }

    void Unsubscribe()
    {
      if (!_subscribed) return;
      if (controller != null) controller.StateChanged -= Apply;
      _subscribed = false;
    }

    void Apply()
    {
      bool visible = controller != null && controller.IsInBuildMode;
      for (int i = 0; i < _renderers.Length; i++)
      {
        if (_renderers[i] != null) _renderers[i].enabled = visible;
      }
    }

    void SetCollidersEnabled(bool enabled)
    {
      for (int i = 0; i < _colliders.Length; i++)
      {
        if (_colliders[i] != null) _colliders[i].enabled = enabled;
      }
    }
  }
}
