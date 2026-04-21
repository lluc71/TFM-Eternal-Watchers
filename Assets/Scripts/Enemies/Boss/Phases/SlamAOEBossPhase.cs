using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class SlamAOEBossPhase : BossPhaseBase
{
    [Header("Attack Params")]
    [SerializeField] private float minRange = 3f;
    [SerializeField] private float maxRange = 8f;

    [Header("AOE Params")]
    [SerializeField] private float aoeRadius = 3.5f;
    [SerializeField] private float aoeDamage = 25f;
    [SerializeField] private GameObject aoeVfxPrefab;
    [SerializeField] private LayerMask playerLayerMask;

    public override bool CanUseSpecial(EnemyBossController boss, Transform target, float distanceToTarget)
    {
        return (distanceToTarget >= minRange) && (distanceToTarget <= maxRange);
    }

    public override string GetAnimatorTriggerName()
    {
        return "Slam";
    }

    public override void ExecuteSpecialImpact(EnemyBossController boss, Transform target)
    {
        Vector3 spawnPosition = boss.WeaponSpawnPoint.position;
        spawnPosition.y = 0.4f;
        Instantiate(aoeVfxPrefab, spawnPosition, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(
            spawnPosition,
            aoeRadius,
            playerLayerMask
        );

        foreach (Collider hit in hits)
        {
            MovementController player = hit.GetComponent<MovementController>();
            if (player == null) continue;

            player.TakeDamage(aoeDamage);
        }
    }
}
