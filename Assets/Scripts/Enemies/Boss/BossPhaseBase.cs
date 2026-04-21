using System.Collections;
using UnityEngine;

public abstract class BossPhaseBase : MonoBehaviour
{
    [Header("Phase Settings")]
    [SerializeField] protected float minHealthPhase = 0f;
    [SerializeField] protected float maxHealthPhase = 100f;
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float specialCooldown = 5f;

    public float MinHealth => minHealthPhase;
    public float MaxHealth => maxHealthPhase;
    public float MoveSpeed => moveSpeed;
    public float SpecialCooldown => specialCooldown;

    public bool IsInPhase(float health)
    {
        return (health <= maxHealthPhase) && (health > minHealthPhase);
    }

    public virtual void OnEnterPhase(EnemyBossController boss) { }
    public virtual void OnExitPhase(EnemyBossController boss) { }

    public abstract bool CanUseSpecial(EnemyBossController boss, Transform target, float distanceToTarget);
    public abstract string GetAnimatorTriggerName();
    public abstract void ExecuteSpecialImpact(EnemyBossController boss, Transform target);
}
