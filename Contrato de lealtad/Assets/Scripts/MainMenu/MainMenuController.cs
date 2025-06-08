using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private SettingsMenu settingsPanel;

    private Button[] menuButtons;
    private TextMeshProUGUI[] buttonTexts;
    private int currentIndex = 0;
    private Color defaultColor = new Color(0.7568f, 0.302f, 0.0f, 1f);
    private Color selectedColor = Color.white;

    private void Start()
    {
        menuButtons = new Button[] { newGameButton, continueButton, settingsButton, quitButton };

        buttonTexts = new TextMeshProUGUI[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            buttonTexts[i] = menuButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        // Set initial selection
        SelectButton(currentIndex);

        // Assign listeners to buttons
        newGameButton.onClick.AddListener(OnNewGameClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void Update()
    {
        // Move Up
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + menuButtons.Length) % menuButtons.Length;
            SelectButton(currentIndex);
        }

        // Move Down
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % menuButtons.Length;
            SelectButton(currentIndex);
        }

        // Confirm selection with "A"
        if (Input.GetKeyDown(KeyCode.A))
        {
            menuButtons[currentIndex].onClick.Invoke();
        }

        // Cancel with "S" (You can customize this action)
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Cancel / Back action");
        }
    }

    private void SelectButton(int index)
    {
        // Ensure the button is visually selected
        EventSystem.current.SetSelectedGameObject(menuButtons[index].gameObject);
        menuButtons[index].Select();

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            buttonTexts[i].color = (i == index) ? selectedColor : defaultColor;
        }
    }

    private void OnNewGameClicked()
    {
        // Aquí después cargaremos el inicio del capítulo 1
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance es null. Asegúrate de que el GameManager está en la escena.");
            return;
        }

        Debug.Log("Nueva Partida Iniciada");
        SupportManager.Instance.BorrarDatosGuardados();
        GameManager.Instance.StartNewGame();
        // Por ahora solo cargaría la siguiente fase o escena placeholder
    }

    private void OnContinueClicked()
    {
        GameManager.Instance.CargarDatosPartida();
        SceneLoader.Instance.LoadScene("CampScene"); // o la escena que toque
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
        {
            settingsPanel.Abrir();
            this.enabled = false;
        }
        else
        {
            Debug.LogWarning("settingsPanel no asignado en el inspector");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Salir del juego");
        Application.Quit();
    }
}
