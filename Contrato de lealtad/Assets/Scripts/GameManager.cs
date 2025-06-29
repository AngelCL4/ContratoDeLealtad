using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GestionDeClases;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

[System.Serializable]
public class GamePersistentData
{
    public string lastMainChapter;
    public string fondoCampamento;
    public string musicaCampamento;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public Dictionary<Vector3Int, Unidad> mapaUnidades = new Dictionary<Vector3Int, Unidad>();
    public string currentChapter;
    public int contractCount;
    public string lastMainChapter;
    public string fondoCampamento;
    public string musicaCampamento;
    public bool bossMusicSounding = false;

    public int nivelMedioEjercito { get; private set; } = 1;

    public DatosJuego datosJuego; // Tus datos cargados desde el JSON

    public DatosJuego datosUnidades;

    public ChapterData chapterDataJuego = new();

    public ChapterData chapterDataActual = new();

    public UnlockedChaptersData unlockedChapterDataJuego = new();

    public UnlockedChaptersData unlockedChapterDataActual = new();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNewGame()
    {
        RestaurarUnitsDataBase(); // Restaurar datos limpios
        SupportManager.Instance.ResetearApoyosPendientes();
        SupportManager.Instance.ResetearDesviosPendientes();
        datosJuego = CargarDatosJuegoNuevo();
        chapterDataJuego = CargarDatosCapitulosJuegoNuevo();
        unlockedChapterDataJuego = CargarDatosDesbloqueosJuegoNuevo();

        if (datosJuego == null)
        {
            Debug.LogWarning("No se encontraron datos de juego, iniciando juego nuevo.");
        }

        // Eliminar archivo persistente
        string path = Application.persistentDataPath + "/persistentData.json";
        if (File.Exists(path))
            File.Delete(path);
            
        if (datosJuego != null)
        {
            ResetearEstadosDeUnidades();
            AsignarClasesAUnidades();
            AsignarObjetosAUnidades();
            GuardarUnidadesEnJson();
        }
        contractCount = chapterDataJuego.contratos;
        currentChapter = "Chapter1";
        SceneLoader.Instance.LoadScene("ChapterScene");
    }

    private DatosJuego CargarDatosJuegoNuevo()
    {
        string path = Path.Combine(Application.dataPath, "Resources/unitsData.json");

        if (!File.Exists(path))
        {
            Debug.LogError("No se pudo encontrar el archivo unitsData.json en disco.");
            return null;
        }

        string json = File.ReadAllText(path);
        DatosJuego datosJuego = JsonConvert.DeserializeObject<DatosJuego>(json);
        return datosJuego;
    }

    private ChapterData CargarDatosCapitulosJuegoNuevo()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("chapterData");
        if (jsonData == null)
        {
            Debug.LogError("No se pudo encontrar el archivo chapterData.json en Resources");
            return null;
        }

