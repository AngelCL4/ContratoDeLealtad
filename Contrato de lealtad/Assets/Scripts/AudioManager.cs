using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource efectosSource;

    void Start()
    {
        float volumenMusica = PlayerPrefs.GetFloat("VolumenMusica", 0.5f); // 0.5f por defecto
        float volumenMusica2 = PlayerPrefs.GetFloat("VolumenMusica2", 0.5f); // 0.5f por defecto
        float volumenEfectos = PlayerPrefs.GetFloat("VolumenEfectos", 0.5f);

        efectosSource.volume = volumenEfectos;
    }

    public void CambiarVolumenMusica(float valor)
    {
        PlayerPrefs.SetFloat("VolumenMusica", valor);
        PlayerPrefs.SetFloat("VolumenMusica2", valor);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ActualizarVolumenMusica(valor, valor);
        }
    }

    public void CambiarVolumenEfectos(float valor)
    {
        efectosSource.volume = valor;
        PlayerPrefs.SetFloat("VolumenEfectos", valor);
    }

    public void ReproducirEfecto(AudioClip clip)
    {
        efectosSource.PlayOneShot(clip, efectosSource.volume);
    }
}
