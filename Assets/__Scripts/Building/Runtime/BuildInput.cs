using UnityEngine;
using UnityEngine.EventSystems;

namespace Supermarket.Building
{
  /// <summary>
  /// All build-mode input reads live here so an Input System migration
  /// only needs to replace this class or its provider.
  /// </summary>
  public static class BuildInput
  {
    static IBuildInputProvider _provider;

    /// <summary>
    /// Optional input provider seam. Defaults to legacy Unity Input/EventSystem.
    /// </summary>
    public static IBuildInputProvider Provider
    {
      get { return _provider ?? LegacyBuildInputProvider.Instance; }
      set { _provider = value; }
    }

    public static bool ToggleBuildModePressed
    {
      get { return Provider.ToggleBuildModePressed; }
    }

    public static bool CancelPressed
    {
      get { return Provider.CancelPressed; }
    }

    public static bool RotatePressed
    {
      get { return Provider.RotatePressed; }
    }

    public static bool PlacePressed
    {
      get { return Provider.PlacePressed; }
    }

    public static bool RemovePressed
    {
      get { return Provider.RemovePressed; }
    }

    public static Vector2 PointerPosition
    {
      get { return Provider.PointerPosition; }
    }

    /// <summary>Returns 1-9 for the number row, or 0 when no number was pressed.</summary>
    public static int NumberPressed()
    {
      return Provider.NumberPressed();
    }

    public static bool PointerOverUI
    {
      get { return Provider.PointerOverUI; }
    }
  }
}
