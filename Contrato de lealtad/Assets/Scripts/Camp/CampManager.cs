using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.IO;

public class CampManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] menuOptions; // Array de opciones de menú (Botones o Imágenes)
    [SerializeField] private GameObject[] activeImages;  // Array de imágenes activadas para cada opción
    [SerializeField] private GameObject[] inactiveImages; // Array de imágenes desactivadas para cada opción
    [SerializeField] private Image background; // Array de imágenes desactivadas para cada opción

    private int currentSelectionIndex = 0;
    public bool isMenuActive = true;
    public int entrenamientos;
    private GameManager gameManager;

    private string unidadSavePath = "Assets/Resources/unitsData_partida.json";
    private string chapterSavePath = "Assets/Resources/chapterData_partida.json";
    private string unlockedSavePath = "Assets/Resources/unlockedChapters_partida.json"; // Ruta para guardar

    public static CampManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        Sprite fondo = Resources.Load<Sprite>($"Backgrounds/{gameManager.fondoCampamento}");
        if (fondo != null)
        {
            background.sprite = fondo;
        }
        else
        {
            Debug.LogWarning($"No se encontró el fondo: {gameManager.fondoCampamento}");
        }
        if(!string.IsNullOrEmpty(gameManager.musicaCampamento))
        {
            MusicManager.Instance.PlayMusic(gameManager.musicaCampamento);
        }
        UpdateMenu();
        entrenamientos = GameManager.Instance.chapterDataJuego.entrenar;
    }

    private void Update()
    {
        if (!isMenuActive) return;
        // Navegar con las flechas
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelectionIndex--;
            if (currentSelectionIndex < 0) currentSelectionIndex = menuOptions.Length - 1;
            UpdateMenu();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelectionIndex++;
            if (currentSelectionIndex >= menuOptions.Length) currentSelectionIndex = 0;
            UpdateMenu();
        }

        // Confirmar selección con "A"
        if (Input.GetKeyDown(KeyCode.A))
        {
            SelectOption();
        }

        // Volver atrás con "S"
        if (Input.GetKeyDown(KeyCode.S))
        {
            GoBack(); // Método para retroceder o cancelar
        }
    }

    private void UpdateMenu()
    {
        // Desactivar todas las imágenes activadas y activar las desactivadas
        for (int i = 0; i < menuOptions.Length; i++)
        {
            // Desactivar imagen activada y activar imagen desactivada
            activeImages[i].SetActive(false);
            inactiveImages[i].SetActive(true);
        }

        // Activar la imagen de la opción seleccionada
        activeImages[currentSelectionIndex].SetActive(true);
        inactiveImages[currentSelectionIndex].SetActive(false);
    }

    // Función que se llama cuando el jugador selecciona una opción
    public void SelectOption()
    {
        isMenuActive = false;
        switch (currentSelectionIndex)
        {
            case 0: // Contratar
                ContractMenu contractMenu = FindObjectOfType<ContractMenu>(true);
                contractMenu.gameObject.SetActive(true);
                break;
            case 1: // Conversar
                ConverseMenu converseMenu = FindObjectOfType<ConverseMenu>(true);
                converseMenu.gameObject.SetActive(true);
                break;
            case 2: // Inventario
                InventoryMenu inventoryMenu = FindObjectOfType<InventoryMenu>(true);
                inventoryMenu.gameObject.SetActive(true);
                break;
            case 3: // Entrenar
                TrainMenu trainMenu = FindObjectOfType<TrainMenu>(true);
                trainMenu.gameObject.SetActive(true);
                break;
            case 4: // Ajustes
                SettingsMenu settingsMenu = FindObjectOfType<SettingsMenu>(true);
                settingsMenu.Abrir();
                break;
            case 5: // Tutorial
                TutorialMenu tutorialMenu = FindObjectOfType<TutorialMenu>(true);
                tutorialMenu.Abrir();
                break;
            case 6: // Guardar
                SaveMenu saveMenu = FindObjectOfType<SaveMenu>(true);
                saveMenu.gameObject.SetActive(true);
                break;
            case 7: // Continuar
                ContinueMenu continueMenu = FindObjectOfType<ContinueMenu>(true);
                continueMenu.OpenContinueMenu();
                break;
        }
    }

    // Método para manejar la acción de volver atrás o cancelar
    public void GoBack()
    {
        Debug.Log("Volviendo atrás o cancelando...");
        // Aquí puedes implementar la lógica para volver al menú principal o salir de una opción
        // Puedes llamar a un método que cierre el menú o muestre el menú anterior
    }

    public void GuardarPartida()
    {
        // Guardar unidades
        string unidadesJson = JsonConvert.SerializeObject(GameManager.Instance.datosJuego, Formatting.Indented);
        File.WriteAllText(unidadSavePath, unidadesJson);
        Debug.Log("Unidades guardadas.");

        // Guardar progreso de capítulos
        string chapterJson = JsonConvert.SerializeObject(GameManager.Instance.chapterDataJuego, Formatting.Indented);
        File.WriteAllText(chapterSavePath, chapterJson);
        Debug.Log("Progreso de capítulos guardado.");

        // Guardar capítulos desbloqueados
        string unlockedJson = JsonConvert.SerializeObject(GameManager.Instance.unlockedChapterDataJuego, Formatting.Indented);
        File.WriteAllText(unlockedSavePath, unlockedJson);
        Debug.Log("Capítulos desbloqueados guardados.");

        // Guardar objetos almacenados
        AlmacenObjetos.Instance.GuardarObjetos();  // Guardamos los objetos almacenados
        SupportManager.Instance.GuardarApoyosPendientes();
        SupportManager.Instance.GuardarDesviosPendientes();
    }
}
