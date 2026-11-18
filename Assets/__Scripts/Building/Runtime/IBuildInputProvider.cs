using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Input seam for build interactions. Default implementation uses legacy Input.
  /// </summary>
  public interface IBuildInputProvider
  {
    bool ToggleBuildModePressed { get; }
    bool CancelPressed { get; }
    bool RotatePressed { get; }
    bool PlacePressed { get; }
    bool RemovePressed { get; }
    Vector2 PointerPosition { get; }
    bool PointerOverUI { get; }

    /// <summary>Returns 1-9 for the number row, or 0 when no number was pressed.</summary>
    int NumberPressed();
  }

  /// <summary>
  /// Legacy Unity Input / EventSystem-backed provider (WebGL-compatible).
  /// </summary>
  public sealed class LegacyBuildInputProvider : IBuildInputProvider
  {
    public static readonly LegacyBuildInputProvider Instance = new LegacyBuildInputProvider();

    LegacyBuildInputProvider() { }

    public bool ToggleBuildModePressed { get { return Input.GetKeyDown(KeyCode.B); } }
    public bool CancelPressed { get { return Input.GetKeyDown(KeyCode.Escape); } }
    public bool RotatePressed { get { return Input.GetKeyDown(KeyCode.R); } }
    public bool PlacePressed { get { return Input.GetMouseButtonDown(0); } }
    public bool RemovePressed { get { return Input.GetMouseButtonDown(1); } }
    public Vector2 PointerPosition { get { return Input.mousePosition; } }

    public int NumberPressed()
    {
      for (int i = 1; i <= 9; i++)
      {
        KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + i);
        if (Input.GetKeyDown(key)) return i;
      }

      return 0;
    }

    public bool PointerOverUI
    {
      get
      {
        return UnityEngine.EventSystems.EventSystem.current != null
               && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
      }
    }
  }
}
