
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlobalClickSound : MonoBehaviour
{
    [Header("Click Sound")]
    [SerializeField] private AudioClip clickSound;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.35f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        if (clickSound == null ||
            EventSystem.current == null)
            return;

        bool pressed = Input.GetMouseButtonDown(0);

        if (!pressed) return;

        if (!EventSystem.current.IsPointerOverGameObject())
            return;

        GameObject selected =
            EventSystem.current.currentSelectedGameObject;

        if (selected != null)
        {
            Button button = selected.GetComponent<Button>();

            if (button != null && !button.interactable)
                return;
        }

        audioSource.PlayOneShot(clickSound, volume);
    }
}
