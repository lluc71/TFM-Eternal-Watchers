using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class AltarController : MonoBehaviour
{
    private const string PLAYER_TAG = "Player";

    [Header("Healing")]
    [SerializeField] private float totalHeal = 80f;
    [SerializeField] private float healPerTick = 10f;
    [SerializeField] private float tickRate = 1f;
    [SerializeField] private float duration = 20f;

    [Header("VFX")]
    [SerializeField] private GameObject healingVFX;

    private List<MovementController> players = new();
    private bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;

        MovementController player = other.GetComponent<MovementController>();
        if (player == null) return;

        if (!players.Contains(player))
            players.Add(player);

        if (!used)
            StartCoroutine(HealRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        MovementController player = other.GetComponent<MovementController>();

        if (player != null)
            players.Remove(player);
    }

    //Mantiene la curación 'duration' segundos
    private IEnumerator HealRoutine()
    {
        used = true;
        healingVFX?.SetActive(true);
        float timer = 0f;

        while (totalHeal > 0f && timer < duration)
        {
            players.RemoveAll(p => p == null || p.isDead);

            if (players.Count > 0)
            {
                float amount = Mathf.Min(healPerTick, totalHeal);

                foreach (MovementController player in players)
                {
                    float healed = player.Heal(amount);
                    totalHeal -= healed;

                    if (totalHeal <= 0f) break;
                }
            }

            timer += tickRate;
            yield return new WaitForSeconds(tickRate);
        }

        healingVFX?.SetActive(false);
        GetComponent<Collider>().enabled = false;
    }
}
