using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiClickSound : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip clip;

    void Awake()
    {
        var toggle = GetComponent<Toggle>();
        if (toggle != null) toggle.onValueChanged.AddListener(_ => Play());
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (GetComponent<Slider>() != null) Play();
    }

    void Play()
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }
}