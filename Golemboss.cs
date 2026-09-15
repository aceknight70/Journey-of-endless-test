using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GolemBoss : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int   maxHealth    = 600;
    public float moveSpeed    = 2.6f;
    public float detectRange  = 16f;
    public float slamRange    = 3.0f;
    public float attackGap    = 1.6f;

    [Header("Combat")]
    public int   slamDamage   = 22;
    public int   rockDamage   = 14;
    public float slamRadius   = 3.2f;
    public LayerMask playerLayer;

    [Header("Refs")]
    public Transform player;
    public Transform groundPoint;
    public Transform throwPoint;
    public RockProjectile rockPrefab;
    public SpriteRenderer rend;

    [Header("VFX")]
    public GameObject slamVfx;
    public GameObject hitVfx;

    Rigidbody2D _rb;
    int _hp;
    bool _dead, _staggered, _active;
    float _staggerUntil;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;
        _hp = maxHealth;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Start()
    {
        StartCoroutine(AILoop());
    }

    void Update()
    {
        if (_staggered && Time.time > _staggerUntil) _staggered = false;
    }

    IEnumerator AILoop()
    {
        yield return new WaitForSeconds(1.0f);

        while (!_dead)
        {
            if (_staggered) { _rb.velocity = new Vector2(0f, _rb.velocity.y); yield return null; continue; }
            if (player == null) { yield return null; continue; }

            float dist = Vector2.Distance(transform.position, player.position);
            _active = dist < detectRange;

            if (!_active) { _rb.velocity = new Vector2(0f, _rb.velocity.y); yield return null; continue; }

            if (dist <= slamRange)      yield return Slam();
            else if (dist < detectRange) yield return Chase(1.6f);

            yield return new WaitForSeconds(attackGap);
        }
    }

    IEnumerator Chase(float duration)
    {
        float t = 0f;
        while (t < duration && !_staggered && !_dead)
        {
            t += Time.deltaTime;

            float dx = player.position.x - transform.position.x;
            _rb.velocity = new Vector2(Mathf.Sign(dx) * moveSpeed, _rb.velocity.y);

            // face the player
            if (Mathf.Abs(dx) > 0.1f)
                transform.localScale = new Vector3(Mathf.Sign(dx), 1f, 1f);

            if (Vector2.Distance(transform.position, player.position) <= slamRange) break;
            yield return null;
        }
        _rb.velocity = new Vector2(0f, _rb.velocity.y);
    }

    IEnumerator Slam()
    {
        // --- Telegraph ---
        if (rend) rend.color = new Color(1f, 0.5f, 0.4f);
        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        yield return new WaitForSeconds(0.55f);

        // --- Impact ---
        if (slamVfx) Instantiate(slamVfx, groundPoint ? groundPoint.position : transform.position, Quaternion.identity);
        if (rend) rend.color = Color.white;

        var hits = Physics2D.OverlapCircleAll(
            groundPoint ? groundPoint.position : transform.position, slamRadius, playerLayer);

        foreach (var h in hits)
            h.GetComponentInParent<IDamageable>()?.TakeDamage(new DamageInfo
            {
                amount    = slamDamage,
                direction = Vector2.up,
                source    = gameObject,
                parryable = false,
                heavy     = true
            });

        yield return new WaitForSeconds(0.5f);

        // Sometimes follow up with a rock throw
        if (Random.value > 0.5f) yield return ThrowRock();
    }

    IEnumerator ThrowRock()
    {
        if (rockPrefab == null || throwPoint == null) yield break;

        if (rend) rend.color = new Color(0.6f, 0.8f, 1f);
        yield return new WaitForSeconds(0.35f);

        var rock = Instantiate(rockPrefab, throwPoint.position, Quaternion.identity);
        Vector2 dir = ((Vector2)player.position - (Vector2)throwPoint.position).normalized;
        rock.Launch(dir, rockDamage, gameObject);

        if (rend) rend.color = Color.white;
        yield return new WaitForSeconds(0.4f);
    }

    // ================= DAMAGE =================

    public void TakeDamage(DamageInfo info)
    {
        if (_dead) return;

        int amount = info.amount;
        if (_staggered) amount *= 2;   // reward for parrying

        _hp -= amount;
        if (hitVfx) Instantiate(hitVfx, transform.position, Quaternion.identity);

        StartCoroutine(FlashWhite());

        if (_hp <= 0) Die();
    }

    IEnumerator FlashWhite()
    {
        if (!rend) yield break;
        var original = rend.color;
        rend.color = Color.white * 1.2f;
        yield return new WaitForSeconds(0.07f);
        rend.color = original;
    }

    public void Stagger(float duration)
    {
        if (_dead) return;
        _staggered = true;
        _staggerUntil = Time.time + duration;
        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        if (rend) rend.color = new Color(0.7f, 0.7f, 1f);
    }

    void Die()
    {
        _dead = true;
        _rb.velocity = Vector2.zero;
        Ulteria.Instance?.Say("The golem falls. Onward, Zenki.");
        // death anim, drop, etc.
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundPoint ? groundPoint.position : transform.position, slamRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}

// ---------------------------------------------------------------
public class RockProjectile : MonoBehaviour
{
    public float speed = 9f;
    public float lifetime = 5f;

    int _damage;
    GameObject _owner;
    Rigidbody2D _rb;
    bool _reflected;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 dir, int damage, GameObject owner)
    {
        _damage = damage;
        _owner  = owner;
        if (_rb) _rb.velocity = dir * speed;
    }

    public void Reflect()
    {
        _reflected = true;
        if (_rb) _rb.velocity = -_rb.velocity * 1.5f;
        // swap layer so it hits enemies instead
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_reflected)
        {
            other.GetComponentInParent<IDamageable>()?.TakeDamage(new DamageInfo
            {
                amount    = _damage,
                direction = (_rb ? _rb.velocity.normalized : Vector2.right),
                source    = gameObject,
                parryable = true,     // ← Zenki can parry this
                heavy     = false
            });
            Destroy(gameObject);
        }
    }
}
