using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public enum Fase
{
    Jugador,
    Enemigo
}

public class TurnManager : MonoBehaviour
{
    [SerializeField] private CombatMenu combatMenu;
    [SerializeField] private ObjectiveMenu objectiveMenu;
    [SerializeField] private ConversationManager conversationManager;
    public static TurnManager Instancia { get; private set; }
    private bool cambiandoFase = false;

    public int TurnoActual { get; private set; } = 0;
    public Fase FaseActual { get; private set; } = Fase.Jugador;

    // Eventos públicos que otras clases pueden suscribirse (UI, controladores, etc.)
    public static event Action<Fase> OnFaseCambiada;
    public static event Action<int> OnTurnoCambiado;

    public List<UnitLoader> unidadesJugador = new List<UnitLoader>();
    public List<UnitLoader> unidadesEnemigas = new List<UnitLoader>();
    public List<UnitLoader> unidadesAEliminar = new List<UnitLoader>();
    public List<Unidad> unidadesAGuardar = new List<Unidad>();
    public GameObject panelDerrota;

    private bool gameOverActivado = false;

    private bool finTurnoAutomatico;
    public bool conversationFinished = true;

    private void Awake()
    {
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Puedes encontrar y registrar las unidades iniciales aquí, o hacerlo dinámicamente
        unidadesJugador.AddRange(FindObjectsOfType<UnitLoader>());
        unidadesEnemigas.AddRange(FindObjectsOfType<UnitLoader>());

        string modo = PlayerPrefs.GetString("FinalTurno", "Automatico"); // por defecto Automático
        finTurnoAutomatico = modo == "Automatico";
    }

    private void Update()
    {
        if (gameOverActivado && Input.anyKeyDown)
        {
            VolverAlMenuPrincipal();
        }
    }

    public void ComprobarDerrota()
    {
        if (unidadesJugador.Count == 0 && !gameOverActivado)
        {
            ActivarGameOver();
        }
    }

    private void ActivarGameOver()
    {
        gameOverActivado = true;
        unidadesAGuardar.Clear();
        panelDerrota.SetActive(true);
        Time.timeScale = 0f; // Pausar el juego si quieres
    }

    private void VolverAlMenuPrincipal()
    {
        Time.timeScale = 1f; // Restaurar el tiempo antes de cambiar de escena
        SceneLoader.Instance.LoadScene("MainMenuScene");
    }

    public void ActualizarModoFinTurno()
    {
        string modo = PlayerPrefs.GetString("FinalTurno", "Automatico");
        finTurnoAutomatico = modo == "Automatico";
        Debug.Log($"Modo de finalización actualizado: {(finTurnoAutomatico ? "Automático" : "Confirmar")}");
    }

    public void RegistrarUnidadAliada(UnitLoader unidad)
    {
        if (!unidadesJugador.Contains(unidad))
            unidadesJugador.Add(unidad);
    }

    public void RegistrarUnidadEnemiga(UnitLoader unidad)
    {
        if (!unidadesEnemigas.Contains(unidad))
            unidadesEnemigas.Add(unidad);
    }

    public IEnumerator NotificarUnidadTerminada(UnitLoader unidad)
    {
        if (TodasLasUnidadesAliadasHanActuado() && finTurnoAutomatico && !cambiandoFase)
        {
            Debug.Log(ConversationManager.Instancia.VisitConversationActive);
            if (ConversationManager.Instancia.VisitConversationActive)
            {
                // Si hay conversación de visita activa, esperamos a que termine
                yield return new WaitUntil(() => !ConversationManager.Instancia.VisitConversationActive);
            }
            CambiarAFaseEnemiga();
        }
    }

    private bool TodasLasUnidadesAliadasHanActuado()
    {
        foreach (var unidad in unidadesJugador)
        {
            if (!unidad.yaActuo) return false;
        }
        return true;
    }

    public void PasarFaseJugador()
    {
        if (FaseActual == Fase.Jugador)
        {
            foreach (var unidad in unidadesJugador)
            {
                unidad.MarcarComoUsada();
            }
            CambiarAFaseEnemiga();
        }
    }

