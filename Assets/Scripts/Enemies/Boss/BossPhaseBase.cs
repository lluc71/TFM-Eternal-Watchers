using System.Collections;
using UnityEngine;

public abstract class BossPhaseBase : MonoBehaviour
{
    [Header("Phase Settings")]
    [SerializeField] protected float minHealthPercent = 0f;
    [SerializeField] protected float maxHealthPercent = 1f;
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float specialCooldown = 5f;

    public float MinHealthPercent => minHealthPercent;
    public float MaxHealthPercent => maxHealthPercent;
    public float MoveSpeed => moveSpeed;
    public float SpecialCooldown => specialCooldown;

    public bool IsInPhase(float healthPercent)
    {
        return ((healthPercent <= maxHealthPercent) && (healthPercent > minHealthPercent));
    }

    public virtual void OnEnterPhase(EnemyBossController boss) { }
    public virtual void OnExitPhase(EnemyBossController boss) { }

    public abstract bool CanUseSpecial(EnemyBossController boss, Transform target, float distanceToTarget);
    public abstract IEnumerator ExecuteSpecial(EnemyBossController boss, Transform target);
}
