using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BootManager : MonoBehaviour
{
    [SerializeField] private GameObject pressAnyKeyUI;  // Texto de "Presiona cualquier tecla"
    [SerializeField] private GameObject mainMenuUI;     // El menú principal
    [Header("Sequential Fade-in Settings")]
    [SerializeField] private Image[] fadeImages; // Assign your 7 UI images here
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float delayBetweenFades = 0.5f;
    private bool readyForInput = false;
    private TextMeshProUGUI pressText;

    private void Start()
    {
        mainMenuUI.SetActive(false); // Esconde el menú al principio
        pressAnyKeyUI.SetActive(false); // Esconde el texto de "Presiona cualquier tecla"
        pressText = pressAnyKeyUI.GetComponent<TextMeshProUGUI>();
        // Make sure images start transparent
        foreach (var img in fadeImages)
        {
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
        MusicManager.Instance.PlayMusic("MainTheme");
        StartCoroutine(PlayBootSequence());
    }

    private System.Collections.IEnumerator PlayBootSequence()
    {
        yield return StartCoroutine(FadeImagesSequence());

        // Show "Press Any Key" after fade-in
        pressAnyKeyUI.SetActive(true);
        readyForInput = true;
        StartCoroutine(BlinkText());
    }

     private IEnumerator FadeImagesSequence()
    {
        for (int i = 0; i < fadeImages.Length; i++)
        {
            yield return StartCoroutine(FadeInImage(fadeImages[i]));
            yield return new WaitForSeconds(delayBetweenFades);
        }
    }

    private IEnumerator BlinkText()
    {
        while (readyForInput) // Keep blinking until player presses a key
        {
            float timer = 0f;
            while (timer < 0.5f) // Fade out
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / 0.5f);
                SetTextAlpha(alpha);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f); // Small delay before fade-in

            timer = 0f;
            while (timer < 0.5f) // Fade in
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
                SetTextAlpha(alpha);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void SetTextAlpha(float alpha)
    {
        Color c = pressText.color;
        c.a = alpha;
        pressText.color = c;
    }

    private IEnumerator FadeInImage(Image img)
    {
        float timer = 0f;
        Color color = img.color;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Clamp01(timer / fadeDuration);
            img.color = color;
            yield return null;
        }
        color.a = 1f;
        img.color = color;
    }

    private void Update()
    {
        if (readyForInput && Input.anyKeyDown)
        {
            // Cuando se pulsa cualquier tecla, mostrar el menú
            pressAnyKeyUI.SetActive(false); // Ocultar el mensaje de tecla
            readyForInput = false; // Stop the blinking coroutine
            mainMenuUI.SetActive(true); // Mostrar el menú principal
        }
    }
}
