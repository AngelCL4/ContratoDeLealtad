using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

[System.Serializable]
public class ChapterStatus
{
    public string chapterName;
    public bool completed;
    public string background;
    public string daytime;
    public string battleMusic;
    public string bossMusic;
    public string campMusic;
    public string unitpromoted;
}

[System.Serializable]
public class ChapterData
{
    public int contratos;
    public int entrenar;
    public List<ChapterStatus> chapters = new();
}

[System.Serializable]
public class UnlockedChapter
{
    public string chapterName;         // Nombre del capítulo
    public string chapterTitle;
    public string description;
    public bool estaDesbloqueado;       // Si el capítulo está desbloqueado
}

[System.Serializable]
public class UnlockedChaptersData
{
    public List<UnlockedChapter> chapters = new();
}

[System.Serializable]
public class RefuerzoEnemigo
{
    public Unidad unidad; // Clase que representa los datos base del enemigo
    public int turno;
    public float x;
    public float y;
}

[System.Serializable]
public class RefuerzoWrapper
{
    public List<RefuerzoEnemigo> refuerzos;
}

public class ChapterManager : MonoBehaviour
{

    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private ConversationManager conversationManager;
    [SerializeField] private GameObject pointer; // Pointer GameObject
    [SerializeField] private CameraController cameraController; // Referencia a CameraController
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private CuadriculaAccesibilidad cuadricula;
    [SerializeField] private TimeTilemap time;
    [SerializeField] private ContractMenu contractMenu;
    public string currentChapter;
    public string lastMainChapter;
    public static ChapterManager instance;
    public bool chapterCompleted;
    bool contratoCerrado = false;
    public List<RefuerzoEnemigo> refuerzos = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentChapter = GameManager.Instance.currentChapter;
        chapterCompleted = false;

        if (pointer != null) pointer.SetActive(false);
        if (cameraController != null) cameraController.enabled = false;

