using UnityEngine;

public class SceneMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip sceneMusic;

    private void Start()
    {
        MusicManager.Instance.PlayMusic(sceneMusic);
    }
}
