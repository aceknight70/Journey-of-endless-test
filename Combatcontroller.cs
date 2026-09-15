using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackDir { Right, Left, Up, Down }

public class CombatController : MonoBehaviour
{
    [Header("Refs")]
    public ZenkiController zenki;
    public Ulteria ulteria;
    public Transform attackOrigin;

    [Header("Attacks")]
    public int   lightDamage     = 10;
    public int   finisherDamage  = 45;
    public float attackCooldown  = 0.22f;
    public float comboWindow     = 1.0f;
    public LayerMask enemyLayer;

    [Header("Hitbox shape")]
    public float lightRange      = 1.4f;
    public float lightThickness  = 1.0f;
    public float finisherRange   = 3.2f;
    public float finisherThickness = 2.2f;

    [Header("Parry")]
    public float parryWindow   = 0.18f;
    public float parryCooldown = 0.55f;
    public float parryStunTime = 1.8f;

    float _attackCd, _parryCd;
    bool  _parrying;
    float _parryEndsAt;

    readonly List<AttackDir> _combo    = new List<AttackDir>();
    readonly List<float>     _comboTime= new List<float>();

    void Update()
    {
        _attackCd -= Time.deltaTime;
        _parryCd  -= Time.deltaTime;

        if (zenki != null && zenki.IsBusy) return;

        if (Input.GetKeyDown(InputMap.Attack) && _attackCd <= 0f)
            Attack();

        if (Input.GetKeyDown(InputMap.Parry) && _parryCd <= 0f)
            StartCoroutine(ParryRoutine());

        // Expire old combo entries
        while (_comboTime.Count > 0 && Time.time - _comboTime[0] > comboWindow)
        {
            _combo.RemoveAt(0);
            _comboTime.RemoveAt(0);
        }
    }

    // ================= ATTACK =================

    void Attack()
    {
        AttackDir dir = ReadDirection();
        _attackCd = attackCooldown;

        RegisterCombo(dir);

        // If finisher triggered, RegisterCombo already launched it — skip the light hit
        if (_finisherQueued) { _finisherQueued = false; return; }

        PerformHit(dir, lightDamage, lightRange, lightThickness, false);
        // trigger swing animation for `dir` here
    }

    AttackDir ReadDirection()
    {
        Vector2 axis = InputMap.MoveAxis();

        // Vertical takes priority when both are held (feels better for up-slashes)
        if (axis.y >  0.5f) return AttackDir.Up;
        if (axis.y < -0.5f) return AttackDir.Down;
        if (axis.x >  0.5f) return AttackDir.Right;
        if (axis.x < -0.5f) return AttackDir.Left;

        return zenki != null && zenki.FacingDir < 0 ? AttackDir.Left : AttackDir.Right;
    }

    void PerformHit(AttackDir dir, int damage, float range, float thickness, bool heavy)
    {
        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;

        Vector2 size, offset;
        switch (dir)
        {
            case AttackDir.Up:
                size = new Vector2(thickness, range); offset = Vector2.up    * range * 0.5f; break;
            case AttackDir.Down:
                size = new Vector2(thickness, range); offset = Vector2.down  * range * 0.5f; break;
            case AttackDir.Left:
                size = new Vector2(range, thickness); offset = Vector2.left  * range * 0.5f; break;
            default:
                size = new Vector2(range, thickness); offset = Vector2.right * range * 0.5f; break;
        }

        var hits = Physics2D.OverlapBoxAll(origin + offset, size, 0f, enemyLayer);
        var seen = new HashSet<IDamageable>();

        foreach (var h in hits)
        {
            var target = h.GetComponentInParent<IDamageable>();
            if (target == null || seen.Contains(target)) continue;
            seen.Add(target);

            Vector2 knock = dir switch
            {
                AttackDir.Up    => Vector2.up,
                AttackDir.Down  => Vector2.down,
                AttackDir.Left  => Vector2.left,
                _               => Vector2.right
            };

            target.TakeDamage(new DamageInfo
            {
                amount    = damage,
                direction = knock,
                source    = gameObject,
                parryable = false,
                heavy     = heavy
            });
        }
    }

    // ================= COMBO FINISHER =================

    bool _finisherQueued;

    void RegisterCombo(AttackDir dir)
    {
        _combo.Add(dir);
        _comboTime.Add(Time.time);

        // Keep only the last 4 — we only care about the tail pattern
        while (_combo.Count > 4) { _combo.RemoveAt(0); _comboTime.RemoveAt(0); }

        if (_combo.Count == 4 &&
            _combo[0] == AttackDir.Up   &&
            _combo[1] == AttackDir.Down &&
            _combo[2] == AttackDir.Left &&
            _combo[3] == AttackDir.Right)
        {
            _combo.Clear();
            _comboTime.Clear();
            _finisherQueued = true;
            StartCoroutine(FinisherRoutine());
        }
    }

    IEnumerator FinisherRoutine()
    {
        // Brief wind-up
        yield return new WaitForSeconds(0.08f);

        if (ulteria != null)
        {
            ulteria.Grow(1.6f, 0.35f);
            ulteria.Say("ENOUGH.");
        }

        yield return new WaitForSeconds(0.15f);

        // Big radial hit
        var hits = Physics2D.OverlapBoxAll(
            transform.position, new Vector2(finisherRange * 2f, finisherRange), 0f, enemyLayer);

        var seen = new HashSet<IDamageable>();
        foreach (var h in hits)
        {
            var target = h.GetComponentInParent<IDamageable>();
            if (target == null || seen.Contains(target)) continue;
            seen.Add(target);

            target.TakeDamage(new DamageInfo
            {
                amount    = finisherDamage,
                direction = ((Vector2)(h.transform.position - transform.position)).normalized,
                source    = gameObject,
                parryable = false,
                heavy     = true
            });
        }

        if (ulteria != null) ulteria.AngelicBurst(transform.position, 5f, finisherDamage / 2, enemyLayer);

        _attackCd = 0.6f;
    }

    // ================= PARRY =================

    IEnumerator ParryRoutine()
    {
        _parrying = true;
        _parryCd = parryCooldown;
        _parryEndsAt = Time.time + parryWindow;

        // parry stance VFX here

        yield return new WaitForSeconds(parryWindow);
        _parrying = false;
    }

    /// Returns true if the incoming hit was parried (and should be nullified).
    public bool TryParry(DamageInfo info)
    {
        if (!_parrying || Time.time > _parryEndsAt) return false;
        if (!info.parryable) return false;

        // Reflect projectiles
        if (info.source != null)
        {
            var rock = info.source.GetComponent<RockProjectile>();
            if (rock != null) rock.Reflect();
        }

        // Stun whatever hit us
        var boss = info.source ? info.source.GetComponentInParent<GolemBoss>() : null;
        if (boss != null) boss.Stagger(parryStunTime);

        ulteria?.Say("Pathetic.");
        // parry flash + hitstop here
        return true;
    }
}