        StartCoroutine(LoadChapter());
    }

    private IEnumerator LoadChapter()
    {
        int chapterNumber = ExtraerNumeroDeCapitulo(currentChapter);
        Debug.Log("Empezando preludio.");
        // Iniciar Preludio
        bool preludioFinished = false;
        dialogueManager.StartDialogue(currentChapter + "Preludio", () => preludioFinished = true);
        yield return new WaitUntil(() => preludioFinished);
        Debug.Log("Preludio acabado.");
        // Cargar eventos 
        conversationManager.LoadChapterEvents(currentChapter + "Events");
        conversationManager.chapterVisits = conversationManager.LoadChapterVisits(currentChapter + "Visits");
        LoadReinforcements(currentChapter);

        // Iniciar Conversación con Callback
        bool conversationFinished = false;
        conversationManager.StartConversation(currentChapter + "Conversation", () => conversationFinished = true);
        yield return new WaitUntil(() => conversationFinished);
        Debug.Log("Conversacion finalizada");

        // ✅ Buscar el capítulo por nombre (para main o desvío) y marcarlo como completado
        var chapterData = GameManager.Instance.chapterDataJuego.chapters
            .FirstOrDefault(c => c.chapterName == currentChapter);

        if (currentChapter == "Chapter1")
        {
            contractMenu.gameObject.SetActive(true);
            contractMenu.OnCerrarContrato = () => contratoCerrado = true;
            yield return new WaitUntil(() => contratoCerrado);
        }

        // Cargar Mapa y esperar a que termine
        Debug.Log("Cargando mapa");
        mapLoader.LoadMapFromJson(currentChapter + "Map");
        Debug.Log("Mapa cargado");
        
        cuadricula.GenerarCuadricula();
        cuadricula.ActualizarOpacidad(cuadricula.opacidadGuardada);

        if (chapterData.daytime == "Night")
        {
            time.GenerarCuadriculaNoche();
        }
        else if (chapterData.daytime == "Evening")
        {
            time.GenerarCuadriculaTarde();
        }
        else
        {
            time.timeTilemap.ClearAllTiles();
        }

        if (pointer != null) pointer.SetActive(true);
        if (cameraController != null) cameraController.enabled = true;

        GameManager.Instance.ActualizarNivelMedioEjercito();
        
        // Aquí iría la jugabilidad
        unitSpawner.SpawnAliados();
        unitSpawner.SpawnEnemigos();
        StartCoroutine(TurnManager.Instancia.IniciarFaseJugador());

        if (!string.IsNullOrEmpty(chapterData.battleMusic))
        {
            MusicManager.Instance.PlayMusic(chapterData.battleMusic);
        }

        // Esperar a que el capítulo se complete
        Debug.Log("Esperando a que el capítulo se complete...");
        yield return new WaitUntil(() => chapterCompleted);


        if (chapterData != null)
        {
            chapterData.completed = true;
            if (!string.IsNullOrEmpty(chapterData.unitpromoted)){
                Unidad promociona = GameManager.Instance.datosJuego.unidades.FirstOrDefault(u => u.nombre == chapterData.unitpromoted);
                var tempGameObject = new GameObject("TempUnitLoader");
                var tempUnitLoader = tempGameObject.AddComponent<UnitLoader>();
                tempUnitLoader.ConfigurarUnidad(promociona, true);
                tempUnitLoader.Promocionar(promociona);
                Destroy(tempGameObject);
            }
            GameManager.Instance.fondoCampamento = chapterData.background;
            GameManager.Instance.musicaCampamento = chapterData.campMusic;
            Debug.Log($"Capítulo marcado como completado: {chapterData.chapterName}");
        }
        else
        {
            Debug.LogWarning($"No se encontró el capítulo en chapterDataJuego con nombre: {currentChapter}");
        }

        if (chapterNumber != -1)
        {
            if (chapterNumber < 15)
            {
                GameManager.Instance.DesbloquearCapitulo(chapterNumber);
            }

            if (chapterNumber <= 8)
            {
                GameManager.Instance.chapterDataJuego.contratos++;
            }

            lastMainChapter = currentChapter;
            GameManager.Instance.lastMainChapter = lastMainChapter;
        }

        GameManager.Instance.GuardarDatosPersistentes();

        List<string> nombresUnidades = TurnManager.Instancia.unidadesJugador.Select(u => u.datos.nombre).ToList();
        SupportManager.Instance.AfectoAlFinalizarNivel(nombresUnidades);

        foreach (var unidad in TurnManager.Instancia.unidadesJugador)
        {
            unidad.datos.PV = unidad.datos.MaxPV;
            Debug.Log($"[LIMPIEZA] {unidad.name} tiene {unidad.efectosArtefacto.Count} efectos pasivos");
            unidad.LimpiarBonosTerreno();
            unidad.LimpiarPotenciadores();
            unidad.LimpiarArtefactosPasivos();
            unidad.LimpiarEfectosEntrantes();
            Debug.Log($"[LIMPIEZA] {unidad.name} tiene {unidad.efectosArtefacto.Count} efectos pasivos");
        }

        GameManager.Instance.bossMusicSounding = false;

        // Iniciar Postludio
        Debug.Log("Empezando postludio");
        bool postludioFinished = false;
        conversationManager.StartConversation(currentChapter + "Postludio", () => postludioFinished = true);
        yield return new WaitUntil(() => postludioFinished);
        GameManager.Instance.ActualizarNivelMedioEjercito();
        GameManager.Instance.AjustarNivelUnidadesLibres();

        CalcularEntrenamientos(ExtraerNumeroDeCapitulo(GameManager.Instance.lastMainChapter));

        foreach (var chapter in GameManager.Instance.unlockedChapterDataJuego.chapters)
        {
            Debug.Log($"DEBUG FINAL: {chapter.chapterName} - {chapter.chapterTitle} - {chapter.description} - Desbloqueado: {chapter.estaDesbloqueado}");
        }
        GuardarUnidadesCaidas();
        GuardarEstadoUnidadesAlTerminarNivel();
        SupportManager.Instance.RevisarApoyosPendientes();
        SupportManager.Instance.RevisarDesviosPendientes();
        

        // Volver al campamento
        SceneLoader.Instance.LoadScene("CampScene");
        GestorTutoriales.instancia.DesbloquearTutorial(currentChapter);
        yield break;
    }

    public void CalcularEntrenamientos(int chapter)
    {
        if (chapter >= 8) GameManager.Instance.chapterDataJuego.entrenar = 3;
        if (chapter >= 4) GameManager.Instance.chapterDataJuego.entrenar = 2;
        if (chapter >= 1) GameManager.Instance.chapterDataJuego.entrenar = 1;
    }

    private int ExtraerNumeroDeCapitulo(string chapterName)
    {
        // Solo acepta capítulos exactamente como "Chapter1", "Chapter2", etc.
        if (System.Text.RegularExpressions.Regex.IsMatch(chapterName, @"^Chapter\d+$"))
        {
            string numeroComoTexto = chapterName.Substring("Chapter".Length);
            return int.TryParse(numeroComoTexto, out int result) ? result : -1;
        }

        return -1; // No es un capítulo principal
    }

    private void LoadReinforcements(string nombreCapitulo)
    {
        TextAsset archivo = Resources.Load<TextAsset>("Data/" + nombreCapitulo + "Reinforcements");
        if (archivo != null)
        {
            refuerzos = JsonUtility.FromJson<RefuerzoWrapper>(archivo.text).refuerzos;
        }
        else
        {
            Debug.LogWarning("No se encontró el archivo de refuerzos para " + nombreCapitulo);
        }
    }

    public void GuardarUnidadesCaidas()
    {
        var unidades = TurnManager.Instancia.unidadesAGuardar;

        foreach (Unidad unidad in unidades)
        {
            GuardarEstadoUnidad(unidad);
        }

        TurnManager.Instancia.unidadesAGuardar.Clear(); // Limpiamos la lista tras guardar
    }

    public void GuardarEstadoUnidadesAlTerminarNivel()
    {
        List<Unidad> personajesReclutados = SupportManager.Instance
            .GetPersonajesReclutados(GameManager.Instance.datosJuego.unidades.ToList());

        foreach (UnitLoader loader in FindObjectsOfType<UnitLoader>())
        {
            Unidad unidadEscena = loader.datos;

            // Verifica que unidadEscena no sea null
            if (unidadEscena == null || string.IsNullOrEmpty(unidadEscena.nombre))
            {
                Debug.LogWarning($"[SYNC] Unidad en escena nula o sin nombre en {loader.gameObject.name}");
                continue;
            }

            Unidad unidadJuego = personajesReclutados.FirstOrDefault(u => u != null && u.nombre == unidadEscena.nombre);

            if (unidadJuego != null)
            {
                // Sincroniza
                unidadJuego.nivel = unidadEscena.nivel;
                unidadJuego.experiencia = unidadEscena.experiencia;
                unidadJuego.MaxPV = unidadEscena.MaxPV;
                unidadJuego.poder = unidadEscena.poder;
                unidadJuego.habilidad = unidadEscena.habilidad;
                unidadJuego.velocidad = unidadEscena.velocidad;
                unidadJuego.suerte = unidadEscena.suerte;
                unidadJuego.defensa = unidadEscena.defensa;
                unidadJuego.resistencia = unidadEscena.resistencia;
            }
            else
            {
                Debug.LogWarning($"[SYNC] No se encontró unidad con nombre '{unidadEscena.nombre}' en los datos del jugador.");
            }
        }

        // Guardar si aplica
        GameManager.Instance.GuardarUnidadesEnJson(); // si tienes una función así
    }

    public void GuardarEstadoUnidad(Unidad unidadEscena)
    {
        if (unidadEscena == null || string.IsNullOrEmpty(unidadEscena.nombre))
        {
            Debug.LogWarning("[SYNC] Unidad nula o sin nombre.");
            return;
        }

        List<Unidad> personajesReclutados = SupportManager.Instance
            .GetPersonajesReclutados(GameManager.Instance.datosJuego.unidades.ToList());

        Unidad unidadJuego = personajesReclutados
            .FirstOrDefault(u => u != null && u.nombre == unidadEscena.nombre);

        if (unidadJuego != null)
        {
            unidadJuego.nivel = unidadEscena.nivel;
            unidadJuego.experiencia = unidadEscena.experiencia;
            unidadJuego.MaxPV = unidadEscena.MaxPV;
            unidadJuego.poder = unidadEscena.poder;
            unidadJuego.habilidad = unidadEscena.habilidad;
            unidadJuego.velocidad = unidadEscena.velocidad;
            unidadJuego.suerte = unidadEscena.suerte;
            unidadJuego.defensa = unidadEscena.defensa;
            unidadJuego.resistencia = unidadEscena.resistencia;
            Debug.Log($"[SYNC] Estado de {unidadJuego.nombre} guardado correctamente.");
        }
        else
        {
            Debug.LogWarning($"[SYNC] No se encontró unidad '{unidadEscena.nombre}' en los datos del jugador.");
        }
    }

}
