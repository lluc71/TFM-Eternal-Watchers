using System.Collections;
using UnityEngine;

public class SimpleBossPhase : BossPhaseBase
{

    [Header("VFX Hitbox")]
    [SerializeField] private GameObject vfxPrefab;

    [Header("Attack Params")]
    [SerializeField] private float castDelay = 0.75f;
    [SerializeField] private float minRange = 3.5f;
    [SerializeField] private float maxRange = 9f;

    public override bool CanUseSpecial(EnemyBossController boss, Transform target, float distanceToTarget)
    {
        return (distanceToTarget >= minRange) && (distanceToTarget <= maxRange);
    }

    public override IEnumerator ExecuteSpecial(EnemyBossController boss, Transform target)
    {
        boss.Animator.SetTrigger("Stab");
        yield return new WaitForSeconds(castDelay);

        boss.RotateTowards(target.position);

        Vector3 spawnPosition = boss.EffectsSpawnPoint.position;

        Instantiate(vfxPrefab, spawnPosition, boss.transform.rotation);
    }
}
