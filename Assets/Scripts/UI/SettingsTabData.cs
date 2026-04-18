using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SettingsTabData
{
    public string id;

    [Header("Tab UI")]
    public Button tabButton;
    public GameObject tabFocus;

    [Header("Content")]
    public GameObject contentPanel;
    public Selectable firstSelectedInPanel;
}
