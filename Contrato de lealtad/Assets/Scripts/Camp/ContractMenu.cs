using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using System;

public class ContractMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI contractCountText; // Texto que muestra la cantidad de contratos
    [SerializeField] private GameObject[] mercenarySlots; // Los espacios donde se mostrarán los mercenarios
    [SerializeField] private Image[] mercenaryPortraits; // Retratos de los mercenarios
    [SerializeField] private Image[] mercenaryClassImages; // Imágenes de las clases de los mercenarios
    [SerializeField] private TextMeshProUGUI[] mercenaryData; // Nombres de los mercenarios
    [SerializeField] private TextMeshProUGUI[] mercenaryDescriptions; // Descripciones de los mercenarios
    [SerializeField] private TextMeshProUGUI confirmationMenu; // Menú de confirmación
    [SerializeField] private GameObject startButton; // Boton de empezar

    private List<Unidad> availableUnits; // Unidades disponibles para el contrato
    private int currentSelectionIndex = 0; // Indice del mercenario seleccionado
    private int contractCount; // Número de contratos disponibles
    private Unidad selectedUnit; // Unidad seleccionada para ser reclutada
    private List<Unidad> seleccionados = new List<Unidad>();
    private enum MenuState
    {
        Initialization, // Estado inicial, antes de que el jugador haya seleccionado un mercenario
        Selection,     // Fase de selección de mercenarios
        Confirmation   // Fase de confirmación de reclutamiento
    }

    private MenuState currentState; // Estado inicial es la inicialización

    private string unitsDataPath = "Assets/Resources/unitsData.json"; // Ruta de archivo JSON

    public void Start()
    {
        // Cargar los datos de unidades desde el archivo JSON
        currentState = MenuState.Initialization;
        currentSelectionIndex = 0;
        UpdateContractCount();
        startButton.SetActive(true); // Mostrar el botón de inicio
        contractCountText.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        currentState = MenuState.Initialization;
        currentSelectionIndex = 0;
        UpdateContractCount();
        startButton.SetActive(true); // Mostrar el botón de inicio
        contractCountText.gameObject.SetActive(true); // Actualizar contratos visibles
    }

    private void LoadUnitsData()
    {
        Debug.Log("Cargando datos de unidades");
        // Leer el archivo JSON de unidades
        string json = File.ReadAllText(unitsDataPath);
        DatosJuego unitData = JsonConvert.DeserializeObject<DatosJuego>(json);
        availableUnits = new List<Unidad>();

        // Filtrar las unidades con estado "Libre"
        foreach (var unit in unitData.unidades)
        {
            if (unit.estado == "Libre")
            {
                availableUnits.Add(unit);
            }
        }

        // Si hay unidades disponibles y contratos, desplegarlas
        if (contractCount > 0 && availableUnits.Count > 0)
        {
            DisplayMercenaries();
        }
    }

    private void DisplayMercenaries()
    {
        for (int i = 0; i < mercenarySlots.Length; i++)
        {
            mercenaryData[i].text = "";
            mercenaryDescriptions[i].text = "";
            mercenaryPortraits[i].sprite = null;
            mercenaryClassImages[i].sprite = null;
            mercenarySlots[i].SetActive(false); // Oculta slots no usados o previos
        }
        int count = Mathf.Min(5, availableUnits.Count); // No mostrar más de lo disponible
        seleccionados.Clear();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableUnits.Count);
            Unidad unit = availableUnits[randomIndex];

            while (seleccionados.Contains(unit)) // Evitar repetir unidades
            {
                randomIndex = UnityEngine.Random.Range(0, availableUnits.Count);
                unit = availableUnits[randomIndex];
            }

            seleccionados.Add(unit);

            // Asignar información de UI
            mercenaryData[i].text = $"{unit.nombre} - Nivel: {unit.nivel} - Clase: {unit.clase.nombre}.";
            mercenaryDescriptions[i].text = $"{unit.descripcion}";

            Sprite portrait = Resources.Load<Sprite>($"Portraits/{unit.nombre}Neutral");
            if (portrait != null)
            {
                mercenaryPortraits[i].sprite = portrait; // Asignamos el retrato al Image
            }
            else
            {
                Debug.LogWarning($"No se encontró el retrato para {unit.nombre}Neutral");
            }

            // Cargar y asignar la imagen de la clase
            Sprite classSprite = Resources.Load<Sprite>($"Sprites/{unit.clase.nombre.Replace(" ", "")}{unit.nombre}");
            if (classSprite != null)
            {
                mercenaryClassImages[i].sprite = classSprite; // Asignamos la clase al Image
            }
            else
            {
                Debug.LogWarning($"No se encontró la imagen de clase para {unit.clase.nombre}{unit.nombre}");
            }

            mercenarySlots[i].SetActive(true);
        }
        currentState = MenuState.Selection;
    }

    private void UpdateContractCount()
    {
        // Actualizar el texto con la cantidad de contratos
        contractCount = GameManager.Instance.chapterDataJuego.contratos; // Suponiendo que la cantidad de contratos se almacena en el GameManager
        contractCountText.text = $"Contratos disponibles: {contractCount}";
    }

    void Update()
    {
        if (currentState == MenuState.Initialization)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                InitializeMenu();
            }
            // Si se pulsa "S" en el estado de inicialización, cerrar el menú de contratos y volver al campamento
            if (Input.GetKeyDown(KeyCode.S))
            {
                CloseContractMenu();
            }
        }
        else if (currentState == MenuState.Selection)
        {
            // Navegar entre mercenarios con las flechas
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentSelectionIndex = (currentSelectionIndex - 1 + 5) % 5; // Asegurarse que el índice esté en el rango correcto
                UpdateMercenarySelection();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentSelectionIndex = (currentSelectionIndex + 1) % 5;
                UpdateMercenarySelection();
            }

            // Seleccionar un mercenario con "A"
            if (Input.GetKeyDown(KeyCode.A))
            {
                SelectMercenary();
            }

        }
        else if (currentState == MenuState.Confirmation)
        {
            // Confirmar reclutamiento con "A"
            if (Input.GetKeyDown(KeyCode.A))
            {
                ConfirmRecruited();
            }

            // Cancelar con "S" y volver al estado de selección
            if (Input.GetKeyDown(KeyCode.S))
            {
                CancelRecruited();
            }
        }
    }

    private void InitializeMenu()
    {
        startButton.SetActive(false); // Ocultar el botón de inicio
        LoadUnitsData();  // Ahora sí carga los mercenarios
    }

    private void UpdateMercenarySelection()
    {
        // Resaltar el mercenario seleccionado
        for (int i = 0; i < 5; i++)
        {
            if (i == currentSelectionIndex)
            {
                // Mercenario seleccionado: mantener el color normal
                mercenarySlots[i].GetComponent<Image>().color = Color.white; // Color normal
            }
            else
            {
                // Mercenario no seleccionado: oscurecer el color
                mercenarySlots[i].GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f); // Color oscuro
            }
        }
    }

    private void SelectMercenary()
    {
        if (contractCount > 0)
        {
            // Confirmar el reclutamiento del mercenario
            selectedUnit = seleccionados[currentSelectionIndex];
            confirmationMenu.gameObject.SetActive(true); // Mostrar el menú de confirmación
            confirmationMenu.text = $"¿Reclutar a {selectedUnit.nombre}?";
            currentState = MenuState.Confirmation;
        }
    }

    public void ConfirmRecruited()
    {
        // Cambiar el estado del mercenario a "Reclutado"
        selectedUnit.estado = "Reclutado";

        // Buscar y actualizar solo la unidad reclutada en la lista de unidades disponibles
        DatosJuego unitData = JsonConvert.DeserializeObject<DatosJuego>(File.ReadAllText(unitsDataPath));
        for (int i = 0; i < unitData.unidades.Length; i++)
        {
            // Si encontramos la unidad que fue seleccionada para ser reclutada
            if (unitData.unidades[i].nombre == selectedUnit.nombre)
            {
                unitData.unidades[i].estado = "Reclutado"; // Actualizar el estado
                break; // Salir del bucle una vez que encontramos la unidad
            }
        }

        // Guardar los datos actualizados en el archivo JSON
        string json = JsonConvert.SerializeObject(unitData, Formatting.Indented);
        File.WriteAllText(unitsDataPath, json);
        GameManager.Instance.datosJuego = unitData;

        // Decrementar los contratos
        GameManager.Instance.chapterDataJuego.contratos--;
        UpdateContractCount();

        for (int i = 0; i < GameManager.Instance.datosJuego.unidades.Length; i++)
        {
            if (GameManager.Instance.datosJuego.unidades[i].nombre == selectedUnit.nombre)
            {
                GameManager.Instance.datosJuego.unidades[i].estado = "Reclutado";
                break;
            }
        }
        
        // Cerrar el menú de confirmación
        confirmationMenu.gameObject.SetActive(false);
        CloseContractMenu(); // Volver al menú principal
    }

    public void CancelRecruited()
    {
        // Cancelar el reclutamiento y cerrar el menú de confirmación
        confirmationMenu.gameObject.SetActive(false);
        currentState = MenuState.Selection;
    }

    public void CloseContractMenu()
    {
        // Regresar al campamento y cerrar el menú de contratos
        // Aquí, debes asegurarte de que tu menú de campamento se reactive.
        GameObject campMenu = GameObject.Find("CampManager"); // Suponiendo que el objeto CampMenu tiene ese nombre
        if (campMenu != null)
        {
            campMenu.GetComponent<CampManager>().isMenuActive = true; // Hacerlo interactuable
        }
        ResetContractMenu();
        // Cerrar el menú de contratos
        OnCerrarContrato?.Invoke();
        gameObject.SetActive(false); // Desactivar el menú de contratos
    }

    private void ResetContractMenu()
    {
        availableUnits?.Clear();
        // Resetear selección
        currentSelectionIndex = 0;
        selectedUnit = null;
        seleccionados.Clear();

        // Limpiar UI de mercenarios
        for (int i = 0; i < mercenarySlots.Length; i++)
        {
            mercenaryData[i].text = "";
            mercenaryDescriptions[i].text = "";
            mercenaryPortraits[i].sprite = null;
            mercenaryClassImages[i].sprite = null;
            mercenarySlots[i].SetActive(false);
            mercenarySlots[i].GetComponent<Image>().color = Color.white;
        }

        // Resetear confirmación
        confirmationMenu.gameObject.SetActive(false);

        // Reestablecer estado
        currentState = MenuState.Initialization;
    }

    public Action OnCerrarContrato;
}
