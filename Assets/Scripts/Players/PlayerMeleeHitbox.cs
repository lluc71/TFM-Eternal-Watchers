using UnityEngine;
using System.Collections.Generic;

public class PlayerMeleeHitbox : MonoBehaviour
{
    public float damage = 10f;

    private HashSet<EnemyBasic> hitBasicEnemies = new HashSet<EnemyBasic>();
    private HashSet<EnemyBossController> hitBossEnemies = new HashSet<EnemyBossController>();

    private void OnEnable()
    {
        hitBasicEnemies.Clear();
        hitBossEnemies.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        CheckBasicEnemy(other);
        CheckBossEnemy(other);
    }

    private void CheckBasicEnemy(Collider other)
    {
        EnemyBasic enemyBasic = other.GetComponentInParent<EnemyBasic>();

        if (enemyBasic == null) return;
        if (hitBasicEnemies.Contains(enemyBasic)) return;

        enemyBasic.TakeDamage(damage);
        hitBasicEnemies.Add(enemyBasic);
    }

    private void CheckBossEnemy(Collider other)
    {
        EnemyBossController bossEnemy = other.GetComponentInParent<EnemyBossController>();

        if (bossEnemy == null) return;
        if (hitBossEnemies.Contains(bossEnemy)) return;

        bossEnemy.TakeDamage(damage);
        hitBossEnemies.Add(bossEnemy);
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}