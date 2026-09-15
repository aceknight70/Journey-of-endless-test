using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class ZenkiController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 100;
    public CombatController combat;

    [Header("Movement")]
    public float moveSpeed   = 7f;
    public float accel       = 70f;
    public float decel       = 80f;
    public float jumpForce   = 14f;
    public int   maxAirJumps = 1;
    public float coyoteTime  = 0.10f;
    public float jumpBuffer  = 0.12f;

    [Header("Gravity")]
    public float baseGravity      = 4f;
    public float fallMultiplier   = 1.9f;
    public float lowJumpMultiplier= 2.6f;
    public float maxFallSpeed     = 24f;

    [Header("Dash — Dark Burst")]
    public float dashSpeed    = 24f;
    public float dashTime     = 0.15f;
    public float dashCooldown = 0.45f;
    public int   maxAirDashes = 1;
    public GameObject dashTrailPrefab;

    [Header("Wall")]
    public float wallSlideSpeed  = 3f;
    public float wallClimbSpeed  = 4f;
    public Vector2 wallJumpForce = new Vector2(12f, 15f);
    public float wallStickTime   = 0.12f;

    [Header("Backflip")]
    public float backflipForce = 12f;
    public float backflipTime  = 0.35f;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.16f;
    public LayerMask groundLayer;
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;

    // ---- public state ----
    public bool IsDashing     { get; private set; }
    public bool IsInvulnerable{ get; private set; }
    public bool IsBusy        => IsDashing || _backflipping || _taunting;
    public int  FacingDir     { get; private set; } = 1;
    public int  Health        { get; private set; }

    Rigidbody2D _rb;
    CapsuleCollider2D _col;

    float _coyote, _jumpBuf, _dashCd, _wallStickTimer;
    int   _airJumpsUsed, _airDashesUsed;
    bool  _backflipping, _taunting, _dead;

    void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();

        _rb.gravityScale            = baseGravity;
        _rb.freezeRotation          = true;
        _rb.collisionDetectionMode  = CollisionDetectionMode2D.Continuous;
        _rb.interpolation           = RigidbodyInterpolation2D.Interpolate;

        Health = maxHealth;
    }

    void Update()
    {
        if (_dead) return;

        bool grounded = GroundCheck();
        if (grounded) { _coyote = coyoteTime; _airJumpsUsed = 0; _airDashesUsed = 0; }
        else          { _coyote -= Time.deltaTime; }

        _jumpBuf        -= Time.deltaTime;
        _dashCd         -= Time.deltaTime;
        _wallStickTimer -= Time.deltaTime;

        if (Input.GetKeyDown(InputMap.Jump)) _jumpBuf = jumpBuffer;

        if (!IsBusy)
        {
            if (Input.GetKeyDown(InputMap.Dash))     TryDash();
            if (Input.GetKeyDown(InputMap.Backflip)) StartCoroutine(Backflip());
            if (Input.GetKeyDown(InputMap.Taunt))    StartCoroutine(Taunt());
        }

        if (_jumpBuf > 0f && !IsBusy) TryJump(grounded);
    }

    void FixedUpdate()
    {
        if (_dead) return;

        if (IsDashing) return; // dash coroutine owns velocity

        if (_backflipping) return;

        float xInput = Input.GetAxisRaw("Horizontal");

        // ---- horizontal movement ----
        float targetX = xInput * moveSpeed;
        float rate    = Mathf.Abs(targetX) > 0.01f ? accel : decel;
        _rb.velocity = new Vector2(
            Mathf.MoveTowards(_rb.velocity.x, targetX, rate * Time.fixedDeltaTime),
            _rb.velocity.y);

        if (Mathf.Abs(xInput) > 0.01f)
        {
            FacingDir = xInput > 0 ? 1 : -1;
            transform.localScale = new Vector3(FacingDir, 1, 1);
        }

        // ---- walls ----
        bool touchingWall = WallCheck(out int wallDir);

        if (touchingWall && !GroundCheck())
        {
            bool pushingIntoWall = (wallDir > 0 && xInput > 0) || (wallDir < 0 && xInput < 0);

            // Wall scaling: hold UP while pressed against a wall
            if (pushingIntoWall && Input.GetKey(InputMap.Up))
            {
                _rb.velocity = new Vector2(_rb.velocity.x, wallClimbSpeed);
            }
            // Wall slide
            else if (pushingIntoWall && _rb.velocity.y < 0f)
            {
                _rb.velocity = new Vector2(_rb.velocity.x,
                    Mathf.Max(_rb.velocity.y, -wallSlideSpeed));
            }
        }

        // ---- gravity shaping ----
        if (_rb.velocity.y < 0f)
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        else if (_rb.velocity.y > 0f && !Input.GetKey(InputMap.Jump))
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;

        _rb.velocity = new Vector2(_rb.velocity.x,
            Mathf.Max(_rb.velocity.y, -maxFallSpeed));
    }

    // ================= ACTIONS =================

    void TryJump(bool grounded)
    {
        bool touchingWall = WallCheck(out int wallDir);

        // Wall jump takes priority
        if (touchingWall && !grounded)
        {
            _rb.velocity = new Vector2(-wallDir * wallJumpForce.x, wallJumpForce.y);
            FacingDir = -wallDir;
            transform.localScale = new Vector3(FacingDir, 1, 1);
            _wallStickTimer = wallStickTime;
            _jumpBuf = 0f;
            return;
        }

        if (_coyote > 0f)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            _coyote = 0f;
            _jumpBuf = 0f;
        }
        else if (_airJumpsUsed < maxAirJumps)
        {
            _airJumpsUsed++;
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            _jumpBuf = 0f;
            // hook VFX here — void ring for double jump
        }
    }

    void TryDash()
    {
        if (_dashCd > 0f) return;
        if (!GroundCheck() && _airDashesUsed >= maxAirDashes) return;

        if (!GroundCheck()) _airDashesUsed++;
        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        IsDashing = true;
        IsInvulnerable = true;
        _dashCd = dashCooldown;

        float dir = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(dir) < 0.01f) dir = FacingDir;

        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0f;
        _rb.velocity = new Vector2(dir * dashSpeed, 0f);

        if (dashTrailPrefab)
            Instantiate(dashTrailPrefab, transform.position, Quaternion.identity, transform);

        yield return new WaitForSeconds(dashTime);

        _rb.gravityScale = originalGravity;
        _rb.velocity = new Vector2(_rb.velocity.x * 0.35f, _rb.velocity.y);
        IsDashing = false;
        IsInvulnerable = false;
    }

    IEnumerator Backflip()
    {
        _backflipping = true;
        IsInvulnerable = true;

        float dir = -FacingDir;
        _rb.velocity = new Vector2(dir * backflipForce * 0.6f, backflipForce);

        // trigger flip animation here
        yield return new WaitForSeconds(backflipTime);

        _backflipping = false;
        IsInvulnerable = false;
    }

    IEnumerator Taunt()
    {
        _taunting = true;
        _rb.velocity = new Vector2(0f, _rb.velocity.y);

        Ulteria.Instance?.Say("Is that all, Zenki? I've seen puddles with more fight.");
        // trigger taunt anim + aura VFX

        yield return new WaitForSeconds(0.8f);
        _taunting = false;
    }

    // ================= CHECKS =================

    public bool GroundCheck() =>
        Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

    bool WallCheck(out int wallDir)
    {
        wallDir = 0;
        var hitR = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, groundLayer);
        var hitL = Physics2D.Raycast(wallCheck.position, Vector2.left,  wallCheckDistance, groundLayer);

        if (hitR.collider != null) { wallDir =  1; return true; }
        if (hitL.collider != null) { wallDir = -1; return true; }
        return false;
    }

    // ================= DAMAGE =================

    public void TakeDamage(DamageInfo info)
    {
        if (_dead || IsInvulnerable || IsDashing) return;

        // Parry check first
        if (combat != null && combat.TryParry(info)) return;

        Health -= info.amount;
        _rb.velocity = new Vector2(info.direction.x * 6f, 8f); // knockback
        // hit VFX, screenshake, i-frames

        if (Health <= 0) Die();
    }

    void Die()
    {
        _dead = true;
        _rb.velocity = Vector2.zero;
        Ulteria.Instance?.Say("Get up. We are not finished, Zenki.");
        // death sequence
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (wallCheck)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.left  * wallCheckDistance);
        }
    }
}
