using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class SlamAOEBossPhase : BossPhaseBase
{
    [Header("Attack Params")]
    [SerializeField] private float castDelay = 0.8f;
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

    public override IEnumerator ExecuteSpecial(EnemyBossController boss, Transform target)
    {
        boss.Animator.SetTrigger("Slam");
        yield return new WaitForSeconds(castDelay);

        boss.RotateTowards(target.position);

        Vector3 spawnPosition = boss.transform.position + boss.transform.forward * 4f;
        spawnPosition.y = 0.4f;
        Instantiate(aoeVfxPrefab, spawnPosition, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(
            spawnPosition,
            aoeRadius,
            playerLayerMask
        );

        for (int i = 0; i < hits.Length; i++)
        {
            MovementController player = hits[i].GetComponent<MovementController>();
            if (player == null) continue;

            player.TakeDamage(aoeDamage);
        }
    }
}
