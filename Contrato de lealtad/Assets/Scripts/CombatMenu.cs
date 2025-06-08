using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatMenu : MonoBehaviour
{
    [SerializeField] private TutorialMenu tutorialMenu;
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private ObjectiveMenu objectiveMenu;
    [SerializeField] private TextMeshProUGUI textoTurnos;
    [SerializeField] private TerrenoUI uiTerreno;
    public GameObject menuPanel;
    public TextMeshProUGUI[] opciones;
    private int opcionSeleccionada = 0;
    public bool menuActivo = false;
    public bool finPulsado = false;

    public bool MenuActivo => menuActivo;

    void Start()
    {
        menuPanel.SetActive(false);
        ActualizarSeleccionVisual();
    }

    void Update()
    {
        if (!TurnManager.Instancia.conversationFinished) return;
        if (!menuActivo) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            opcionSeleccionada = (opcionSeleccionada - 1 + opciones.Length) % opciones.Length;
            ActualizarSeleccionVisual();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            opcionSeleccionada = (opcionSeleccionada + 1) % opciones.Length;
            ActualizarSeleccionVisual();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            CerrarMenu();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            EjecutarOpcionSeleccionada();
        }
    }

    public void ActualizarTextoTurnos()
    {   
        textoTurnos.text = $"Turno: {TurnManager.Instancia.TurnoActual.ToString()}";
    }

    public void AbrirMenu()
    {
        if (menuActivo) return;
        Debug.Log("Abriendo menu: " + menuPanel.name);
        menuPanel.SetActive(true);
        uiTerreno.gameObject.SetActive(true);
        menuActivo = true;
        opcionSeleccionada = 0;
        ActualizarSeleccionVisual();
    }

    public void CerrarMenu()
    {
        Debug.Log("Cerrando menu: " + menuPanel.name);
        uiTerreno.gameObject.SetActive(false);
        menuPanel.SetActive(false);
        menuActivo = false;
    }

    void ActualizarSeleccionVisual()
    {
        for (int i = 0; i < opciones.Length; i++)
        {
            opciones[i].color = (i == opcionSeleccionada) ? Color.yellow : Color.white;
        }
    }

    void EjecutarOpcionSeleccionada()
    {
        string opcion = opciones[opcionSeleccionada].text;
        Debug.Log("Seleccionaste: " + opcion);

        switch (opcion)
        {
            case "Tutoriales":
                CerrarMenu();
                tutorialMenu.Abrir();
                break;
            case "Ajustes":
                CerrarMenu();
                settingsMenu.Abrir();
                break;
            case "Objetivo":
                CerrarMenu();
                objectiveMenu.Abrir();
                break;
            case "Fin":
                finPulsado = true;
                CerrarMenu();
                TurnManager.Instancia.PasarFaseJugador();
                break;
        }
         // Opcional: cierra tras seleccionar
    }
}
