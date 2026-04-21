using UnityEngine;

public class ThrowProjectilesBossPhase : BossPhaseBase
{
    [Header("Attack Params")]
    [SerializeField] private float minRange = 3f;
    [SerializeField] private float maxRange = 15f;

    [Header("Projectile Params")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float maxSpreadAngle = 50f;

    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float projectileDamage = 15f;

    public override bool CanUseSpecial(EnemyBossController boss, Transform target, float distanceToTarget)
    {
        return (distanceToTarget >= minRange) && (distanceToTarget <= maxRange);
    }

    public override string GetAnimatorTriggerName()
    {
        return "Throw";
    }

    public override void ExecuteSpecialImpact(EnemyBossController boss, Transform target)
    {
        boss.ForceRotateTowards(target.position);

        Transform spawnPoint = boss.ProjectileSpawnPoint;

        float spread = Mathf.Clamp(maxSpreadAngle, 0f, 50f);

        //Dispara hacia el jugador => Mas dificil
        //Vector3 direction = target.position - spawnPoint.position;
        //Dispara hacia delante => Mas realista
        Vector3 direction = boss.transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = boss.transform.forward;

        direction.Normalize();

        Quaternion baseRotation = Quaternion.LookRotation(direction, Vector3.up);

        float step = (projectileCount > 1) ? (spread * 2f) / (projectileCount - 1) : 0f;
        float startAngle = -spread;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (projectileCount == 1) ? 0f : startAngle + (step * i);
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f) * baseRotation;
            Vector3 dir = rotation * Vector3.forward;

            EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPoint.position, rotation).GetComponent<EnemyProjectile>();
            projectile.Init(dir, projectileSpeed, projectileDamage);
        }
    }
}
