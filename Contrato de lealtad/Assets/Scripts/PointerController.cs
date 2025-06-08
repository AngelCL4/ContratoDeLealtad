using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PointerController : MonoBehaviour
{
    public Tilemap tilemap; // Tilemap del nivel
    public float moveDelay = 0.1f; // Tiempo entre movimientos
    private Vector3Int currentCell; // Posición actual en el Tilemap
    private float lastMoveTime;
    [SerializeField] private UnidadInfoUI unidadInfoUI;
    public UnitLoader unidadSeleccionada = null;
    private bool mostrandoMovimiento = false;
    [SerializeField] public MovementRangeVisualizer visualizadorMovimiento;
    [SerializeField] private TerrainLibrary terrainLibrary;
    [SerializeField] private CombatMenu combatMenu;
    [SerializeField] private TutorialMenu tutorialMenu;
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private ObjectiveMenu objectiveMenu;
    [SerializeField] private TerrenoUI uiTerreno;
    private bool volverAbrirCombatMenu = false;
    [SerializeField] private ActionMenu actionMenu;
    private Vector3 posicionAnteriorUnidad;
    [SerializeField] private Canvas canvas; // arrastrá el Canvas desde el Inspector
    private bool mostrarInfoUnidad = true;
    private HashSet<Unidad> enemigosConRangoMostrado = new HashSet<Unidad>();
    public bool mostrarRangosEnemigos = false;  // Controla si los rangos de los enemigos se muestran
    public Unidad enemigoSeleccionado = null;  // Unidad de enemigo seleccionada

    private void Start()
    {
        Vector3 worldStartPos = transform.position;
        currentCell = tilemap.WorldToCell(worldStartPos);
        uiTerreno.ActualizarInfoTerreno(currentCell);
        MovePointer(Vector3Int.zero); // Asegura que el puntero se alinee a la cuadrícula
        foreach (var unidad in TurnManager.Instancia.unidadesJugador)
        {
            unidad.ActualizarBonosPorTerreno(tilemap, terrainLibrary);
        }
        foreach (var unidad in TurnManager.Instancia.unidadesEnemigas)
        {
            unidad.ActualizarBonosPorTerreno(tilemap, terrainLibrary);
        }
    }

    private void Update()
    {
        if (!TurnManager.Instancia.conversationFinished) return;
        if ((tutorialMenu != null && tutorialMenu.Activo) || (settingsMenu != null && settingsMenu.gameObject.activeSelf) || (objectiveMenu != null && objectiveMenu.gameObject.activeSelf))
        {
            if (combatMenu.MenuActivo)
            {
                combatMenu.CerrarMenu(); // Deberías tener un método público para cerrar el menú de combate
                volverAbrirCombatMenu = true;
            }
            return; // Mientras uno de esos esté activo, no hacemos más lógica de puntero
        }
        else if (volverAbrirCombatMenu)
        {
            combatMenu.AbrirMenu();
            volverAbrirCombatMenu = false;
        }
        if (combatMenu != null && combatMenu.MenuActivo)
            return;

        HandleMovement();
        VerificarUnidadBajoCursor(); // Verificación de la unidad bajo el puntero (si la hay)

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (mostrandoMovimiento && unidadSeleccionada != null)
            {
                // Si ya se está mostrando el movimiento, intentar mover la unidad
                MoverUnidad();
            }
            else if (unidadSeleccionada != null)
            {
                // Ya hay una unidad seleccionada pero no estás en movimiento, así que ignoramos la entrada
                Debug.Log("Ya hay una unidad seleccionada, no se puede seleccionar otra.");
                return;
            }
            else if (actionMenu != null && actionMenu.MenuActivo) // Asegúrate de tener esta propiedad en ActionMenu
            {
                Debug.Log("El menú de acción está abierto. No se puede seleccionar otra unidad.");
                return;
            }
            else
            {
                Vector3Int cell = tilemap.WorldToCell(transform.position);
                Collider2D col = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(cell));

                if ((col == null && !combatMenu.menuActivo) && !combatMenu.finPulsado)
                {
                    combatMenu.AbrirMenu();
                    return;
                }

                IntentarSeleccionarUnidad();
            }

            if (combatMenu.finPulsado)
            {
                combatMenu.finPulsado = false;
            }
        }

        if (mostrandoMovimiento && Input.GetKeyDown(KeyCode.S))
        {
            unidadSeleccionada = null;
            mostrandoMovimiento = false; // Cancelar la acción de movimiento si no se seleccionó unidad aliada
            visualizadorMovimiento.LimpiarCasillas();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (!mostrarRangosEnemigos)
            {
                MostrarRangosAtaqueEnemigos();
                mostrarRangosEnemigos = true;
            }
            else
            {
                visualizadorMovimiento.LimpiarRangosDeEnemigos();
                mostrarRangosEnemigos = false;
            }
        }
    }

    public void HandleMovement()
    {
        if (Time.time - lastMoveTime < moveDelay) return; // Evitar movimientos demasiado rápidos

        Vector3Int moveDirection = Vector3Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow)) moveDirection = Vector3Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) moveDirection = Vector3Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) moveDirection = Vector3Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) moveDirection = Vector3Int.right;

        if (moveDirection != Vector3Int.zero)
        {
            Vector3Int newCell = currentCell + moveDirection;

            // Verificar que no salga de los límites del Tilemap
            if (tilemap.HasTile(newCell))
            {
                MovePointer(moveDirection);
                uiTerreno.ActualizarInfoTerreno(newCell);
            }
        }

        if (mostrarInfoUnidad)
        {
            VerificarUnidadBajoCursor();
        }
    }

    public void MovePointer(Vector3Int direction)
    {
        currentCell += direction;
        transform.position = tilemap.GetCellCenterWorld(currentCell);
        lastMoveTime = Time.time;
    }

    public void IntentarSeleccionarUnidad()
    {
        Vector3Int cell = tilemap.WorldToCell(transform.position);
        Collider2D col = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(cell));

        // Solo proceder si se detecta una unidad bajo el puntero
        if (col != null && col.TryGetComponent(out UnitLoader u))
        {
            // Verificar si la unidad es aliada
            if (u.esAliado && !u.yaActuo)
            {
                Debug.Log("Unidad aliada seleccionada.");

                // Activar el modo de movimiento
                unidadSeleccionada = u;
                mostrandoMovimiento = true;

                // Obtener las casillas ocupadas por otras unidades
                var ocupadas = new List<Vector3Int>();
                foreach (var other in FindObjectsOfType<UnitLoader>())
                {
                    if (other != u) // Evita contar su propia casilla como ocupada
                        ocupadas.Add(tilemap.WorldToCell(other.transform.position));
                }

                // Mostrar las casillas accesibles
                var casillasMovimiento = visualizadorMovimiento.MostrarCasillasAccesibles(cell, u.datos, terrainLibrary, ocupadas, true);
                visualizadorMovimiento.MostrarRangoAtaque(casillasMovimiento, u.datos.clase.rangoAtaqueMinimo, u.datos.clase.rangoAtaqueMaximo, u.datos);
                Debug.Log("Casillas accesibles mostradas.");
            }
            else if (u.yaActuo)
            {
                Debug.Log("Esta unidad ya actuó y no puede volver a seleccionarse.");
            }
            else
            {
                Debug.Log("La unidad no es aliada.");
                SeleccionarEnemigo();
            }
        }
        else
        {
            Debug.Log("No hay unidad bajo el puntero.");
            mostrandoMovimiento = false; // Cancelar la acción de movimiento si no se seleccionó unidad aliada
            visualizadorMovimiento.LimpiarCasillas();
        }
    }

    public void HabilitarInfoUnidad(bool habilitar)
    {
        mostrarInfoUnidad = habilitar;

        if (!habilitar)
            unidadInfoUI.Ocultar(); // Ocultamos al instante si se desactiva
    }

    private void VerificarUnidadBajoCursor()
    {
        if (!mostrarInfoUnidad)
        {
            unidadInfoUI.Ocultar();
            return;
        }

        Vector3Int posGrid = tilemap.WorldToCell(transform.position);
        Vector3 worldPos = tilemap.GetCellCenterWorld(posGrid);
        Collider2D col = Physics2D.OverlapPoint(worldPos);

        if (col != null && col.TryGetComponent(out UnitLoader unidad))
        {
            unidadInfoUI.MostrarDatos(unidad.datos);
        }
        else
        {
            unidadInfoUI.Ocultar();
        }
    }

    private void MoverUnidad()
    {
        Vector3Int cell = tilemap.WorldToCell(transform.position);

        // Verificar si la casilla es accesible
        if (visualizadorMovimiento.EsCasillaAccesible(cell))
        {
            // Guardar la posición anterior
            posicionAnteriorUnidad = unidadSeleccionada.transform.position;
            // Mover la unidad a la nueva casilla
            unidadSeleccionada.transform.position = tilemap.GetCellCenterWorld(cell);
            unidadSeleccionada.ActualizarBonosPorTerreno(tilemap, terrainLibrary);
            // Detener el movimiento y limpiar las casillas
            mostrandoMovimiento = false;
            visualizadorMovimiento.LimpiarCasillas();
            // Abrir el menú de acciones
            PosicionActionMenu(unidadSeleccionada, unidadSeleccionada.transform.position);
            actionMenu.AbrirMenu(unidadSeleccionada, posicionAnteriorUnidad, this);
            Vector3Int celdaActual = tilemap.WorldToCell(unidadSeleccionada.transform.position);
            List<Vector3Int> posicionActual = new List<Vector3Int> { celdaActual };
            visualizadorMovimiento.MostrarRangoAtaque(posicionActual, unidadSeleccionada.datos.clase.rangoAtaqueMinimo, unidadSeleccionada.datos.clase.rangoAtaqueMaximo, unidadSeleccionada.datos);
        }
        else
        {
            // Cancelar selección si la casilla no es accesible
            CancelarAccion();
            visualizadorMovimiento.LimpiarCasillasAtaque();
        }
    }

    public void PosicionActionMenu(UnitLoader unidad, Vector3 mundoPos)
    {
        SpriteRenderer sr = unidad.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("No se encontró SpriteRenderer en la unidad.");
            return;
        }

        Bounds bounds = sr.bounds;
        Vector3 ladoDerechoMundo = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
        Vector3 ladoIzquierdoMundo = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z);

        Vector3 pantallaDerecha = Camera.main.WorldToScreenPoint(ladoDerechoMundo);
        Vector3 pantallaIzquierda = Camera.main.WorldToScreenPoint(ladoIzquierdoMundo);

        float menuWidth = actionMenu.GetComponent<RectTransform>().rect.width;
        float margen = 10f;

        Vector3 nuevaPos;

        // Comprobar si hay espacio a la derecha para colocar el menú
        if (pantallaDerecha.x + menuWidth + margen < Screen.width)
        {
            // Colocar a la derecha del sprite (alineando el borde izquierdo del menú al borde derecho del sprite)
            nuevaPos = pantallaDerecha;
            nuevaPos.x += margen;
            actionMenu.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f); // anclar izquierda del menú
        }
        else
        {
            // Colocar a la izquierda del sprite (alineando el borde derecho del menú al borde izquierdo del sprite)
            nuevaPos = pantallaIzquierda;
            nuevaPos.x -= margen;
            actionMenu.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f); // anclar derecha del menú
        }

        actionMenu.transform.position = nuevaPos;
    }

    public void CancelarAccion()
    {
        unidadSeleccionada = null;
        mostrandoMovimiento = false;
        visualizadorMovimiento.LimpiarCasillas();
    }

    public void MoverA(Vector3 posicion)
    {
        transform.position = posicion;
        currentCell = tilemap.WorldToCell(posicion);
        lastMoveTime = Time.time;
    }

    // Mostrar el rango de ataque de todos los enemigos
    public void MostrarRangosAtaqueEnemigos()
    {

        foreach (var unidad in FindObjectsOfType<UnitLoader>())
        {
            if (unidad != null && (unidad.datos.estado == "Enemigo" || unidad.datos.estado == "Jefe"))  // Solo enemigos
            {
                var ocupadas = new List<Vector3Int>();
                foreach (var other in FindObjectsOfType<UnitLoader>())
                {
                    if (other != unidad) // No contar la casilla del enemigo actual
                        ocupadas.Add(tilemap.WorldToCell(other.transform.position));
                }
                var casillasMovimiento = visualizadorMovimiento.MostrarCasillasAccesibles(tilemap.WorldToCell(unidad.transform.position), unidad.datos, terrainLibrary, ocupadas, true);
                visualizadorMovimiento.MostrarRangoAtaque(casillasMovimiento, unidad.datos.clase.rangoAtaqueMinimo, unidad.datos.clase.rangoAtaqueMaximo, unidad.datos);
            }
        }
    }

    // Seleccionar un enemigo y mostrar su rango de ataque
    private void SeleccionarEnemigo()
    {
        Vector3Int cell = tilemap.WorldToCell(transform.position);
        Collider2D col = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(cell));

        if (col != null && col.TryGetComponent(out UnitLoader u))
        {
            if (u.datos.estado == "Enemigo" || u.datos.estado == "Jefe")
            {
                if (enemigoSeleccionado == u.datos)
                {
                    // Si se presiona el mismo enemigo, borrar su rango
                    visualizadorMovimiento.LimpiarRangoDeEnemigo(u.datos);
                    enemigoSeleccionado = null;
                }
                else
                {
                    if (enemigoSeleccionado != null)
                    {
                        visualizadorMovimiento.LimpiarRangoDeEnemigo(enemigoSeleccionado);
                    }

                    var ocupadas = new List<Vector3Int>();
                    foreach (var other in FindObjectsOfType<UnitLoader>())
                    {
                        if (other.datos != enemigoSeleccionado)
                            ocupadas.Add(tilemap.WorldToCell(other.transform.position));
                    }

                    var casillasMovimiento = visualizadorMovimiento.MostrarCasillasAccesibles(cell, u.datos, terrainLibrary, ocupadas, false);
                    var rango = new List<Vector3Int>();

                    foreach (var casilla in casillasMovimiento)
                    {
                        for (int dx = -u.datos.clase.rangoAtaqueMaximo; dx <= u.datos.clase.rangoAtaqueMaximo; dx++)
                        {
                            for (int dy = -u.datos.clase.rangoAtaqueMaximo; dy <= u.datos.clase.rangoAtaqueMaximo; dy++)
                            {
                                int distancia = Mathf.Abs(dx) + Mathf.Abs(dy);
                                if (distancia >= u.datos.clase.rangoAtaqueMinimo && distancia <= u.datos.clase.rangoAtaqueMaximo)
                                {
                                    Vector3Int pos = new Vector3Int(casilla.x + dx, casilla.y + dy, 0);
                                    rango.Add(pos);
                                }
                            }
                        }
                    }

                    visualizadorMovimiento.MostrarRangoDeEnemigo(u.datos, rango);
                    enemigoSeleccionado = u.datos;
                }
            }
        }
    }

    public void ActualizarRangoEnemigo()
    {
        if (enemigoSeleccionado == null)
            return;

        visualizadorMovimiento.LimpiarRangoDeEnemigo(enemigoSeleccionado);

        var ocupadas = new List<Vector3Int>();
        foreach (var other in FindObjectsOfType<UnitLoader>())
        {
            if (other.datos != enemigoSeleccionado)
                ocupadas.Add(tilemap.WorldToCell(other.transform.position));
        }

        UnitLoader loaderEnemigo = null;
        foreach (var unit in FindObjectsOfType<UnitLoader>())
        {
            if (unit.datos == enemigoSeleccionado)
            {
                loaderEnemigo = unit;
                break;
            }
        }

        if (loaderEnemigo != null)
        {
            Vector3Int enemigoCell = tilemap.WorldToCell(loaderEnemigo.transform.position);
            var casillasMovimiento = visualizadorMovimiento.MostrarCasillasAccesibles(enemigoCell, enemigoSeleccionado, terrainLibrary, ocupadas, false);
            var nuevoRango = new List<Vector3Int>();

            foreach (var casilla in casillasMovimiento)
            {
                for (int dx = -enemigoSeleccionado.clase.rangoAtaqueMaximo; dx <= enemigoSeleccionado.clase.rangoAtaqueMaximo; dx++)
                {
                    for (int dy = -enemigoSeleccionado.clase.rangoAtaqueMaximo; dy <= enemigoSeleccionado.clase.rangoAtaqueMaximo; dy++)
                    {
                        int distancia = Mathf.Abs(dx) + Mathf.Abs(dy);
                        if (distancia >= enemigoSeleccionado.clase.rangoAtaqueMinimo &&
                            distancia <= enemigoSeleccionado.clase.rangoAtaqueMaximo)
                        {
                            Vector3Int pos = new Vector3Int(casilla.x + dx, casilla.y + dy, 0);
                            nuevoRango.Add(pos);
                        }
                    }
                }
            }

            visualizadorMovimiento.MostrarRangoDeEnemigo(enemigoSeleccionado, nuevoRango);
        }    
    }
}
