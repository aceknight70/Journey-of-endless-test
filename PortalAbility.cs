using UnityEngine;

public class PortalAbility : MonoBehaviour
{
    [Header("Refs")]
    public Portal portalPrefab;
    public Transform castPoint;

    [Header("Tuning")]
    public float placeCooldown = 0.4f;
    public float portalLifetime = 20f;

    Portal _a, _b;
    bool _nextIsA = true;
    float _cd;

    void Update()
    {
        _cd -= Time.deltaTime;

        if (Input.GetKeyDown(InputMap.Portal) && _cd <= 0f)
        {
            PlacePortal();
            _cd = placeCooldown;
        }
    }

    void PlacePortal()
    {
        Vector2 pos = castPoint ? (Vector2)castPoint.position : (Vector2)transform.position;

        if (_nextIsA)
        {
            DestroyOld(ref _a);
            _a = Instantiate(portalPrefab, pos, Quaternion.identity);
            _a.color = Portal.PortalColor.Blue;
        }
        else
        {
            DestroyOld(ref _b);
            _b = Instantiate(portalPrefab, pos, Quaternion.identity);
            _b.color = Portal.PortalColor.Orange;
        }

        _nextIsA = !_nextIsA;

        if (_a != null && _b != null)
        {
            _a.Link(_b);
            _b.Link(_a);
        }
    }

    void DestroyOld(ref Portal p)
    {
        if (p != null) Destroy(p.gameObject);
        p = null;
    }
}

// ---------------------------------------------------------------
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    public enum PortalColor { Blue, Orange }

    public PortalColor color = PortalColor.Blue;
    public SpriteRenderer rend;
    public float exitOffset = 0.9f;

    Portal _linked;
    float _blockedUntil;

    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ApplyColor();
    }

    public void Link(Portal other) => _linked = other;

    void ApplyColor()
    {
        if (!rend) return;
        rend.color = color == PortalColor.Blue
            ? new Color(0.3f, 0.7f, 1f, 0.9f)
            : new Color(1f, 0.55f, 0.15f, 0.9f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_linked == null) return;
        if (Time.time < _blockedUntil) return;
        if (!other.CompareTag("Player")) return;

        var rb = other.attachedRigidbody;
        Vector2 exitDir = _linked.transform.up;
        other.transform.position = (Vector2)_linked.transform.position + exitDir * exitOffset;

        if (rb != null)
        {
            float speed = rb.velocity.magnitude;
            rb.velocity = exitDir * Mathf.Max(speed, 6f);
        }

        _blockedUntil        = Time.time + 0.35f;
        _linked._blockedUntil = Time.time + 0.35f;
    }
}
