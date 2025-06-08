using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    public Tilemap tilemap;
    [SerializeField] private TerrainLibrary terrainLibrary;
    [SerializeField] private MovementRangeVisualizer visualizer;
    [SerializeField] private PointerController pointer;
    public bool fightConversationFinished = true;

    void Awake()
    {
        if (pointer == null)
        {
            pointer = FindObjectOfType<PointerController>();
        }

        if (visualizer == null)
        {
            visualizer = FindObjectOfType<MovementRangeVisualizer>();
        }

        if (visualizer != null)
        {
            tilemap = visualizer.tilemap;
        }
        else
        {
            Debug.LogError("No se encontró un MovementRangeVisualizer en la escena.");
        }
    }

    public IEnumerator TomarDecision(UnitLoader unidad)
    {
        if (unidad == null)
        {
            Debug.Log("La unidad es nula, finalizando decisión.");
            yield break; // Si la unidad está destruida, no continuar
        }
        // Paso 0: Comprobar tipo de IA
        if (unidad.datos.ia == "Defensiva")
        {
            // Solo atacar si hay enemigos ya en rango
            List<UnitLoader> enemigosCercanos = GetEnemigosEnRangoDeMovimientoYAtaque(unidad);

            if (enemigosCercanos.Count == 0)
            {
                Debug.Log($"{unidad.datos.nombre} es defensiva y no ataca ni se mueve.");
                unidad.MarcarComoUsada();
                yield break;
            }
            else
            {
                // Hay enemigos en rango, cambia IA a ofensiva y ataca
                Debug.Log($"{unidad.datos.nombre} cambia a IA Ofensiva para atacar.");
                unidad.datos.ia = "Ofensiva";
            }
        }

        // Paso 1: Detectar unidades en rango
        List<UnitLoader> enemigosEnRango = GetEnemigosEnRangoDeMovimientoYAtaque(unidad);

        if (enemigosEnRango.Count == 0)
        {
            // No hay enemigos en rango: mover hacia el más cercano
            UnitLoader objetivoCercano = BuscarEnemigoMasCercano(unidad);
            Vector2Int casilla = CalcularCasillaCercanaA(unidad, objetivoCercano);
            MoverUnidad(unidad, casilla, tilemap, terrainLibrary);
            yield break;
        }

        // Paso 2: Seleccionar objetivo óptimo
        UnitLoader objetivo = SeleccionarObjetivoPrioritario(unidad, enemigosEnRango);

        // Paso 3: Buscar casilla óptima para atacar
        Vector2Int? casillaDesdeLaQueAtacar = BuscarCasillaDesdeLaQueAtacar(unidad, objetivo);

        if (casillaDesdeLaQueAtacar.HasValue)
        {
            MoverUnidad(unidad, casillaDesdeLaQueAtacar.Value, tilemap, terrainLibrary);
            yield return StartCoroutine(Atacar(unidad, objetivo));
            unidad.MarcarComoUsada();
        }
        else
        {
            // Paso 4: Buscar casilla alternativa de ataque
            Vector2Int? casillaAlternativa = BuscarOtraCasillaDeAtaque(unidad, objetivo);
            if (casillaAlternativa.HasValue)
            {
                MoverUnidad(unidad, casillaAlternativa.Value, tilemap, terrainLibrary);
                yield return StartCoroutine(Atacar(unidad, objetivo));
                unidad.MarcarComoUsada();
            }
            else
            {
                // Paso 5: Ver si hay otro enemigo a atacar
                UnitLoader otroObjetivo = BuscarOtroObjetivo(unidad);
                if (otroObjetivo != null)
                {
                    yield return StartCoroutine(TomarDecision(unidad)); // Volver a intentar con otro objetivo
                }
                else
                {
                    Debug.Log($"{unidad.datos.nombre} mantiene su posición.");
                    unidad.MarcarComoUsada();
                }
            }
        }
    }

    List<UnitLoader> GetEnemigosEnRangoDeMovimientoYAtaque(UnitLoader unidad)
    {
        var ocupadas = new List<Vector3Int>();
        foreach (var other in FindObjectsOfType<UnitLoader>())
        {
            if (other != unidad) // Evita contar su propia casilla como ocupada
                ocupadas.Add(tilemap.WorldToCell(other.transform.position));
        }
        List<Vector3Int> movibles = visualizer.MostrarCasillasAccesiblesEnemigo(tilemap.WorldToCell(unidad.transform.position), unidad.datos, terrainLibrary, ocupadas, false);
        List<UnitLoader> enemigosDetectados = new();

        foreach (var casilla in movibles)
        {
            var enemigos = UnitLoader.ObtenerEnemigosEnRangoDesde(
                casilla,
                unidad.datos.clase.rangoAtaqueMinimo,
                unidad.datos.clase.rangoAtaqueMaximo,
                soyAliado: unidad.esAliado, // Asumiendo que tienes este bool
                tilemap);

            enemigosDetectados.AddRange(enemigos);
        }

        return enemigosDetectados.Distinct().ToList();
    }

    public HashSet<Vector3Int> ObtenerCasillasDeAtaque(List<Vector3Int> origenes, int rangoMin, int rangoMax)
    {
        HashSet<Vector3Int> casillas = new HashSet<Vector3Int>();
        foreach (Vector3Int posicion in origenes)
        {
            for (int dx = -rangoMax; dx <= rangoMax; dx++)
            {
                for (int dy = -rangoMax; dy <= rangoMax; dy++)
                {
                    int distancia = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (distancia >= rangoMin && distancia <= rangoMax)
                    {
                        Vector3Int ataquePos = new Vector3Int(posicion.x + dx, posicion.y + dy, 0);
                        casillas.Add(ataquePos);
                    }
                }
            }
        }
        return casillas;
    }

    UnitLoader SeleccionarObjetivoPrioritario(UnitLoader atacante, List<UnitLoader> posiblesObjetivos)
    {
        var derrotables = posiblesObjetivos
            .Where(o => CalcularDano(atacante, o) >= o.datos.PV)
            .OrderBy(o => (float)o.datos.PV / o.datos.MaxPV)
            .ToList();

        if (derrotables.Count > 0)
            return derrotables.First();

        return posiblesObjetivos
            .OrderByDescending(o => CalcularDano(atacante, o))
            .ThenBy(o => o.datos.PV)
            .First();
    }

    Vector2Int? BuscarCasillaDesdeLaQueAtacar(UnitLoader atacante, UnitLoader objetivo)
    {
        // 1. Obtener casillas accesibles para el atacante
        List<Vector3Int> ocupadas = new List<Vector3Int>();
        foreach (var other in FindObjectsOfType<UnitLoader>())
        {
            if (other != atacante)
                ocupadas.Add(tilemap.WorldToCell(other.transform.position));
        }

        var casillasAccesibles = visualizer.MostrarCasillasAccesiblesEnemigo(
            tilemap.WorldToCell(atacante.transform.position),
            atacante.datos,
            terrainLibrary,
            ocupadas,
            false);

        // 2. Filtrar casillas desde las que se puede atacar al objetivo
        Vector3Int celdaObjetivo = tilemap.WorldToCell(objetivo.transform.position);
        var casillasDesdeDondeAtacar = casillasAccesibles
            .Where(casilla =>
            {
                int distancia = Mathf.Abs(casilla.x - celdaObjetivo.x) + Mathf.Abs(casilla.y - celdaObjetivo.y);
                return distancia >= atacante.datos.clase.rangoAtaqueMinimo && distancia <= atacante.datos.clase.rangoAtaqueMaximo;
            })
            .Select(c => (Vector2Int)c)
            .ToList();

        // 3. Ordenar según prioridad
        return casillasDesdeDondeAtacar
            .OrderBy(c => CalcularPrioridadDeCasilla(atacante, objetivo, c))
            .FirstOrDefault();
    }

    public void MoverUnidad(UnitLoader unidad, Vector2Int casillaDestino, Tilemap tilemap, TerrainLibrary terrainLibrary)
    {
        unidad.transform.position = tilemap.GetCellCenterWorld(new Vector3Int(casillaDestino.x, casillaDestino.y, 0));
        unidad.ActualizarBonosPorTerreno(tilemap, terrainLibrary);

        // Puedes opcionalmente marcar que ya se movió, si usas ese estado
        // unidad.yaSeMovio = true; // si lo manejas
    }

    public IEnumerator Atacar(UnitLoader atacante, UnitLoader objetivo)
    {
        yield return StartCoroutine(RealizarCombateIA(atacante, objetivo));

        if (atacante != null)
        {
            atacante.MarcarComoUsada();
            yield return TurnManager.Instancia.NotificarUnidadTerminada(atacante);
        }
    }

    int CalcularDistancia(Vector3 pos1, Vector3 pos2)
    {
        Vector3Int cell1 = FindObjectOfType<Tilemap>().WorldToCell(pos1);
        Vector3Int cell2 = FindObjectOfType<Tilemap>().WorldToCell(pos2);
        return Mathf.Abs(cell1.x - cell2.x) + Mathf.Abs(cell1.y - cell2.y);
    }

    private IEnumerator RealizarCombateIA(UnitLoader atacante, UnitLoader atacado)
    {
        Debug.Log(atacante.datos.estado);
        if (atacante.datos.estado == "Jefe")
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
            ConversationManager.Instancia.StartFightConversation(atacado.datos.nombre, chapterNumber, () => fightConversationFinished = true);
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

        if (atacante.datos.PV == 0)
        {
            if (atacante.datos == pointer.enemigoSeleccionado){
                visualizer.LimpiarRangoDeEnemigo(pointer.enemigoSeleccionado);
            }
            if (ActionMenu.Instancia != null)
            {
                yield return StartCoroutine(ActionMenu.Instancia.EsperarRetiradaYFinalizar(atacante.datos.nombre));
            }
            yield return null;
        }

        if (atacado.datos.PV == 0)
        {
            if (ActionMenu.Instancia != null)
            {
                yield return StartCoroutine(ActionMenu.Instancia.EsperarRetiradaYFinalizar(atacado.datos.nombre));
            }
            atacado.LimpiarBonosTerreno();
            atacado.LimpiarPotenciadores();
            atacado.LimpiarArtefactosPasivos();
            atacado.LimpiarEfectosEntrantes();
            atacado.datos.PV = atacado.datos.MaxPV;
            TurnManager.Instancia.unidadesAGuardar.Add(atacado.datos);
            TurnManager.Instancia.unidadesJugador.Remove(atacado);
            Destroy(atacado.gameObject);
            TurnManager.Instancia.ComprobarDerrota(); 
            yield break;
        }

        if (atacado != null)
        {
            // Solo ejecutar si el objeto sigue vivo
            int leveldiff = atacante.datos.nivel - atacado.datos.nivel;
            if (leveldiff < 0) leveldiff = 0;
            int experienciaGanada = (atacante.datos.PV == 0) ? 30 + (5 * leveldiff) : 15 + (5 * leveldiff);
            atacado.GanarExp(atacado.datos, experienciaGanada);

            foreach (var aliada in atacado.UnidadesAliadasAdyacentes())
            {
                SupportManager.Instance.AñadirAfecto(atacado.datos.nombre, aliada.datos.nombre, 2);
            }
        }

        yield return null;
    }

    UnitLoader BuscarEnemigoMasCercano(UnitLoader unidad)
    {
        var enemigos = (unidad.esAliado 
            ? TurnManager.Instancia.unidadesEnemigas 
            : TurnManager.Instancia.unidadesJugador)
            .Where(u => u != unidad)
            .ToList();

        UnitLoader masCercano = null;
        float distanciaMinima = float.MaxValue;

        Vector2Int posicionUnidad = (Vector2Int)tilemap.WorldToCell(unidad.transform.position);

        foreach (var enemigo in enemigos)
        {
            Vector2Int posicionEnemigo = (Vector2Int)tilemap.WorldToCell(enemigo.transform.position);
            float distancia = Vector2Int.Distance(posicionUnidad, posicionEnemigo);

            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                masCercano = enemigo;
            }
        }

        return masCercano;
    }

    Vector2Int CalcularCasillaCercanaA(UnitLoader unidad, UnitLoader objetivo)
    {
        var ocupadas = new List<Vector3Int>();
        foreach (var other in FindObjectsOfType<UnitLoader>())
        {
            if (other != unidad) // Evita contar su propia casilla como ocupada
                ocupadas.Add(tilemap.WorldToCell(other.transform.position));
        }
        var casillas = visualizer.MostrarCasillasAccesiblesEnemigo(tilemap.WorldToCell(unidad.transform.position), unidad.datos, terrainLibrary, ocupadas, false);

        Vector2Int mejorCasilla = (Vector2Int)tilemap.WorldToCell(unidad.transform.position);
        float distanciaMinima = float.MaxValue;

        foreach (var casilla in casillas)
        {
            Vector2Int celdaObjetivo = (Vector2Int)tilemap.WorldToCell(objetivo.transform.position);
            float distancia = Vector2Int.Distance((Vector2Int)casilla, celdaObjetivo);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                mejorCasilla = (Vector2Int)casilla;
            }
        }

        return mejorCasilla;
    }

    Vector2Int? BuscarOtraCasillaDeAtaque(UnitLoader atacante, UnitLoader objetivo)
    {
        var ocupadas = new List<Vector3Int>();
        foreach (var other in FindObjectsOfType<UnitLoader>())
        {
            if (other != atacante) // Evita contar su propia casilla como ocupada
                ocupadas.Add(tilemap.WorldToCell(other.transform.position));
        }
        var casillasAccesibles = visualizer.MostrarCasillasAccesiblesEnemigo(tilemap.WorldToCell(atacante.transform.position), atacante.datos, terrainLibrary, ocupadas, false);
        var casillasDeAtaque = ObtenerCasillasDeAtaque(casillasAccesibles, atacante.datos.clase.rangoAtaqueMinimo, atacante.datos.clase.rangoAtaqueMaximo);

        foreach (var casilla in casillasAccesibles)
        {
            Vector3Int objetivoCelda = tilemap.WorldToCell(objetivo.transform.position);
            int distancia = Mathf.Abs(casilla.x - objetivoCelda.x) + Mathf.Abs(casilla.y - objetivoCelda.y);
            if (distancia >= atacante.datos.clase.rangoAtaqueMinimo && distancia <= atacante.datos.clase.rangoAtaqueMaximo)
            {
                // Confirmar que no está ocupada
                if (!ocupadas.Contains(casilla))
                {
                    return (Vector2Int)casilla;
                }
            }
        }

        return null; // No encontró casilla desde donde atacar
    }

    UnitLoader BuscarOtroObjetivo(UnitLoader unidad)
    {
        var ocupadas = new List<Vector3Int>();
        foreach (var other in FindObjectsOfType<UnitLoader>())
        {
            if (other != unidad) // Evita contar su propia casilla como ocupada
                ocupadas.Add(tilemap.WorldToCell(other.transform.position));
        }
        var casillasAccesibles = visualizer.MostrarCasillasAccesiblesEnemigo(tilemap.WorldToCell(unidad.transform.position), unidad.datos, terrainLibrary, ocupadas, false);

        var enemigos = (unidad.esAliado 
            ? TurnManager.Instancia.unidadesEnemigas 
            : TurnManager.Instancia.unidadesJugador)
            .Where(u => u != unidad)
            .ToList();

        foreach (var enemigo in enemigos)
        {
            foreach (var casilla in casillasAccesibles)
            {
                Vector3Int objetivoCelda = tilemap.WorldToCell(enemigo.transform.position);
                int distancia = Mathf.Abs(casilla.x - objetivoCelda.x) + Mathf.Abs(casilla.y - objetivoCelda.y);
                if (distancia >= unidad.datos.clase.rangoAtaqueMinimo && distancia <= unidad.datos.clase.rangoAtaqueMaximo)
                {
                    return enemigo; // Este enemigo puede ser atacado desde al menos una casilla accesible
                }
            }
        }

        return null;
    }

    public int CalcularDano(UnitLoader atacante, UnitLoader objetivo)
    {
        string tipoDano = atacante.datos.clase.tipoDano;
        int poder = atacante.datos.poder;
        int defensaObjetivo = tipoDano == "Fisico" ? objetivo.datos.defensa : objetivo.datos.resistencia;

        int daño = Mathf.Max(0, poder - defensaObjetivo);

        // Probabilidad de crítico
        bool esCritico = Random.Range(1, 100) <= Mathf.Max(0, atacante.datos.habilidad - objetivo.datos.suerte);
        if (esCritico)
            daño *= 2;

        // Bonus por doble ataque (por ejemplo, si la IA quiere considerar que atacará dos veces)
        if (atacante.datos.velocidad > objetivo.datos.velocidad)
            daño += Mathf.Max(0, poder - defensaObjetivo);

        return daño;
    }

    private float CalcularPrioridadDeCasilla(UnitLoader atacante, UnitLoader objetivo, Vector2Int casilla)
    {
        float prioridad = 0f;

        // 1. Distancia al objetivo (más cerca = mejor)
        float distancia = Vector2Int.Distance(casilla, (Vector2Int)tilemap.WorldToCell(objetivo.transform.position));
        prioridad += distancia;
        Vector3Int objetivoCelda = tilemap.WorldToCell(objetivo.transform.position);
        // 2. Penalizar si está en rango de contraataque del objetivo
        int distanciaManhattan = Mathf.Abs(casilla.x - objetivoCelda.x) + Mathf.Abs(casilla.y - objetivoCelda.y);
        if (distanciaManhattan >= objetivo.datos.clase.rangoAtaqueMinimo &&
            distanciaManhattan <= objetivo.datos.clase.rangoAtaqueMaximo)
        {
            prioridad += 5f; // Penalización por arriesgar contraataque
        }
        
        return prioridad;
    }
}