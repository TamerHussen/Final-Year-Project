using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource tensionSource;

    [Header("Mixer")]
    public AudioMixerGroup musicGroup;

    [Header("Music Tracks")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    public AudioClip tensionMusic;

    [Header("Settings")]
    public float targetVolume = 0.3f;
    public float fadeSpeed = 1.5f;

    [Header("Tension Trigger")]
    public float tensionDistance = 12f;


    private Transform playerTransform;
    private Transform predatorTransform;
    private bool tensionActive = false;

    private void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            musicSource = sources[0];
            tensionSource = sources[1];
        }
        else
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            tensionSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;

        tensionSource.loop = true;
        tensionSource.playOnAwake = false;
        tensionSource.spatialBlend = 0f;
        tensionSource.volume = 0f;

        musicSource.ignoreListenerPause = true;
        tensionSource.ignoreListenerPause = true;

        if (musicGroup != null)
        {
            musicSource.outputAudioMixerGroup = musicGroup;
            tensionSource.outputAudioMixerGroup = musicGroup;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioClip trackToPlay = menuMusic != null ? menuMusic : gameMusic;
        if (trackToPlay != null)
        {
            musicSource.clip = trackToPlay;
            musicSource.Play();
            StartCoroutine(FadeIn(musicSource, targetVolume, fadeSpeed));
        }
        if (tensionMusic != null)
        {
            tensionSource.clip = tensionMusic;
            tensionSource.volume = 0f;
            tensionSource.Play();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        GameObject predObj = GameObject.FindGameObjectWithTag("Predator");
        if (predObj != null) predatorTransform = predObj.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (tensionMusic == null || predatorTransform == null || playerTransform == null) return;

        float dist = Vector3.Distance(playerTransform.position, predatorTransform.position);
        bool shouldTense = dist < tensionDistance;

        float targetTensionVol = shouldTense ? (targetVolume * 0.8f) : 0f;
        float targetMusicVol = shouldTense ? (targetVolume * 0.4f) : targetVolume;

        if (Time.deltaTime == 0f)
        {
            targetMusicVol *= 0.3f;
            targetTensionVol *= 0.3f;
        }
        float speed = (targetVolume / fadeSpeed) * Time.unscaledDeltaTime;

        tensionSource.volume = Mathf.MoveTowards(tensionSource.volume, targetTensionVol, speed);
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetMusicVol, speed);
    }

    IEnumerator FadeIn(AudioSource source, float targetVol, float duration)
    {
        source.volume = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
            yield return null;
        }
        source.volume = targetVol;
    }

    IEnumerator FadeTo(AudioSource source, float targetVol, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
            yield return null;
        }
        source.volume = targetVol;
    }

    public void OnPause() => StartCoroutine(FadeTo(musicSource, targetVolume * 0.3f, 0.5f));
    public void OnResume() => StartCoroutine(FadeTo(musicSource, targetVolume, 0.5f));
}
