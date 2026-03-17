using UnityEngine;
using System.Collections.Generic;

public class PlayerMeleeHitbox : MonoBehaviour
{
    public float damage = 10f;

    private HashSet<EnemyBasic> hitEnemies = new HashSet<EnemyBasic>();

    private void OnEnable()
    {
        hitEnemies.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        EnemyBasic enemy = other.GetComponentInParent<EnemyBasic>();
        
        if (enemy == null) return;
        if (hitEnemies.Contains(enemy)) return;

        enemy?.TakeDamage(damage);
        hitEnemies.Add(enemy);
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}