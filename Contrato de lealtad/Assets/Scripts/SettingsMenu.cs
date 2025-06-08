using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private List<OptionMenu> opciones;
    private int indiceActual = 0;
    private bool enModoEdicion = false;
    [SerializeField] private Slider cuadriculaSlider;
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderEfectos;
    [SerializeField] private GameObject mainMenuControllerObject;

    private void Start()
    {
        gameObject.SetActive(false);
        float opacidad = PlayerPrefs.GetFloat("OpacidadCuadricula", 0f);
        cuadriculaSlider.value = opacidad;
        sliderMusica.value = PlayerPrefs.GetFloat("VolumenMusica", 0.5f);
        sliderMusica.value = PlayerPrefs.GetFloat("VolumenMusica", 0.5f);
        sliderEfectos.value = PlayerPrefs.GetFloat("VolumenEfectos", 0.5f);
        if (!PlayerPrefs.HasKey("VelocidadTexto"))
        {
            PlayerPrefs.SetString("VelocidadTexto", "Normal");
        }
        if (!PlayerPrefs.HasKey("FinalTurno"))
        {
            PlayerPrefs.SetString("FinalTurno", "Automatico");
        }
    }

    public void Abrir()
    {
        gameObject.SetActive(true);
        ActualizarSeleccion();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (enModoEdicion)
            {
                enModoEdicion = false;
                Debug.Log("Salir de modo edición");
            }
            else
            {
                CerrarMenu();
            }
        }

        if (!enModoEdicion)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) CambiarOpcion(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) CambiarOpcion(1);
            if (Input.GetKeyDown(KeyCode.A)) ActivarOpcion();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) opciones[indiceActual].CambiarValor(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) opciones[indiceActual].CambiarValor(1);
            if (Input.GetKeyDown(KeyCode.A)) ConfirmarOpcion();
        }
    }

    private void CambiarOpcion(int dir)
    {
        opciones[indiceActual].Seleccionar(false);
        indiceActual = (indiceActual + dir + opciones.Count) % opciones.Count;
        ActualizarSeleccion();
    }

    private void ActualizarSeleccion()
    {
        opciones[indiceActual].Seleccionar(true);
    }

    private void ActivarOpcion()
    {
        var actual = opciones[indiceActual];

        if (actual.tipo == TipoOpcion.Boton)
        {
            actual.Confirmar(); // Banda Sonora, abrir submenú
        }
        else
        {
            enModoEdicion = true;
            Debug.Log("Entrar en modo edición");
        }
    }

    private void ConfirmarOpcion()
    {
        var actual = opciones[indiceActual];
        actual.Confirmar();
        enModoEdicion = false;
    }

    private void CerrarMenu()
    {
        gameObject.SetActive(false);
        if (mainMenuControllerObject != null)
        {
            mainMenuControllerObject.GetComponent<MainMenuController>().enabled = true;
        }
        GameObject campManagerObj = GameObject.Find("CampManager");
        
        if (campManagerObj != null)
        {
            CampManager campManager = campManagerObj.GetComponent<CampManager>();
            if (campManager != null)
            {
                campManager.isMenuActive = true;
            }
        }

        Debug.Log("Cerrar menú de ajustes");
    }
}
