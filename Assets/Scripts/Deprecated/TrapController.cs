using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TrapController : MonoBehaviour
{
    [Header("Trap Settings")]
    public int damageAmount = 10;
    public bool destroyAfterHit = false;


    private void OnTriggerEnter(Collider other)
    {
        other.GetComponent<MovementController>()?.TakeDamage(10f);

        if (destroyAfterHit) Destroy(gameObject);
    }
}
