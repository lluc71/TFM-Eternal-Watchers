using System.Collections;
using UnityEngine;

public class WeaponStabBossPhase : BossPhaseBase
{

    [Header("Melee Hitbox")]
    [SerializeField] private BossMeleeHitbox meleeHitbox;
    [SerializeField] private float activeTime = 0.5f;

    [Header("Attack Params")]
    [SerializeField] private float minRange = 3.5f;
    [SerializeField] private float maxRange = 9f;
    [SerializeField] private float damage = 20f;

    public override bool CanUseSpecial(EnemyBossController boss, Transform target, float distanceToTarget)
    {
        return (distanceToTarget >= minRange) && (distanceToTarget <= maxRange);
    }

    public override string GetAnimatorTriggerName()
    {
        return "Stab";
    }

    public override void ExecuteSpecialImpact(EnemyBossController boss, Transform target)
    {
        boss.ForceRotateTowards(target.position);

        meleeHitbox?.Initialize(damage);
        meleeHitbox?.ActivateHitbox(activeTime);
    }
}
