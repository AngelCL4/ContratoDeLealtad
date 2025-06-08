using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public Image backgroundImage;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;
    public AudioSource musicSource;

    private Queue<string> sentences; // Cola de frases
    private System.Action onComplete; // Callback al finalizar el diálogo
    private bool isDialogueActive = false; // Controla si hay un diálogo en curso
    private bool isTyping = false;
    private Coroutine typingCoroutine;  

    private void Awake()
    {
        sentences = new Queue<string>();
    }

    public void StartDialogue(string jsonFileName, System.Action onComplete)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Dialogues/{jsonFileName}");
        if (jsonAsset == null)
        {
            Debug.LogError($"No se encontró el archivo JSON: {jsonFileName}");
            onComplete?.Invoke(); // Llamar a la función de finalización aunque no haya diálogo
            return;
        }

        DialogueData dialogueData = JsonUtility.FromJson<DialogueData>(jsonAsset.text);

        // Cambiar el fondo
        Sprite bgSprite = Resources.Load<Sprite>($"Backgrounds/{dialogueData.background}");
        if (bgSprite != null)
        {
            backgroundImage.sprite = bgSprite;
        }

        // Preparar el diálogo
        this.onComplete = onComplete;
        sentences.Clear();
        foreach (string sentence in dialogueData.lines)
        {
            sentences.Enqueue(sentence);
        }

        if (!string.IsNullOrEmpty(dialogueData.music))
        {
            MusicManager.Instance.PlayMusic(dialogueData.music);
        }

        dialoguePanel.SetActive(true);
        isDialogueActive = true;
        DisplayNextSentence();
    }

    private void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Mostrar la línea completa inmediatamente
                SkipToFullText();
            }
            else
            {
                DisplayNextSentence();
            }
        }
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
    
        typingCoroutine = StartCoroutine(EscribirTextoProgresivo(dialogueText, sentence));
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        backgroundImage.gameObject.SetActive(false); // Ocultar la imagen de fondo
        dialogueText.gameObject.SetActive(false);
        isDialogueActive = false;
        onComplete?.Invoke(); // Llamar al callback cuando termine el diálogo
    }

    private float GetDelayPorVelocidad()
    {
        string velocidad = PlayerPrefs.GetString("VelocidadTexto", "Normal");

        switch (velocidad)
        {
            case "Lenta": return 0.1f;
            case "Rapida": return 0f;
            default: return 0.05f;
        }
    }

    private void SkipToFullText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = fullSentenceBeingTyped;
        isTyping = false;
    }

    private string fullSentenceBeingTyped = "";

    private IEnumerator EscribirTextoProgresivo(TextMeshProUGUI textoUI, string textoCompleto)
    {
        isTyping = true;
        textoUI.text = "";
        fullSentenceBeingTyped = textoCompleto;

        float delay = GetDelayPorVelocidad();

        foreach (char letra in textoCompleto)
        {
            textoUI.text += letra;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
    }

}

[System.Serializable]
public class DialogueData
{
    public string background;
    public string music;
    public List<string> lines;
}
