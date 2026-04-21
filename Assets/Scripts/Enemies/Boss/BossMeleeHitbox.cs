using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(Collider))]
public class BossMeleeHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private float damage;
    private bool isActive = false;

    private readonly HashSet<MovementController> alreadyHit = new HashSet<MovementController>();
    private Coroutine activeRoutine;

    public void Initialize(float bossDamage)
    {
        damage = bossDamage;

        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false;
    }

    public void ActivateHitbox(float activeTime)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ActivateRoutine(activeTime));
    }

    public void Disable()
    {
        isActive = false;
        alreadyHit.Clear();

        hitboxCollider.enabled = false;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    /**
     * Activamos el hitboxCollider y esperamos el tiempo indicado.
     */
    private IEnumerator ActivateRoutine(float activeTime)
    {
        alreadyHit.Clear();
        isActive = true;
        hitboxCollider.enabled = true;

        yield return new WaitForSeconds(activeTime);

        hitboxCollider.enabled = false;
        isActive = false;
        alreadyHit.Clear();
        activeRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        MovementController player = other.GetComponent<MovementController>();
        if (player == null) return;

        if (alreadyHit.Contains(player)) return;

        alreadyHit.Add(player);
        player.TakeDamage(damage);
    }

}
