using UnityEngine;
using UnityEngine.InputSystem;

namespace Supermarket
{
  public class CameraMovement : MonoBehaviour
  {
    [Header("Inscribed")]
    public float moveSpeed = 15f;
    public float movementSmoothing = 10f;

    [Header("Bounds")]
    public bool clampToPlot = true;
    public Vector2 minXZ = new Vector2(-8f, -8f);
    public Vector2 maxXZ = new Vector2(8f, 8f);

    InputAction _moveAction;

    Vector3 targetPosition;

    void Awake()
    {
      // Replaces the legacy Horizontal/Vertical axes with an Input System action:
      // WASD, arrow keys, and the left gamepad stick all drive the same 2D value.
      _moveAction = new InputAction("CameraMove", InputActionType.Value);
      _moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/w")
        .With("Up", "<Keyboard>/upArrow")
        .With("Down", "<Keyboard>/s")
        .With("Down", "<Keyboard>/downArrow")
        .With("Left", "<Keyboard>/a")
        .With("Left", "<Keyboard>/leftArrow")
        .With("Right", "<Keyboard>/d")
        .With("Right", "<Keyboard>/rightArrow");
      _moveAction.AddBinding("<Gamepad>/leftStick");
    }

    void OnEnable()
    {
      if (_moveAction != null) _moveAction.Enable();
    }

    void OnDisable()
    {
      if (_moveAction != null) _moveAction.Disable();
    }

    void OnDestroy()
    {
      if (_moveAction == null) return;

      _moveAction.Dispose();
      _moveAction = null;
    }

    void Start()
    {
      targetPosition = transform.position;
    }

    void Update()
    {
      Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
      float horizontal = move.x;
      float vertical = move.y;

      Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
      Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
      Vector3 direction = (forward * vertical + right * horizontal).normalized;

      targetPosition += direction * moveSpeed * Time.deltaTime;

      if (clampToPlot)
      {
        targetPosition.x = Mathf.Clamp(targetPosition.x, minXZ.x, maxXZ.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minXZ.y, maxXZ.y);
      }

      float t = 1f - Mathf.Exp(-movementSmoothing * Time.deltaTime);
      transform.position = Vector3.Lerp(transform.position, targetPosition, t);
    }
  }
}
