using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2.5f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private GameObject impactVFX;

    private Vector3 dir;
    private float speed;
    private float damage;

    public void Init(Vector3 direction, float projectileSpeed, float projectileDamage)
    {
        dir = direction.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other) return;

        //Detectamos colisiones con paredes
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            SpawnImpactVFX(other);
            Destroy(gameObject);
            return;
        }

        //Detectamos colisiones con enemigos
        if (!other.CompareTag("Enemy")) return;

        EnemyBasic enemy = other.GetComponentInParent<EnemyBasic>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);
        SpawnImpactVFX(other);

        Destroy(gameObject);
    }

    private void SpawnImpactVFX(Collider col)
    {
        if (!impactVFX) return;

        Vector3 point = col.ClosestPoint(transform.position);
        Quaternion rot = Quaternion.LookRotation(-dir, Vector3.up);

        Instantiate(impactVFX, point, rot);
    }
}
