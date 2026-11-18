using UnityEngine;
using UnityEngine.EventSystems;

namespace Supermarket.Building
{
  /// <summary>
  /// All build-mode input reads live here so an Input System migration
  /// only needs to replace this class.
  /// </summary>
  public static class BuildInput
  {
    public static bool ToggleBuildModePressed
    {
      get { return Input.GetKeyDown(KeyCode.B); }
    }

    public static bool CancelPressed
    {
      get { return Input.GetKeyDown(KeyCode.Escape); }
    }

    public static bool RotatePressed
    {
      get { return Input.GetKeyDown(KeyCode.R); }
    }

    public static bool PlacePressed
    {
      get { return Input.GetMouseButtonDown(0); }
    }

    public static bool RemovePressed
    {
      get { return Input.GetMouseButtonDown(1); }
    }

    public static Vector2 PointerPosition
    {
      get { return Input.mousePosition; }
    }

    /// <summary>Returns 1-9 for the number row, or 0 when no number was pressed.</summary>
    public static int NumberPressed()
    {
      for (int i = 1; i <= 9; i++)
      {
        KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + i);
        if (Input.GetKeyDown(key)) return i;
      }

      return 0;
    }

    public static bool PointerOverUI
    {
      get
      {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
      }
    }
  }
}
