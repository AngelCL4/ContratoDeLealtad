using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;
using System.Linq;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuUI;
    [SerializeField] private Button[] botones;
    [SerializeField] private MovementRangeVisualizer visualizadorMovimiento;
    [SerializeField] private ObjectPanelUI objetoPanelUI; // referencia en el inspector
    [SerializeField] private IntercambioPanelUI intercambioPanelUI;
    [SerializeField] private ConversationManager conversationManager;
    [SerializeField] public ObjectiveMenu objectiveMenu;
    private Button[] todosLosBotones; // Para mantener referencia fija
    public bool MenuActivo => gameObject.activeSelf;
    private int indexActual = 0;
    private Color colorSeleccionado = Color.white;
    private Color colorNormal = new Color(0.7f, 0.7f, 0.7f, 1f);

    private enum Modo { Menu, IntercambioSeleccion, IntercambioPanel, CuracionSeleccion, CuracionPanel, AtaqueSeleccion, AtaquePanel }
    private Modo modoActual = Modo.Menu;
    private List<UnitLoader> aliadosAdyacentes;
    [SerializeField] private CurarPanelUI curarPanelUI; // asignar en Inspector
    private List<UnitLoader> aliadosHeridos;
    private int indiceSeleccionado = 0;
    public bool fightConversationFinished = true;

    private Vector3 posicionAnterior;
    private PointerController pointer;
    private UnitLoader unidad;
    [SerializeField] private AtacarPanelUI atacarPanelUI; // Asignar en inspector
    private List<UnitLoader> enemigosEnRango;
    private UnitLoader enemigoSeleccionado;
    private int experienciaPorCuracion = 15;
    public static ActionMenu Instancia { get; private set; }

    private void Awake()
    {
        todosLosBotones = botones; // Guardamos todos los botones originales
        Instancia = this;
    }

    public void AbrirMenu(UnitLoader unidadActual, Vector3 posAnterior, PointerController controller)
    {
        unidad = unidadActual;
        posicionAnterior = posAnterior;
        pointer = controller;
        pointer.enabled = false;
        pointer.HabilitarInfoUnidad(true);
        gameObject.SetActive(true);
        menuUI.SetActive(true);
        modoActual = Modo.Menu;
        indexActual = 0;
        ActualizarSeleccionVisual();
        ActualizarOpciones();
    }

    public void CerrarMenu()
    {
        if (pointer != null)
            pointer.enabled = true;
            pointer.HabilitarInfoUnidad(true);
        gameObject.SetActive(false);
        pointer.MoverA(unidad.transform.position);
        pointer.unidadSeleccionada = null;
        visualizadorMovimiento.LimpiarCasillasAtaque();
    }

    private void Update()
    {
        if (!TurnManager.Instancia.conversationFinished || !fightConversationFinished) return;
        if (modoActual == Modo.IntercambioSeleccion)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                // Cancelar intercambio
                modoActual = Modo.Menu;
                pointer.enabled = true;
                pointer.HabilitarInfoUnidad(true);
                AbrirMenu(unidad, posicionAnterior, pointer);
            }

            pointer.HandleMovement();

            if (Input.GetKeyDown(KeyCode.A))
            {
                Vector3Int posPuntero = FindObjectOfType<Tilemap>().WorldToCell(pointer.transform.position);

                // Evitar seleccionar la misma unidad
                Vector3Int posUnidad = FindObjectOfType<Tilemap>().WorldToCell(unidad.transform.position);
                if (posPuntero == posUnidad)
                {
                    Debug.Log("No puedes seleccionar a ti mismo.");
                    return;
                }

                // Verificar si hay un aliado adyacente en esa posición
                foreach (var aliado in aliadosAdyacentes)
                {
                    Vector3Int posAliado = FindObjectOfType<Tilemap>().WorldToCell(aliado.transform.position);
                    if (posAliado == posPuntero)
                    {
                        indiceSeleccionado = aliadosAdyacentes.IndexOf(aliado);
                        MostrarPanelIntercambio();
                        return;
                    }
                }

                Debug.Log("Esa unidad no es un aliado adyacente válido.");
            }

            return; // Evita que el resto de Update corra
        }

        if (modoActual == Modo.IntercambioPanel)
        {
            return;
        }

        if (modoActual == Modo.CuracionSeleccion)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                modoActual = Modo.Menu;
                pointer.enabled = true;
                pointer.HabilitarInfoUnidad(true);
                curarPanelUI.Ocultar();
                AbrirMenu(unidad, posicionAnterior, pointer);
                return;
            }

            pointer.HandleMovement();

            Vector3Int posPuntero = FindObjectOfType<Tilemap>().WorldToCell(pointer.transform.position);
            foreach (var herido in aliadosHeridos)
            {
                Vector3Int posAliado = FindObjectOfType<Tilemap>().WorldToCell(herido.transform.position);
                if (posAliado == posPuntero)
                {
                    MostrarPanelCurar(herido);
                }
                else curarPanelUI.Ocultar();
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                foreach (var herido in aliadosHeridos)
                {
                    Vector3Int posAliado = FindObjectOfType<Tilemap>().WorldToCell(herido.transform.position);
                    if (posAliado == posPuntero)
                    {
                        RealizarCuracion(herido);
                        return;
                    }
                }

                Debug.Log("No es un aliado herido válido.");
            }

            return;
        }

        if (modoActual == Modo.AtaqueSeleccion)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                unidad.barraVida.LimpiarDanioProyectado();
                foreach (var enemigo in enemigosEnRango)
                {
                    enemigo.barraVida.LimpiarDanioProyectado();
                }
                modoActual = Modo.Menu;
                pointer.enabled = true;
                pointer.HabilitarInfoUnidad(true);
                atacarPanelUI.Ocultar();
                AbrirMenu(unidad, posicionAnterior, pointer);
                return;
            }

            pointer.HandleMovement();

            Vector3Int posPuntero = FindObjectOfType<Tilemap>().WorldToCell(pointer.transform.position);

            foreach (var enemigo in enemigosEnRango)
            {
                enemigo.barraVida.LimpiarDanioProyectado(); // 🧼 Limpieza al cambiar selección
            }
            unidad.barraVida.LimpiarDanioProyectado(); // Por si hay contraataque mostrado previo

            foreach (var enemigo in enemigosEnRango)
            {
                Vector3Int posEnemigo = FindObjectOfType<Tilemap>().WorldToCell(enemigo.transform.position);
                if (posPuntero == posEnemigo)
                {
                    MostrarPanelAtaque(enemigo);
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("Tecla A pulsada");
                foreach (var enemigo in enemigosEnRango)
                {
                    Vector3Int posEnemigo = FindObjectOfType<Tilemap>().WorldToCell(enemigo.transform.position);
                    Debug.Log(posPuntero);
                    Debug.Log(posEnemigo);
                    if (posPuntero == posEnemigo)
                    {
                        enemigoSeleccionado = enemigo;
                        modoActual = Modo.AtaquePanel;

                        EjecutarCombate(unidad, enemigoSeleccionado);
                    }
                }

                Debug.Log("No es un enemigo válido.");
            }

            return;
        }

        if (modoActual == Modo.Menu)
        {
            // Navegación entre botones
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                indexActual = (indexActual - 1 + botones.Length) % botones.Length;
                ActualizarSeleccionVisual();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                indexActual = (indexActual + 1) % botones.Length;
                ActualizarSeleccionVisual();
            }

            // Seleccionar opción actual
            if (Input.GetKeyDown(KeyCode.A))
            {
                EjecutarOpcion(indexActual);
            }

            // Cancelar acción
            if (Input.GetKeyDown(KeyCode.S))
            {
                unidad.transform.position = posicionAnterior;
                pointer.CancelarAccion();
                CerrarMenu();
            }
        }
    }

    public void EjecutarCombate(UnitLoader unidad, UnitLoader enemigoSeleccionado)
    {
        StartCoroutine(AtacarYPostProceso(unidad, enemigoSeleccionado));
    }

    private IEnumerator AtacarYPostProceso(UnitLoader unidad, UnitLoader enemigoSeleccionado)
    {
        // Llamamos a la corutina de combate
        yield return StartCoroutine(RealizarCombate(unidad, enemigoSeleccionado));

        // 7. Marcar como usada y cerrar todo
        if (unidad != null)
        {
            unidad.MarcarComoUsada();
            TurnManager.Instancia.StartCoroutine(TurnManager.Instancia.NotificarUnidadTerminada(unidad));
            if (pointer.mostrarRangosEnemigos)
            {
                pointer.visualizadorMovimiento.LimpiarRangosDeEnemigos();
                pointer.MostrarRangosAtaqueEnemigos();
            }
            pointer.ActualizarRangoEnemigo();
            pointer.unidadSeleccionada = null;
            pointer.MoverA(unidad.transform.position);
        }
        pointer.enabled = true;
        atacarPanelUI.Ocultar();
        CerrarMenu();
    }

    private IEnumerator RealizarCombate(UnitLoader atacante, UnitLoader atacado)
    {
        atacante.barraVida.LimpiarDanioProyectado();
        atacado.barraVida.LimpiarDanioProyectado();
        Debug.Log(atacado.datos.estado);
        if (atacado.datos.estado == "Jefe")
        {
            if (GameManager.Instance.bossMusicSounding == false)
            {
                var chapterData = GameManager.Instance.chapterDataJuego.chapters.FirstOrDefault(c => c.chapterName == ChapterManager.instance.currentChapter);
                if (!string.IsNullOrEmpty(chapterData.bossMusic))
                {
                    MusicManager.Instance.PlayMusic(chapterData.bossMusic);
                    GameManager.Instance.bossMusicSounding = true;
                }
            }
            fightConversationFinished = false;
            string chapterNumber = ChapterManager.instance.currentChapter;
            ConversationManager.Instancia.StartFightConversation(atacante.datos.nombre, chapterNumber, () => fightConversationFinished = true);
            yield return new WaitUntil(() => fightConversationFinished);
        }

        int distancia = CalcularDistancia(atacante.transform.position, atacado.transform.position);
        bool enemigoPuedeContraatacar = distancia >= atacado.datos.clase.rangoAtaqueMinimo && distancia <= atacado.datos.clase.rangoAtaqueMaximo;

        // --- Datos del atacante (unidad) ---
        int poderAtacante = atacante.datos.poder;
        string tipoDanoAtacante = atacante.datos.clase.tipoDano;
        int defensaObjetivo = tipoDanoAtacante == "Fisico" ? atacado.datos.defensa : atacado.datos.resistencia;
        int dañoAtacante = Mathf.Max(0, poderAtacante - defensaObjetivo);

        bool esCriticoAtacante = Random.Range(1, 100) <= Mathf.Max(0, atacante.datos.habilidad - atacado.datos.suerte);
        int velocidadAtacante = atacante.datos.velocidad;

        // --- Datos del defensor (enemigo) ---
        int poderDefensor = atacado.datos.poder;
        string tipoDanoDefensor = atacado.datos.clase.tipoDano;
        int defensaDelAtacante = tipoDanoDefensor == "Fisico" ? atacante.datos.defensa : atacante.datos.resistencia;
        int dañoDefensor = Mathf.Max(0, poderDefensor - defensaDelAtacante);

        bool esCriticoDefensor = Random.Range(0, 100) < Mathf.Max(0, atacado.datos.habilidad - atacante.datos.suerte);
        int velocidadDefensor = atacado.datos.velocidad;

        bool dobleAtacante = velocidadAtacante > velocidadDefensor;
        bool dobleDefensor = velocidadDefensor > velocidadAtacante;

        // --- Combate --- 
        Debug.Log("▶ COMBATE INICIADO");

        // 1. Atacante ataca
        int dañoTotalAtacante = esCriticoAtacante ? dañoAtacante * 2 : dañoAtacante;
        atacado.datos.PV -= dañoTotalAtacante;
        Debug.Log($"🗡️ {atacante.datos.nombre} ataca a {atacado.datos.nombre} por {dañoTotalAtacante} de daño" + (esCriticoAtacante ? " (CRÍTICO!)" : ""));
        atacado.barraVida.ActualizarPV();
        // Esperar 1 segundo
        yield return new WaitForSeconds(1f);

        // 2. Verificar si el enemigo sobrevivió para contraatacar
        if (atacado.datos.PV > 0 && enemigoPuedeContraatacar)
        {
            int dañoTotalDefensor = esCriticoDefensor ? dañoDefensor * 2 : dañoDefensor;
            atacante.datos.PV -= dañoTotalDefensor;
            Debug.Log($"🛡️ {atacado.datos.nombre} contraataca a {atacante.datos.nombre} por {dañoTotalDefensor} de daño" + (esCriticoDefensor ? " (CRÍTICO!)" : ""));
            atacante.barraVida.ActualizarPV();
            // Esperar 1 segundo
            yield return new WaitForSeconds(1f);
        }

        // 3. Doble ataque del atacante
        if (dobleAtacante && atacado.datos.PV > 0 && atacante.datos.PV > 0)
        {
            int dañoExtra = Mathf.Max(0, poderAtacante - defensaObjetivo);
            atacado.datos.PV -= dañoExtra;
            Debug.Log($"🔁 {atacante.datos.nombre} realiza un segundo ataque por {dañoExtra} de daño");
            atacado.barraVida.ActualizarPV();
            // Esperar 1 segundo
            yield return new WaitForSeconds(1f);
        }

        // 4. Doble ataque del defensor
        if (dobleDefensor && atacante.datos.PV > 0 && atacado.datos.PV > 0 && enemigoPuedeContraatacar)
        {
            int dañoExtra = Mathf.Max(0, poderDefensor - defensaDelAtacante);
            atacante.datos.PV -= dañoExtra;
            Debug.Log($"🔁 {atacado.datos.nombre} realiza un segundo contraataque por {dañoExtra} de daño");
            atacante.barraVida.ActualizarPV();
            // Esperar 1 segundo
            yield return new WaitForSeconds(1f);
        }

        // 5. Clampeamos la vida para no ir debajo de cero
        atacante.datos.PV = Mathf.Max(0, atacante.datos.PV);
        atacado.datos.PV = Mathf.Max(0, atacado.datos.PV);

        if (atacado.datos.PV == 0)
        {
            if (atacado.datos == pointer.enemigoSeleccionado){
                visualizadorMovimiento.LimpiarRangoDeEnemigo(pointer.enemigoSeleccionado);
            }
            yield return StartCoroutine(EsperarRetiradaYFinalizar(atacado.datos.nombre));
            TurnManager.Instancia.unidadesEnemigas.Remove(atacado);
            Destroy(atacado.gameObject);
            if (objectiveMenu.data.victoryCondition == "Comandante")
            {
                yield return null;
                ComprobarJefe();
            }
            if (objectiveMenu.data.victoryCondition == "Derrotar")
            {
                yield return null;
                ComprobarVictoria();
            }
        }

        if (atacante.datos.PV == 0)
        {
            yield return StartCoroutine(EsperarRetiradaYFinalizar(atacante.datos.nombre));
            atacante.LimpiarBonosTerreno();
            atacante.LimpiarPotenciadores();
            atacante.LimpiarArtefactosPasivos();
            atacante.LimpiarEfectosEntrantes();
            atacante.datos.PV = atacante.datos.MaxPV;
            TurnManager.Instancia.unidadesAGuardar.Add(atacante.datos);
            TurnManager.Instancia.unidadesJugador.Remove(atacante);
            Destroy(atacante.gameObject);
            TurnManager.Instancia.ComprobarDerrota();  
            yield break;
        }

        if (atacante != null)
        {
            // Solo ejecutar si el objeto sigue vivo
            int leveldiff = atacado.datos.nivel - atacante.datos.nivel;
            if (leveldiff < 0) leveldiff = 0;
            int experienciaGanada = (atacado.datos.PV == 0) ? 30 + (5 * leveldiff) : 15 + (5 * leveldiff);
            atacante.GanarExp(atacante.datos, experienciaGanada);

            foreach (var aliada in atacante.UnidadesAliadasAdyacentes())
            {
                SupportManager.Instance.AñadirAfecto(atacante.datos.nombre, aliada.datos.nombre, 2);
            }
        }

        yield return null;
    }

    public IEnumerator EsperarRetiradaYFinalizar(string nombre)
    {
        bool finalizado = false;
        conversationManager.StartRetreatQuote(nombre, () => finalizado = true);

        // Esperar a que finalice el diálogo de retirada
        yield return new WaitUntil(() => finalizado);

        var chapterData = GameManager.Instance.chapterDataJuego.chapters.FirstOrDefault(c => c.chapterName == ChapterManager.instance.currentChapter);
        if (GameManager.Instance.bossMusicSounding == false)
        {
            if (!string.IsNullOrEmpty(chapterData.battleMusic))
            {
                MusicManager.Instance.PlayMusic(chapterData.battleMusic);
            }
        }
        else 
        {
            if (!string.IsNullOrEmpty(chapterData.bossMusic))
            {
                MusicManager.Instance.PlayMusic(chapterData.bossMusic);
            }
        }
    }

    private void MoverPuntero(Vector3Int direccion)
    {
        pointer.MovePointer(direccion);
    }

    private void ActualizarSeleccionVisual()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            TextMeshProUGUI texto = botones[i].GetComponentInChildren<TextMeshProUGUI>();
            texto.color = (i == indexActual) ? colorSeleccionado : colorNormal;
        }
    }

    private void EjecutarOpcion(int indice)
    {
        string nombre = botones[indice].name;

        switch (nombre)
        {
            case "Atacar":
                OnAtacar();
                break;
            case "Curar":
                OnCurar();
                break;
            case "Objeto":
                OnObjeto();
                break;
            case "Intercambiar":
                OnIntercambiar();
                break;
            case "Esperar":
                StartCoroutine(OnEsperar());
                break;
            default:
                Debug.LogWarning("Botón sin acción definida: " + nombre);
                break;
        }
    }

    public void OnAtacar()
    {
        Debug.Log("Acción: Atacar");
        enemigosEnRango = unidad.EnemigosEnRango();

        if (enemigosEnRango.Count == 0)
        {
            Debug.Log("No hay enemigos en rango.");
            return;
        }

        modoActual = Modo.AtaqueSeleccion;
        pointer.enabled = true;
        pointer.HabilitarInfoUnidad(false);
        menuUI.SetActive(false);
    }

    int CalcularDistancia(Vector3 pos1, Vector3 pos2)
    {
        Vector3Int cell1 = FindObjectOfType<Tilemap>().WorldToCell(pos1);
        Vector3Int cell2 = FindObjectOfType<Tilemap>().WorldToCell(pos2);
        return Mathf.Abs(cell1.x - cell2.x) + Mathf.Abs(cell1.y - cell2.y);
    }

    private void MostrarPanelAtaque(UnitLoader enemigo)
    {
        int distancia = CalcularDistancia(unidad.transform.position, enemigo.transform.position);
        bool enemigoPuedeContraatacar = distancia >= enemigo.datos.clase.rangoAtaqueMinimo && distancia <= enemigo.datos.clase.rangoAtaqueMaximo;

        // --- Atacante ---
        int poderAtacante = unidad.datos.poder;
        string tipoDanoAtacante = unidad.datos.clase.tipoDano;
        int defensaObjetivo = tipoDanoAtacante == "Fisico" ? enemigo.datos.defensa : enemigo.datos.resistencia;
        int dañoAtacante = Mathf.Max(0, poderAtacante - defensaObjetivo);

        int criticoAtacante = Mathf.Max(0, unidad.datos.habilidad - enemigo.datos.suerte);
        int velocidadAtacante = unidad.datos.velocidad;

        // --- Defensor (contraataque) ---
        int poderDefensor = enemigo.datos.poder;
        string tipoDanoDefensor = enemigo.datos.clase.tipoDano;
        int defensaDelAtacante = tipoDanoDefensor == "Fisico" ? unidad.datos.defensa : unidad.datos.resistencia;
        int dañoDefensor = Mathf.Max(0, poderDefensor - defensaDelAtacante);

        int criticoDefensor = Mathf.Max(0, enemigo.datos.habilidad - unidad.datos.suerte);
        int velocidadDefensor = enemigo.datos.velocidad;

        bool dobleAtacante = velocidadAtacante > velocidadDefensor;
        bool dobleDefensor = velocidadDefensor > velocidadAtacante;

        atacarPanelUI.Mostrar(
            unidad.datos.nombre, unidad.datos.PV, dañoAtacante, criticoAtacante, dobleAtacante,
            enemigo.datos.nombre, enemigo.datos.PV, dañoDefensor, criticoDefensor, dobleDefensor, enemigoPuedeContraatacar
        );

        int dañoTotalAlEnemigo = dobleAtacante ? dañoAtacante * 2 : dañoAtacante;
        enemigo.barraVida.MostrarDanioProyectado(dañoTotalAlEnemigo);

        if (enemigoPuedeContraatacar)
        {
            int dañoTotalAlJugador = dobleDefensor ? dañoDefensor * 2 : dañoDefensor;
            unidad.barraVida.MostrarDanioProyectado(dañoTotalAlJugador);
        }
    }

    public void OnCurar()
    {
        Debug.Log("Acción: Curar");
        modoActual = Modo.CuracionSeleccion;

        aliadosHeridos = unidad.UnidadesAliadasAdyacentesHeridas();
        if (aliadosHeridos.Count == 0)
        {
            Debug.Log("No hay aliados heridos adyacentes.");
            return;
        }

        indexActual = 0;
        pointer.enabled = true;
        pointer.HabilitarInfoUnidad(false);
        menuUI.SetActive(false);
    }

    private void MostrarPanelCurar(UnitLoader aliadoHerido)
    {
        curarPanelUI.Mostrar(aliadoHerido);
    }

    private void RealizarCuracion(UnitLoader objetivo)
    {
        int poder = unidad.datos.poder; // Asumiendo que el "poder" está en los datos
        int nuevaVida = Mathf.Min(objetivo.datos.PV + poder, objetivo.datos.MaxPV);
        int cantidadCurada = nuevaVida - objetivo.datos.PV;

        objetivo.datos.PV = nuevaVida;

        Debug.Log($"Curaste a {objetivo.datos.nombre} por {cantidadCurada} puntos de vida.");
        objetivo.barraVida.ActualizarPV();
        unidad.GanarExp(unidad.datos, experienciaPorCuracion);
        SupportManager.Instance.AñadirAfecto(unidad.datos.nombre, objetivo.datos.nombre, 2);
        unidad.MarcarComoUsada();
        TurnManager.Instancia.StartCoroutine(TurnManager.Instancia.NotificarUnidadTerminada(unidad));
        if (pointer.mostrarRangosEnemigos)
        {
            pointer.visualizadorMovimiento.LimpiarRangosDeEnemigos();
            pointer.MostrarRangosAtaqueEnemigos();
        }
        pointer.ActualizarRangoEnemigo();
        pointer.MoverA(unidad.transform.position);
        pointer.unidadSeleccionada = null;
        pointer.enabled = true;
        curarPanelUI.Ocultar();
        CerrarMenu();
    }

    public void OnObjeto()
    {
        Debug.Log("Abriendo panel de objeto");

        // Supongamos que solo hay un objeto
        Objeto objeto = unidad.datos.objeto;

        bool objetoFueUsado = false; // 🧠 bandera de control

        objetoPanelUI.Mostrar(unidad, objeto, () =>
        {
            // Se ejecuta al cerrar el panel de objeto
            if (!objetoFueUsado)
            {
                gameObject.SetActive(true); // ✅ solo reabrimos si NO se usó
            }
        },
        (objetoUsado) =>
        {
            // Se ejecuta al usar el objeto
            Debug.Log($"Usando objeto: {objetoUsado.nombre}");
            unidad.MarcarComoUsada();
            TurnManager.Instancia.StartCoroutine(TurnManager.Instancia.NotificarUnidadTerminada(unidad));
            if (pointer.mostrarRangosEnemigos)
            {
                pointer.visualizadorMovimiento.LimpiarRangosDeEnemigos();
                pointer.MostrarRangosAtaqueEnemigos();
            }
            pointer.ActualizarRangoEnemigo();
            pointer.unidadSeleccionada = null;

            objetoFueUsado = true; // marcamos como usado
            CerrarMenu(); // ✅ cerramos correctamente
        });

        gameObject.SetActive(false); // ocultamos mientras se muestra el panel
    }

    public void OnIntercambiar()
    {
        Debug.Log("Acción: Intercambiar");
        modoActual = Modo.IntercambioSeleccion;
        if (unidad.TieneObjetos()){
            aliadosAdyacentes = unidad.UnidadesAliadasAdyacentes();
        }
        else {
            aliadosAdyacentes = unidad.UnidadesAliadasAdyacentesConObjeto();
        }
        if (aliadosAdyacentes.Count == 0)
        {
            Debug.Log("No hay aliados para intercambiar.");
            return;
        }
        indexActual = 0;
        pointer.enabled = true;
        pointer.HabilitarInfoUnidad(false);
        menuUI.SetActive(false);
    }

    private void MostrarPanelIntercambio()
    {
        modoActual = Modo.IntercambioPanel;
        pointer.enabled = false;

        Objeto obj1 = unidad.datos.objeto;
        Objeto obj2 = aliadosAdyacentes[indiceSeleccionado].datos.objeto;

        intercambioPanelUI.Mostrar(obj1, obj2, 
            () => // callback cancelar
            {
                modoActual = Modo.IntercambioSeleccion;
            },
            () => // callback confirmar intercambio
            {
                unidad.datos.objeto = obj2;
                aliadosAdyacentes[indiceSeleccionado].datos.objeto = obj1;

                unidad.MarcarComoUsada();
                TurnManager.Instancia.StartCoroutine(TurnManager.Instancia.NotificarUnidadTerminada(unidad));
                if (pointer.mostrarRangosEnemigos)
                {
                    pointer.visualizadorMovimiento.LimpiarRangosDeEnemigos();
                    pointer.MostrarRangosAtaqueEnemigos();
                }
                pointer.ActualizarRangoEnemigo();
                pointer.MoverA(unidad.transform.position);
                pointer.unidadSeleccionada = null;
                pointer.enabled = true;
                intercambioPanelUI.Cerrar();
                CerrarMenu();
            });
    }

    public IEnumerator OnEsperar()
    {
        Debug.Log("Acción: Esperar / Confirmar movimiento");
        yield return StartCoroutine(ComprobarVisita());
        unidad.MarcarComoUsada();
        TurnManager.Instancia.StartCoroutine(TurnManager.Instancia.NotificarUnidadTerminada(unidad));
        if (pointer.mostrarRangosEnemigos)
        {
            pointer.visualizadorMovimiento.LimpiarRangosDeEnemigos();
            pointer.MostrarRangosAtaqueEnemigos();
        }
        pointer.ActualizarRangoEnemigo();
        pointer.unidadSeleccionada = null;
        CerrarMenu();

        if(objectiveMenu.data.victoryCondition == "Escapar")
        {
            if (unidad.transform.position.x == objectiveMenu.data.x && unidad.transform.position.y == objectiveMenu.data.y)
            {
                unidad.LimpiarBonosTerreno();
                unidad.LimpiarPotenciadores();
                unidad.LimpiarArtefactosPasivos();
                unidad.LimpiarEfectosEntrantes();
                unidad.datos.PV = unidad.datos.MaxPV;
                TurnManager.Instancia.unidadesAGuardar.Add(unidad.datos);
                TurnManager.Instancia.unidadesJugador.Remove(unidad);
                ComprobarEscape();
                Destroy(unidad.gameObject);              
            }
        }
    }

    public IEnumerator ComprobarVisita()
    {
        Debug.Log(ConversationManager.Instancia == null);
        if (ConversationManager.Instancia == null) yield break;

        float x = unidad.transform.position.x; // O donde guardes la posición X de la unidad
        float y = unidad.transform.position.y; // Igual para Y
        Debug.Log(x);
        Debug.Log(y);

        VisitData visita = ConversationManager.Instancia.BuscarVisitaEnCoordenada(x, y);

        if (visita != null && !visita.visited)
        {
            Debug.Log($"¡Visitando edificio en ({x},{y})!");

            conversationManager.VisitConversationActive = true;

            // Iniciar la conversación asociada
            conversationManager.StartConversation(visita.conversation, () =>
            {
                Debug.Log($"Conversación {visita.conversation} terminada.");

                // Dar la recompensa
                DarRecompensa(visita.reward);

                // Marcar como visitada
                visita.visited = true;
                conversationManager.VisitConversationActive = false;
            });

            yield return new WaitUntil(() => !conversationManager.VisitConversationActive);
        }
    }

    private void DarRecompensa(Objeto recompensa)
    {
        if (string.IsNullOrEmpty(recompensa.nombre)) return;

        if (recompensa.nombre == "Contrato")
        {
            GameManager.Instance.chapterDataJuego.contratos++;
        }
        else
        {
            if (unidad.TieneObjetos())
            {
                AlmacenObjetos.Instance.AñadirObjeto(recompensa);
            }
            else 
            {
                unidad.datos.objeto = recompensa;
                unidad.datos.objeto.icono = Resources.Load<Sprite>(recompensa.spritePath);
            }
        }
        Debug.Log(GameManager.Instance.contractCount);
    }

    private void ActualizarOpciones()
    {
        // Ejemplo: ocultar el botón de atacar si no hay enemigos
        bool puedeAtacar = unidad.EnemigosEnRango().Count > 0;
        bool tieneObjeto = unidad.TieneObjetos();
        bool puedeIntercambiar = unidad.UnidadesAliadasAdyacentesConObjeto().Count > 0;
        bool puedeIntercambiarObjeto = unidad.TieneObjetos() && unidad.UnidadesAliadasAdyacentes().Count > 0;
        bool puedeCurar = unidad.UnidadesAliadasAdyacentesHeridas().Count > 0;

        foreach (var boton in todosLosBotones)
        {
            switch (boton.name)
            {
                case "Atacar":
                    boton.gameObject.SetActive(puedeAtacar);
                    break;
                case "Curar":
                    boton.gameObject.SetActive(puedeCurar);
                    break;
                case "Objeto":
                    boton.gameObject.SetActive(tieneObjeto);
                    break;
                case "Intercambiar":
                    bool mostrarBoton = puedeIntercambiar || puedeIntercambiarObjeto;
                    boton.gameObject.SetActive(mostrarBoton);
                    break;
                case "Esperar":
                    boton.gameObject.SetActive(true);
                    break;
            }
        }

        
        var botonesVisibles = new List<Button>();
        foreach (var boton in todosLosBotones)
        {
            if (boton.gameObject.activeSelf)
                botonesVisibles.Add(boton);
        }
        botones = botonesVisibles.ToArray();

        indexActual = 0;
        ActualizarSeleccionVisual();
    }

    
    public void ComprobarVictoria()
    {

        // Buscar todas las unidades en la escena
        UnitLoader[] unidades = FindObjectsOfType<UnitLoader>();

        // Comprobar si queda algún enemigo o jefe
        bool quedanEnemigos = unidades.Any(unit => !unit.esAliado);
        Debug.Log(quedanEnemigos);
        if (!quedanEnemigos)
        {
            ChapterManager.instance.chapterCompleted = true;
            Debug.Log("✅ ¡Capítulo completado! No quedan enemigos.");
        }
    }

    public void ComprobarJefe()
    {
        // Buscar todas las unidades en la escena
        UnitLoader[] unidades = FindObjectsOfType<UnitLoader>();

        foreach (var unidad in unidades)
        {
            Debug.Log($"Unidad encontrada: {unidad.datos.nombre}, Estado: {unidad.datos.estado}, esAliado: {unidad.esAliado}");
        }

        // Comprobar si queda algún "Jefe"
        bool quedaJefe = unidades.Any(unit => unit.datos.estado == "Jefe");

        if (!quedaJefe)
        {
            // Si no queda ningún "Jefe", completar el capítulo
            ChapterManager.instance.chapterCompleted = true;
            Debug.Log("✅ ¡Capítulo completado! No queda ningún Jefe.");
        }
    }

    private void ComprobarEscape()
    {
        bool quedanAliados = TurnManager.Instancia.unidadesJugador.Count != 0;
        Debug.Log("Quedan aliados: " + quedanAliados);
        if (!quedanAliados)
        {
            ChapterManager.instance.chapterCompleted = true;
            Debug.Log("✅ ¡Capítulo completado! No quedan aliados por escapar.");
        }
    }
}
