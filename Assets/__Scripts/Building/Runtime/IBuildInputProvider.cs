using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Supermarket.Building
{
  /// <summary>
  /// Input seam for build interactions. Default implementation uses the Input System.
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
  /// Input System / EventSystem-backed provider (WebGL-compatible).
  /// </summary>
  public sealed class InputSystemBuildInputProvider : IBuildInputProvider
  {
    public static readonly InputSystemBuildInputProvider Instance = new InputSystemBuildInputProvider();

    InputSystemBuildInputProvider() { }

    public bool ToggleBuildModePressed
    {
      get
      {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.bKey.wasPressedThisFrame;
      }
    }

    public bool CancelPressed
    {
      get
      {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
      }
    }

    public bool RotatePressed
    {
      get
      {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
      }
    }

    public bool PlacePressed
    {
      get
      {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
      }
    }

    public bool RemovePressed
    {
      get
      {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
      }
    }

    public Vector2 PointerPosition
    {
      get
      {
        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
      }
    }

    public int NumberPressed()
    {
      Keyboard keyboard = Keyboard.current;
      if (keyboard == null) return 0;

      for (int i = 1; i <= 9; i++)
      {
        if (DigitPressed(keyboard, i)) return i;
      }

      return 0;
    }

    public bool PointerOverUI
    {
      get
      {
        return EventSystem.current != null
               && EventSystem.current.IsPointerOverGameObject();
      }
    }

    static bool DigitPressed(Keyboard keyboard, int digit)
    {
      switch (digit)
      {
        case 1: return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
        case 2: return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
        case 3: return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
        case 4: return keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame;
        case 5: return keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame;
        case 6: return keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame;
        case 7: return keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame;
        case 8: return keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame;
        case 9: return keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame;
        default: return false;
      }
    }
  }
}
