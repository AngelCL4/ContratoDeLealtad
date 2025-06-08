using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ConversationManager : MonoBehaviour
{

    public static ConversationManager Instancia { get; private set; }

    public Image backgroundImage;
    public Image leftCharacterImage;
    public Image rightCharacterImage;
    public TextMeshProUGUI leftdialogueText;
    public TextMeshProUGUI rightdialogueText;
    public GameObject leftconversationPanel;
    public GameObject rightconversationPanel;
    public ChapterVisits chapterVisits;
    public bool VisitConversationActive { get; set; } = false;
    private Dictionary<string, bool> conversationSeenTracker = new Dictionary<string, bool>();
    private Objeto rewardActual;

    private Queue<DialogueLine> dialogueQueue;
    private System.Action onComplete; // Callback al finalizar el diálogo
    private bool isConversationActive = false; // Controla si hay un diálogo en curso
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string textoCompletoActual = "";
    private TextMeshProUGUI textoUIActual = null;

    private ChapterEvents loadedChapterEvents;
    private bool chapterEventsLoaded = false;
    private bool eventConversationFinished = false;

    private void Awake()
    {
        dialogueQueue = new Queue<DialogueLine>();
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);
    }

    public void StartConversation(string jsonFileName, System.Action onComplete)
    {
        Debug.Log($"Intentando cargar archivo JSON: {jsonFileName}");
        this.onComplete = onComplete;
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Dialogues/{jsonFileName}");
        if (jsonAsset == null)
        {
            Debug.LogError($"No se encontró el archivo JSON: {jsonFileName}");
            onComplete?.Invoke(); // Llamar a la función de finalización aunque no haya diálogo
            return;
        }


        ConversationData conversationData = JsonUtility.FromJson<ConversationData>(jsonAsset.text);
        Debug.Log($"Diálogos encontrados: {conversationData.dialogue.Count}");

        rewardActual = conversationData.reward;

        // Preparar el diálogo
        dialogueQueue.Clear();
        foreach (DialogueLine line in conversationData.dialogue)
        {
            dialogueQueue.Enqueue(line);
        }

        // Cargar las imágenes de los personajes iniciales
        bool hayIzquierda = PreloadInitialPortraits(conversationData.dialogue);

        bool hayCambioDeFondo = conversationData.dialogue.Any(line => !string.IsNullOrEmpty(line.backgroundChange));
        backgroundImage.gameObject.SetActive(hayCambioDeFondo);
        leftCharacterImage.gameObject.SetActive(hayIzquierda);
        rightCharacterImage.gameObject.SetActive(true);

        isConversationActive = true;
        DisplayNextLine();
    }

    public void LoadChapterEvents(string jsonFileName)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Dialogues/{jsonFileName}");
        if (jsonAsset == null)
        {
            Debug.LogError($"No se encontró el archivo de eventos: {jsonFileName}");
            return;
        }

        loadedChapterEvents = JsonUtility.FromJson<ChapterEvents>(jsonAsset.text);
        chapterEventsLoaded = true;
        Debug.Log($"Eventos del capítulo {jsonFileName} cargados correctamente.");
    }

    public ChapterVisits LoadChapterVisits(string chapterVisitsFileName)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Dialogues/{chapterVisitsFileName}");

        if (jsonAsset == null)
        {
            Debug.LogError($"No se encontró el archivo de visitas: {chapterVisitsFileName}");
            return null;
        }

        ChapterVisits chapterVisits = JsonUtility.FromJson<ChapterVisits>(jsonAsset.text);
        Debug.Log($"Visitas del capítulo {chapterVisitsFileName} cargados correctamente.");
        return chapterVisits;
    }

    public void StartEventConversation(int turno, System.Action onComplete)
    {

        if (!chapterEventsLoaded)
        {
            Debug.LogWarning("Los eventos del capítulo no han sido cargados todavía.");
            onComplete?.Invoke();
            return;
        }

        EventConversation evento = loadedChapterEvents.eventConversations.Find(e => e.turno == turno);

        if (evento == null)
        {
            Debug.Log($"No hay evento para el turno {turno}");
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"Iniciando conversación de evento en turno {turno}.");

        dialogueQueue.Clear();
        foreach (DialogueLine line in evento.dialogue)
        {
            dialogueQueue.Enqueue(line);
        }

        bool hayIzquierda = PreloadInitialPortraits(evento.dialogue);

        bool hayCambioDeFondo = evento.dialogue.Any(line => !string.IsNullOrEmpty(line.backgroundChange));
        backgroundImage.gameObject.SetActive(hayCambioDeFondo);
        leftCharacterImage.gameObject.SetActive(hayIzquierda);
        rightCharacterImage.gameObject.SetActive(true);

        isConversationActive = true;
        this.onComplete = onComplete;

        DisplayNextLine();
    }

    public bool IsEventConversationFinished()
    {
        return eventConversationFinished;
    }

    private void Update()
    {
        if (isConversationActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                SkipToFullText(); // 👈 Muestra todo el texto de inmediato
            }
            else
            {
                DisplayNextLine(); // 👈 Avanza a la siguiente línea
            }
        }
    }

    private bool PreloadInitialPortraits(List<DialogueLine> dialogueLines)
    {
        string firstLeftPortrait = null;
        string firstRightPortrait = null;
        string firstLeftDirection = "left";
        string firstRightDirection = "left";

        foreach (DialogueLine line in dialogueLines)
        {
            if (line.position == "left" && firstLeftPortrait == null)
            {
                firstLeftPortrait = line.portrait;
                firstLeftDirection = line.facingDirection;
            }
            else if (line.position == "right" && firstRightPortrait == null)
            {
                firstRightPortrait = line.portrait;
                firstRightDirection = line.facingDirection;
            }

            if (firstLeftPortrait != null && firstRightPortrait != null)
                break;
        }

        if (!string.IsNullOrEmpty(firstLeftPortrait))
        {
            Sprite leftSprite = Resources.Load<Sprite>($"Portraits/{firstLeftPortrait}");
            if (leftSprite != null)
            {
                leftCharacterImage.sprite = leftSprite;
                SetFacingDirection(leftCharacterImage, firstLeftDirection);
            }
        }

        if (!string.IsNullOrEmpty(firstRightPortrait))
        {
            Sprite rightSprite = Resources.Load<Sprite>($"Portraits/{firstRightPortrait}");
            if (rightSprite != null)
            {
                rightCharacterImage.sprite = rightSprite;
                SetFacingDirection(rightCharacterImage, firstRightDirection);
            }
        }

        // 👇 Devolvemos true si hay retrato izquierdo
        return !string.IsNullOrEmpty(firstLeftPortrait);
    }

    public void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndConversation();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();

        // Cambiar el fondo si el JSON lo indica
        if (!string.IsNullOrEmpty(line.backgroundChange))
        {
            Sprite bgSprite = Resources.Load<Sprite>($"Backgrounds/{line.backgroundChange}");
            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
            }
        }

        // Cargar retrato del personaje
        Sprite portraitSprite = Resources.Load<Sprite>($"Portraits/{line.portrait}");

        if (portraitSprite != null)
        {
            if (line.position == "left")
            {
                leftCharacterImage.sprite = portraitSprite;
                SetFacingDirection(leftCharacterImage, line.facingDirection);
            }
            else if (line.position == "right")
            {
                rightCharacterImage.sprite = portraitSprite;
                SetFacingDirection(rightCharacterImage, line.facingDirection);
            }
        }

        // Ajustar opacidad: el que habla se mantiene normal, el otro se oscurece
        Color activeColor = new Color(1f, 1f, 1f, 1f); // Normal
        Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Oscurecido

        if (!string.IsNullOrEmpty(line.music))
        {
            MusicManager.Instance.PlayMusic(line.music);
        }

        if (line.position == "left")
        {
            leftCharacterImage.color = activeColor;
            rightCharacterImage.color = inactiveColor;
            leftconversationPanel.SetActive(true);
            rightconversationPanel.SetActive(false);
            leftdialogueText.gameObject.SetActive(true);
            rightdialogueText.gameObject.SetActive(false);
            // Mostrar el texto
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            textoUIActual = leftdialogueText; // o rightdialogueText según el caso
            textoCompletoActual = line.text;
            typingCoroutine = StartCoroutine(EscribirTextoProgresivo(textoUIActual, textoCompletoActual));
        }
        else
        {
            rightCharacterImage.color = activeColor;
            leftCharacterImage.color = inactiveColor;
            leftconversationPanel.SetActive(false);
            rightconversationPanel.SetActive(true);
            leftdialogueText.gameObject.SetActive(false);
            rightdialogueText.gameObject.SetActive(true);
            // Mostrar el texto
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            textoUIActual = rightdialogueText; // o rightdialogueText según el caso
            textoCompletoActual = line.text;
            typingCoroutine = StartCoroutine(EscribirTextoProgresivo(textoUIActual, textoCompletoActual));
        }
    }

    private void SkipToFullText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (textoUIActual != null)
            textoUIActual.text = textoCompletoActual;

        isTyping = false;
    }

    private void SetFacingDirection(Image characterImage, string direction)
    {
        if (direction == "right")
        {
            characterImage.rectTransform.localScale = new Vector3(-1, 1, 1); // Invierte horizontalmente
        }
        else
        {
            characterImage.rectTransform.localScale = new Vector3(1, 1, 1); // Mantiene la orientación normal
        }
    }

    private void EndConversation()
    {
        leftconversationPanel.SetActive(false);
        rightconversationPanel.SetActive(false);
        backgroundImage.gameObject.SetActive(false); // Ocultar la imagen de fondo
        leftdialogueText.gameObject.SetActive(false);
        rightdialogueText.gameObject.SetActive(false);
        leftCharacterImage.gameObject.SetActive(false);
        rightCharacterImage.gameObject.SetActive(false);
        isConversationActive = false;
        if (!string.IsNullOrEmpty(rewardActual.nombre))
        {
            AlmacenObjetos.Instance.AñadirObjeto(rewardActual);
            Debug.Log($"Recompensa otorgada: {rewardActual.nombre}"); // Asegúrate de que `Objeto` tenga un nombre o algo similar
            rewardActual = null;
        }
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

    private IEnumerator EscribirTextoProgresivo(TextMeshProUGUI textoUI, string textoCompleto)
    {
        isTyping = true;
        textoUI.text = "";
        float delay = GetDelayPorVelocidad();

        foreach (char letra in textoCompleto)
        {
            textoUI.text += letra;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
    }

    public VisitData BuscarVisitaEnCoordenada(float x, float y)
    {
        return chapterVisits?.visits.Find(v => v.x == x && v.y == y);
    }

    public void StartRetreatQuote(string nombre, System.Action onComplete)
    {
        Debug.Log($"Intentando cargar diálogo de retirada para: {nombre}");
        this.onComplete = onComplete;

        TextAsset jsonAsset = Resources.Load<TextAsset>("Dialogues/RetreatQuotes");
        if (jsonAsset == null)
        {
            Debug.LogError("No se encontró el archivo JSON: RetreatQuotes");
            onComplete?.Invoke();
            return;
        }

        RetreatQuoteList quoteList = JsonUtility.FromJson<RetreatQuoteList>(jsonAsset.text);
        RetreatQuoteEntry quote = quoteList.quotes.Find(q => q.character == nombre);

        if (quote == null)
        {
            Debug.LogWarning($"No se encontró diálogo de retirada para el personaje: {nombre}");
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"Diálogo de retirada encontrado para {nombre}, líneas: {quote.dialogue.Count}");

        dialogueQueue.Clear();
        foreach (DialogueLine line in quote.dialogue)
        {
            dialogueQueue.Enqueue(line);
        }

        bool hayIzquierda = PreloadInitialPortraits(quote.dialogue);
        bool hayFondo = quote.dialogue.Any(line => !string.IsNullOrEmpty(line.backgroundChange));

        backgroundImage.gameObject.SetActive(hayFondo);
        leftCharacterImage.gameObject.SetActive(hayIzquierda);
        rightCharacterImage.gameObject.SetActive(true);

        isConversationActive = true;
        DisplayNextLine();
    }

    public void StartFightConversation(string attackerName, string chapterNumber, System.Action onComplete)
    {
        string jsonFileName = $"{chapterNumber}Boss{attackerName}";
        Debug.Log($"Intentando cargar conversación de jefe: {jsonFileName}");

        this.onComplete = onComplete;

        TextAsset jsonAsset = Resources.Load<TextAsset>($"Dialogues/{jsonFileName}");

        if (jsonAsset == null)
        {
            Debug.LogWarning($"No se encontró conversación de jefe para {attackerName} atacando al jefe en capítulo {chapterNumber}.");
            onComplete?.Invoke(); // Si no hay conversación, continuar normalmente
            return;
        }

        FightDialogueData conversationData = JsonUtility.FromJson<FightDialogueData>(jsonAsset.text);

        // Verifica si ya se ha visto la conversación
        if (conversationSeenTracker.ContainsKey(jsonFileName) && conversationSeenTracker[jsonFileName])
        {
            // Si ya se ha visto, termina sin hacer nada
            Debug.Log("La conversación ya ha sido vista.");
            onComplete?.Invoke();
            return;
        }

        // Marca la conversación como vista en el diccionario (no en el JSON)
        conversationSeenTracker[jsonFileName] = true;

        dialogueQueue.Clear();
        foreach (DialogueLine line in conversationData.dialogue)
        {
            dialogueQueue.Enqueue(line);
        }

        bool hayIzquierda = PreloadInitialPortraits(conversationData.dialogue);
        bool hayCambioDeFondo = conversationData.dialogue.Any(line => !string.IsNullOrEmpty(line.backgroundChange));

        backgroundImage.gameObject.SetActive(hayCambioDeFondo);
        leftCharacterImage.gameObject.SetActive(hayIzquierda);
        rightCharacterImage.gameObject.SetActive(true);

        isConversationActive = true;
        DisplayNextLine();
    }
}



[System.Serializable]
public class ConversationData
{
    public List<DialogueLine> dialogue;
    public Objeto reward;
}


[System.Serializable]
public class DialogueLine
{
    public string backgroundChange; // Permite cambiar el fondo en medio de la conversación
    public string character;
    public string portrait;
    public string position; // Nueva variable para definir izquierda o derecha
    public string facingDirection; // "left" o "right"
    public string text;
    public string music;
}

[System.Serializable]
public class FightDialogueData
{
    public bool seen;
    public List<DialogueLine> dialogue;
}


[System.Serializable]
public class EventConversation
{
    public int turno;
    public List<DialogueLine> dialogue;
}

[System.Serializable]
public class ChapterEvents
{
    public List<EventConversation> eventConversations;
}

[System.Serializable]
public class VisitData
{
    public float x;
    public float y;
    public string conversation;
    public Objeto reward;
    public bool visited;
}

[System.Serializable]
public class ChapterVisits
{
    public List<VisitData> visits;
}

[System.Serializable]
public class RetreatQuoteEntry
{
    public string character;
    public List<DialogueLine> dialogue;
}

[System.Serializable]
public class RetreatQuoteList
{
    public List<RetreatQuoteEntry> quotes;
}