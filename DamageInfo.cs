using UnityEngine;

public struct DamageInfo
{
    public int amount;
    public Vector2 direction;
    public GameObject source;
    public bool parryable;   // can Zenki parry this?
    public bool heavy;       // finisher / boss slam
}

public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}
