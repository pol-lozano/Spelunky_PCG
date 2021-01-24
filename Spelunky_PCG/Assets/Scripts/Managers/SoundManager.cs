using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance = null;

    public AudioSource sfxSource;
    public AudioSource musicSource;

    //Min ammount the pitch will be shifted to, to make sounds feel a bit more random.
    public float minPitch = .95f;
    public float maxPitch = 1.05f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(this);
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        sfxSource.volume = 0.25f;
        musicSource.volume = 0.25f;
    }

    public void PlayClip(AudioClip clip)
    {
        sfxSource.clip = clip;
        sfxSource.Play();
    }

    public void PlayRandomClip(params AudioClip[] clips)
    {
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.clip = clips[Random.Range(0, clips.Length)];
        sfxSource.Play();
    }
}
