using UnityEngine;

public class CameraMovement : MonoBehaviour
{
  [Header("Inscribed")]
  public float moveSpeed = 15f;
  public float movementSmoothing = 10f;

  private Vector3 targetPosition;

  void Start()
  {
    targetPosition = transform.position;
  }

  void Update()
  {
    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");

    Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

    Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

    Vector3 direction = forward * vertical + right * horizontal;

    targetPosition += direction.normalized * moveSpeed * Time.deltaTime;

    transform.position = Vector3.Lerp(
        transform.position,
        targetPosition,
        movementSmoothing * Time.deltaTime
    );
  }
}
