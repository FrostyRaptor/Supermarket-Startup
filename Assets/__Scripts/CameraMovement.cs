using UnityEngine;

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

    private Vector3 targetPosition;

    void Start()
    {
      targetPosition = transform.position;
    }

    void Update()
    {
      float horizontal = Input.GetAxisRaw("Horizontal");
      float vertical = Input.GetAxisRaw("Vertical");

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