        ChapterData datosCapitulos = JsonUtility.FromJson<ChapterData>(jsonData.text);
        return datosCapitulos;
    }

    private UnlockedChaptersData CargarDatosDesbloqueosJuegoNuevo()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("unlockedChapters");
        if (jsonData == null)
        {
            Debug.LogError("No se pudo encontrar el archivo unlockedChapters.json en Resources");
            return null;
        }

        UnlockedChaptersData datosDesbloqueos = JsonConvert.DeserializeObject<UnlockedChaptersData>(jsonData.text);
        return datosDesbloqueos;
    }

    public void CargarDatosPartida()
    {
        string unidadSavePath = "Assets/Resources/unitsData_partida.json";
        string chapterSavePath = "Assets/Resources/chapterData_partida.json";
        string unlockedSavePath = "Assets/Resources/unlockedChapters_partida.json";

        if (File.Exists(unidadSavePath))
        {
            string unidadesJson = File.ReadAllText(unidadSavePath);
            datosUnidades = JsonConvert.DeserializeObject<DatosJuego>(unidadesJson);
            Debug.Log("Unidades cargadas.");
        }

        if (File.Exists(chapterSavePath))
        {
            string chapterJson = File.ReadAllText(chapterSavePath);
            chapterDataActual = JsonConvert.DeserializeObject<ChapterData>(chapterJson);
            Debug.Log("Capítulos cargados.");
        }

        if (File.Exists(unlockedSavePath))
        {
            string unlockedJson = File.ReadAllText(unlockedSavePath);
            unlockedChapterDataActual = JsonConvert.DeserializeObject<UnlockedChaptersData>(unlockedJson);
            Debug.Log("Capítulos desbloqueados cargados.");
        }

        // Cargar objetos almacenados
        AlmacenObjetos.Instance.CargarObjetos();  // Cargamos los objetos almacenados
        SupportManager.Instance.CargarApoyosPendientes();
        SupportManager.Instance.CargarDesviosPendientes();

        CargarDatosPersistentes();

        datosJuego = datosUnidades;

        chapterDataJuego = chapterDataActual;

        unlockedChapterDataJuego = unlockedChapterDataActual;
    }

    public void DesbloquearCapitulo(int capIndex)
    {
        if (capIndex >= 0 && capIndex < unlockedChapterDataJuego.chapters.Count)
        {
            unlockedChapterDataJuego.chapters[capIndex].estaDesbloqueado = true;
        }
    }

    private void ResetearEstadosDeUnidades()
    {
        if (datosJuego.unidades == null || datosJuego.unidades.Length == 0)
        {
            Debug.LogWarning("No hay unidades para resetear.");
            return;
        }

        for (int i = 0; i < datosJuego.unidades.Length; i++)
        {
            if (i >= 2)
            {
                datosJuego.unidades[i].estado = "Libre";
            }
            else
            {
                if (string.IsNullOrEmpty(datosJuego.unidades[i].estado))
                {
                    datosJuego.unidades[i].estado = "Reclutado";
                }
            }
        }

        // Guardar el archivo actualizado en Resources (Editor solamente)
        string path = Application.dataPath + "/Resources/unitsData.json";
        string json = JsonConvert.SerializeObject(datosJuego, Formatting.Indented);
        File.WriteAllText(path, json);

        Debug.Log("Estados de unidades reseteados y guardados.");
    }

    private void RestaurarUnitsDataBase()
    {
        string basePath = Path.Combine(Application.dataPath, "Resources/unitsData_base.json");
        string destinoPath = Path.Combine(Application.dataPath, "Resources/unitsData.json");

        if (File.Exists(basePath))
        {
            File.Copy(basePath, destinoPath, true); // Sobrescribe el actual
            Debug.Log("unitsData.json restaurado desde unitsData_base.json");
        }
        else
        {
            Debug.LogError("No se encontró unitsData_base.json para restaurar.");
        }
    }

    public void GuardarUnidadesEnJson()
    {
        string path = Path.Combine(Application.dataPath, "Resources/unitsData.json");
        string json = JsonConvert.SerializeObject(datosJuego, Formatting.Indented);
        File.WriteAllText(path, json);
        Debug.Log("Unidades guardadas con clase y objeto actualizado.");
    }

    private void AsignarClasesAUnidades()
    {
        if (datosJuego.unidades == null || datosJuego.unidades.Length == 0)
        {
            Debug.LogWarning("No hay unidades disponibles para asignar clases.");
            return;
        }

        foreach (Unidad unidad in datosJuego.unidades)
        {
            AsignarClase(unidad);
        }
    }

    void AsignarClase(Unidad unidad)
    {
        if (string.IsNullOrEmpty(unidad.clase.nombre))
        {
            // Filtrar solo clases de tier 'Basica'
            List<Clases> clasesBasicas = GestorDeClases.clasesDisponibles.FindAll(c => c.tier == "Basica");

            if (clasesBasicas.Count > 0)
            {
                Clases claseAleatoria = clasesBasicas[Random.Range(1, clasesBasicas.Count)];
                unidad.clase = claseAleatoria;
                Debug.Log(unidad.nombre);
                Debug.Log(unidad.clase.nombre);
            }
            else
            {
                Debug.LogError("No hay clases básicas disponibles para asignar.");
            }
        }
        else
        {
            // Si ya tiene una clase asignada, busca la clase por nombre en las clases disponibles
            Clases claseEncontrada = GestorDeClases.clasesDisponibles.Find(clase => clase.nombre == unidad.clase.nombre);
            
            if (claseEncontrada != null)
            {
                unidad.clase = claseEncontrada;
            }
            else
            {
                Debug.LogWarning("Clase no encontrada para " + unidad.nombre);
            }
        }
    }

    private void AsignarObjetosAUnidades()
    {
        if (datosJuego.unidades == null || datosJuego.unidades.Length == 0)
        {
            Debug.LogWarning("No hay unidades para asignar objetos.");
            return;
        }

        foreach (Unidad unidad in datosJuego.unidades)
        {
            if (!string.IsNullOrEmpty(unidad.objeto.nombre))
            {
                var objetoOriginal = ObjetoLoader.objetosDisponibles
                    .FirstOrDefault(o => o.nombre == unidad.objeto.nombre);

                if (objetoOriginal != null)
                {
                    unidad.objeto = objetoOriginal;
                }
                else
                {
                    Debug.LogWarning($"No se encontró el objeto '{unidad.objeto.nombre}' para la unidad '{unidad.nombre}'.");
                    unidad.objeto = null;
                }
            }
        }
    }

    public void ActualizarNivelMedioEjercito()
    {
        var reclutados = SupportManager.Instance.GetPersonajesReclutados(datosJuego.unidades.ToList());

        if (reclutados.Count == 0)
        {
            nivelMedioEjercito = 1;
            Debug.LogWarning("No hay unidades reclutadas. Nivel medio establecido a 1.");
            return;
        }

        float sumaNiveles = reclutados.Sum(u => u.nivel);
        nivelMedioEjercito = Mathf.RoundToInt(sumaNiveles / reclutados.Count);

        Debug.Log($"[GAME MANAGER] Nivel medio del ejército actualizado a {nivelMedioEjercito}");
    }

    public void AjustarNivelUnidadesLibres()
    {
        GameObject tempGO = new GameObject("TempUnitLoader");
        UnitLoader tempLoader = tempGO.AddComponent<UnitLoader>();
        foreach (Unidad unidad in datosJuego.unidades)
        {
            if (unidad.estado == "Libre" && unidad.nivel < nivelMedioEjercito)
            {
                while (unidad.nivel < nivelMedioEjercito)
                {
                    tempLoader.SubirNivel(unidad);
                }
                Debug.Log($"[UNIDAD LIBRE] {unidad.nombre} subida a nivel {unidad.nivel}");
                unidad.PV = unidad.MaxPV;
            }
        }
        GameObject.Destroy(tempGO);
    }

    private string persistentDataPath => Application.persistentDataPath + "/persistentData.json";

    public void GuardarDatosPersistentes()
    {
        GamePersistentData data = new()
        {
            lastMainChapter = this.lastMainChapter,
            fondoCampamento = this.fondoCampamento,
            musicaCampamento = this.musicaCampamento
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(persistentDataPath, json);
        Debug.Log("[Guardado] Datos persistentes guardados.");
    }

    public void CargarDatosPersistentes()
    {
        if (File.Exists(persistentDataPath))
        {
            string json = File.ReadAllText(persistentDataPath);
            GamePersistentData data = JsonUtility.FromJson<GamePersistentData>(json);
            this.lastMainChapter = data.lastMainChapter;
            this.fondoCampamento = data.fondoCampamento;
            this.musicaCampamento = data.musicaCampamento;
            Debug.Log("[Cargado] Datos persistentes cargados.");
        }
        else
        {
            Debug.Log("[Cargado] No se encontraron datos persistentes.");
        }
    }
}