    public IEnumerator IniciarFaseJugador()
    {
        FaseActual = Fase.Jugador;
        
        TurnoActual++;
        combatMenu.ActualizarTextoTurnos();

        UnitSpawner.Instancia.SpawnearRefuerzos(TurnoActual);

        conversationFinished = false;
        Debug.Log($"Empezando evento de turno: {TurnoActual}");
        conversationManager.StartEventConversation(TurnoActual, () => conversationFinished = true);
        yield return new WaitUntil(() => conversationFinished);
        Debug.Log($"Evento terminado de turno: {TurnoActual}");
        foreach (var unidad in unidadesJugador)
        {
            unidad.ResetearUso(); // método en UnidadAliada que reactiva la unidad
            unidad.ActualizarBuffsTemporales();
        }

        OnTurnoCambiado?.Invoke(TurnoActual);
        OnFaseCambiada?.Invoke(FaseActual);

        Debug.Log($"Turno {TurnoActual}: Comienza la fase del jugador.");

        if(objectiveMenu.data.victoryCondition == "Sobrevivir")
        {
            ComprobarSobrevivir();
        }
        objectiveMenu.ActualizarTextoLimiteTurnos();
    }

    private void CambiarAFaseEnemiga()
    {
        if (cambiandoFase) return;

        cambiandoFase = true;

        FaseActual = Fase.Enemigo;
        OnFaseCambiada?.Invoke(FaseActual);

        Debug.Log("Comienza la fase del enemigo.");

        // Por ahora la fase del enemigo se simula brevemente y se pasa al siguiente turno
        StartCoroutine(SimularFaseEnemiga());
    }

    private System.Collections.IEnumerator SimularFaseEnemiga()
    {
        // Usamos un bucle for en lugar de foreach para poder controlar el índice y modificar la lista durante el ciclo
        for (int i = 0; i < unidadesEnemigas.Count; i++)
        {
            var enemigo = unidadesEnemigas[i];
            EnemyAI ia = enemigo.GetComponent<EnemyAI>();
            
            if (ia != null)
            {
                yield return StartCoroutine(ia.TomarDecision(enemigo));
            }
            else
            {
                Debug.LogWarning($"EnemyAI no encontrado en {enemigo.name}");
            }

            // Si el enemigo ha muerto durante la toma de decisiones, lo eliminamos inmediatamente
            if (enemigo.datos.PV == 0)
            {
                unidadesEnemigas.RemoveAt(i);
                Destroy(enemigo.gameObject);

                yield return new WaitForEndOfFrame();

                if (ActionMenu.Instancia != null)
                {
                    if (ActionMenu.Instancia.objectiveMenu.data.victoryCondition == "Comandante")
                        ActionMenu.Instancia.ComprobarJefe();

                    if (ActionMenu.Instancia.objectiveMenu.data.victoryCondition == "Derrotar")
                        ActionMenu.Instancia.ComprobarVictoria();
                }

                i--; // Decrementamos el índice para evitar saltarnos al siguiente enemigo
                continue; // Continuamos con el siguiente enemigo en la lista
            }

            if (enemigo != null)
            {
                enemigo.ResetearUso();
            }

            yield return new WaitForSeconds(1f); // Retardo de 1 segundo entre enemigos
        }

        // No necesitamos eliminar enemigos aquí, ya que ahora se eliminan durante el bucle

        Debug.Log("Fase del enemigo terminada.");
        cambiandoFase = false; // 🔄 Resetear el flag
        StartCoroutine(IniciarFaseJugador());
    }

    public void ComprobarSobrevivir()
    {
        // Comprobar si el turno actual es mayor que el turno objetivo en el objetivo de "Sobrevivir"
        if (TurnoActual > objectiveMenu.data.turnos)
        {
            // Si el turno actual es mayor que el objetivo, completar el capítulo
            ChapterManager.instance.chapterCompleted = true;
            Debug.Log("✅ ¡Capítulo completado! Se ha superado el turno objetivo.");
        }
    }
}
