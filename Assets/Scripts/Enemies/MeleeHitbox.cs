using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    public float damage = 10f;

    private bool hasHit = false;

    private void OnEnable()
    {
        hasHit = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<MovementController>()?.TakeDamage(damage);
            hasHit = true;
        }
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}
