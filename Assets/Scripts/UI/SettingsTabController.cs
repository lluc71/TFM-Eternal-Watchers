using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsTabController : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private SettingsTabData[] tabs;

    private int currentTabIndex = 0;

    public void OpenTab(int index)
    {
        if (tabs == null || tabs.Length == 0) return;
        if (index < 0 || index >= tabs.Length) return;
        if (index == currentTabIndex) return;

        currentTabIndex = index;

        foreach (SettingsTabData tab in tabs)
        {
            bool isActive = (tab == tabs[currentTabIndex]);

            if (tab.tabFocus != null)
                tab.tabFocus.SetActive(isActive);

            if (tab.contentPanel != null)
                tab.contentPanel.SetActive(isActive);
        }
    }

    /**
     * Sirve para configurar la Navegacion UI por Gamepad al abrir el panel
     */
    public void SelectCurrentTabContent()
    {
        if (tabs == null || tabs.Length == 0) return;

        Selectable first = tabs[currentTabIndex].firstSelectedInPanel;
        if (first == null) return;
        if (!first.gameObject.activeInHierarchy) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

}
