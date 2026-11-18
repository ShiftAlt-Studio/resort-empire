
using System.Collections;
using UnityEngine;

public class AmbientSoundManager : MonoBehaviour
{
    [Header("Ocean")]
    [SerializeField] private AudioClip oceanClip;
    [SerializeField, Range(0f, 1f)]
    private float oceanVolume = 0.12f;

    [Header("Seagulls")]
    [SerializeField] private AudioClip[] seagullClips;
    [SerializeField, Range(0f, 1f)]
    private float seagullVolume = 0.18f;
    [SerializeField] private float minInterval = 8f;
    [SerializeField] private float maxInterval = 20f;

    private AudioSource oceanSource;
    private AudioSource birdSource;

    private void Awake()
    {
        oceanSource = gameObject.AddComponent<AudioSource>();
        oceanSource.playOnAwake = false;
        oceanSource.loop = true;
        oceanSource.spatialBlend = 0f;
        oceanSource.volume = oceanVolume;

        birdSource = gameObject.AddComponent<AudioSource>();
        birdSource.playOnAwake = false;
        birdSource.loop = false;
        birdSource.spatialBlend = 0f;
        birdSource.volume = seagullVolume;
    }

    private void Start()
    {
        if (oceanClip != null)
        {
            oceanSource.clip = oceanClip;
            oceanSource.Play();
        }

        StartCoroutine(SeagullRoutine());
    }

    private IEnumerator SeagullRoutine()
    {
        while (true)
        {
            float delay = Random.Range(
                Mathf.Max(0.1f, minInterval),
                Mathf.Max(minInterval, maxInterval)
            );

            yield return new WaitForSeconds(delay);

            if (seagullClips == null || seagullClips.Length == 0)
                continue;

            AudioClip clip = seagullClips[
                Random.Range(0, seagullClips.Length)
            ];

            if (clip != null)
                birdSource.PlayOneShot(clip);
        }
    }
}
