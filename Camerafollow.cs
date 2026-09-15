using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public Vector2 lookAhead = new Vector2(2f, 0f);
    public Vector2 offset = Vector2.zero;

    Vector3 _vel;

    void LateUpdate()
    {
        if (!target) return;

        var rb = target.GetComponent<Rigidbody2D>();
        Vector2 lead = rb ? new Vector2(rb.velocity.x * 0.12f, 0f) : Vector2.zero;
        lead = Vector2.ClampMagnitude(lead, Mathf.Abs(lookAhead.x));

        Vector3 desired = new Vector3(
            target.position.x + lead.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);
    }
}
