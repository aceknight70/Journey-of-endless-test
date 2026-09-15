using System;
using System.Collections;
using UnityEngine;

public class Ulteria : MonoBehaviour
{
    public static Ulteria Instance { get; private set; }

    [Header("Shape-shift")]
    public float normalScale = 1f;
    public Transform bladePivot;      // child transform that scales
    public float growPunch = 0.08f;   // scale overshoot

    [Header("Angelic Burst")]
    public GameObject burstVfxPrefab;
    public int burstHealAmount = 0;

    public event Action<string> OnSpeak;

    Vector3 _baseScale;
    Coroutine _growRoutine;

    void Awake()
    {
        Instance = this;
        _baseScale = bladePivot ? bladePivot.localScale : Vector3.one;
    }

    public void Say(string line)
    {
        OnSpeak?.Invoke(line);
        Debug.Log($"[Ulteria]: {line}");
    }

    /// Grows the blade and springs back.
    public void Grow(float multiplier, float duration)
    {
        if (!bladePivot) return;
        if (_growRoutine != null) StopCoroutine(_growRoutine);
        _growRoutine = StartCoroutine(GrowRoutine(multiplier, duration));
    }

    IEnumerator GrowRoutine(float multiplier, float duration)
    {
        Vector3 target = _baseScale * multiplier;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            bladePivot.localScale = Vector3.Lerp(_baseScale, target, EaseOutBack(k));
            yield return null;
        }

        bladePivot.localScale = target;
        // hold briefly
        yield return new WaitForSeconds(0.12f);

        t = 0f;
        while (t < duration * 0.6f)
        {
            t += Time.deltaTime;
            bladePivot.localScale = Vector3.Lerp(target, _baseScale, t / (duration * 0.6f));
            yield return null;
        }
        bladePivot.localScale = _baseScale;
    }

    /// Radial holy damage + light flash.
    public void AngelicBurst(Vector2 origin, float radius, int damage, LayerMask enemyLayer)
    {
        if (burstVfxPrefab)
            Instantiate(burstVfxPrefab, origin, Quaternion.identity);

        var hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayer);
        foreach (var h in hits)
        {
            var target = h.GetComponentInParent<IDamageable>();
            target?.TakeDamage(new DamageInfo
            {
                amount    = damage,
                direction = ((Vector2)h.transform.position - origin).normalized,
                source    = gameObject,
                parryable = false,
                heavy     = true
            });
        }

        Say("Angelic Burst!");
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
