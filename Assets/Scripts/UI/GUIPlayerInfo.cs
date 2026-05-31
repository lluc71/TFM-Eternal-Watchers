using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIPlayerInfo : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private List<GameObject> shieldAuras = new List<GameObject>();

    public void UpdateHealth(float health, float maxHealth)
    {
        if (healthSlider == null) return;
        healthSlider.value = Mathf.Clamp01(health / maxHealth);
    }

    public void UpdateShields(int currentShieldHits)
    {
        for (int i = 0; i < shieldAuras.Count; i++)
        {
            shieldAuras[i].SetActive(i >= currentShieldHits);
        }
    }
}
