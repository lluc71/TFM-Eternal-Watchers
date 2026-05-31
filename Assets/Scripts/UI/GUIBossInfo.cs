using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIBossInfo : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    [SerializeField] private RectTransform markersParent;
    [SerializeField] private GameObject markerPrefab;

    private List<GameObject> spawnedMarkers = new();

    public void Setup(float maxHealth, int phaseCount)
    {
        UpdateHealth(maxHealth, maxHealth);
        AddPhaseMarkers(phaseCount);
    }

    public void UpdateHealth(float health, float maxHealth)
    {
        if (healthSlider == null) return;
        healthSlider.value = Mathf.Clamp01(health / maxHealth);
    }

    /**
     * Añade marcas en el Slider para diferenciar las distintas fases del Boss
     */
    private void AddPhaseMarkers(int phaseCount)
    {
        ClearMarkers();

        if (markersParent == null || markerPrefab == null || phaseCount <= 1) return;

        int markerCount = phaseCount - 1;

        for (int i = 1; i <= markerCount; i++)
        {
            float normalizedPosition = (float)i / phaseCount;

            GameObject marker = Instantiate(markerPrefab, markersParent);
            RectTransform rect = marker.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin = new Vector2(normalizedPosition, 0f);
                rect.anchorMax = new Vector2(normalizedPosition, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            spawnedMarkers.Add(marker);
        }
    }

    /**
     * Limpiamos las Marcas por si en una partida aparecen 2 o mas Bosses
     */
    private void ClearMarkers()
    {
        foreach (GameObject marker in spawnedMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }

        spawnedMarkers.Clear();
    }
}
