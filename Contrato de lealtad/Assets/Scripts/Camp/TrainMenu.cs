using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class TrainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject personajeListContainer;
    [SerializeField] private GameObject personajePrefab; // Con retrato, clase, nivel, etc.
    [SerializeField] private TextMeshProUGUI textoEntrenamientos; // Vincula en el inspector

    private List<Unidad> personajesReclutados = new();
    private List<GameObject> botonesInstanciados = new();
    private int currentSelectionIndex = 0;
    private CampManager campManager;
    private const int visibleCount = 10;
    private int visibleStartIndex = 0;

    private void OnEnable()
    {
        campManager = FindObjectOfType<CampManager>();
        personajesReclutados = SupportManager.Instance.GetPersonajesReclutados(GameManager.Instance.datosJuego.unidades.ToList());
        MostrarListaPersonajes();
        UpdateSelectionVisual();
        textoEntrenamientos.text = $"Entrenamientos disponibles: {campManager.entrenamientos}";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelectionIndex++;
            if (currentSelectionIndex >= visibleCount)
            {
                if (visibleStartIndex + visibleCount < personajesReclutados.Count)
                {
                    visibleStartIndex += visibleCount;
                    currentSelectionIndex = 0;
                    MostrarListaPersonajes();
                }
                else
                {
                    currentSelectionIndex = visibleCount - 1;
                }
            }
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelectionIndex--;
            if (currentSelectionIndex < 0)
            {
                if (visibleStartIndex > 0)
                {
                    visibleStartIndex -= visibleCount;
                    currentSelectionIndex = visibleCount - 1;
                    MostrarListaPersonajes();
                }
                else
                {
                    currentSelectionIndex = 0;
                }
            }
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            EntrenarPersonaje(visibleStartIndex + currentSelectionIndex);
            MostrarListaPersonajes(); // refrescar datos tras entrenar
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            gameObject.SetActive(false); // cerrar menú
            GameObject.Find("CampManager").GetComponent<CampManager>().isMenuActive = true;
        }
    }

    private void MostrarListaPersonajes()
    {
        foreach (Transform child in personajeListContainer.transform)
            Destroy(child.gameObject);

        botonesInstanciados.Clear();

        int total = personajesReclutados.Count;
        int end = Mathf.Min(visibleStartIndex + visibleCount, total);

        for (int i = visibleStartIndex; i < end; i++)
        {
            Unidad unidad = personajesReclutados[i];
            GameObject item = Instantiate(personajePrefab, personajeListContainer.transform);
            botonesInstanciados.Add(item);   

            // Asignar retrato
            Image portraitImage = item.GetComponent<Image>();
            var retrato = Resources.Load<Sprite>($"Portraits/{unidad.nombre}Neutral");
            if (retrato != null)
                portraitImage.sprite = retrato;
            else
                Debug.LogWarning($"No se encontró retrato para {unidad.nombre}Neutral");

            // Nombre
            TextMeshProUGUI nameText = item.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = unidad.nombre;

            // Clase sprite
            Image classImage = item.transform.Find("ClassIcon")?.GetComponent<Image>();
            var claseSprite = Resources.Load<Sprite>($"Sprites/{unidad.clase.nombre.Replace(" ", "")}{unidad.nombre}"); // o según tu estructura
            if (classImage != null && claseSprite != null) 
                classImage.sprite = claseSprite;
            else 
                Debug.LogWarning($"No se encontró icono para {unidad.clase.nombre}{unidad.nombre}");

            // Clase nombre
            TextMeshProUGUI classText = item.transform.Find("ClassName")?.GetComponent<TextMeshProUGUI>();
            if (classText != null)
                classText.text = unidad.clase.nombre;

            // Nivel y experiencia
            item.transform.Find("Level")?.GetComponent<TextMeshProUGUI>().SetText($"Nivel: {unidad.nivel}");
            item.transform.Find("EXP")?.GetComponent<TextMeshProUGUI>().SetText($"Exp: {unidad.experiencia}");

        }
    }

    private void UpdateSelectionVisual()
    {
        for (int i = 0; i < botonesInstanciados.Count; i++)
        {
            var img = botonesInstanciados[i].GetComponent<Image>();
            img.color = (i == currentSelectionIndex) ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private void EntrenarPersonaje(int index)
    {
        if (campManager.entrenamientos >= 1)
        {
            var unidad = personajesReclutados[index];
            unidad.experiencia += 100;
            if (unidad.experiencia >= 100)
            {
                unidad.experiencia -= 100;
                var tempGameObject = new GameObject("TempUnitLoader");
                var tempUnitLoader = tempGameObject.AddComponent<UnitLoader>();
                tempUnitLoader.ConfigurarUnidad(unidad, true);
                tempUnitLoader.SubirNivel(unidad);
                tempUnitLoader.datos.PV = tempUnitLoader.datos.MaxPV;
                Destroy(tempGameObject);
                GameManager.Instance.chapterDataJuego.entrenar--;
                campManager.entrenamientos = GameManager.Instance.chapterDataJuego.entrenar;
                textoEntrenamientos.text = $"Entrenamientos disponibles: {campManager.entrenamientos}";
                Debug.Log($"{unidad.nombre} subió a nivel {unidad.nivel}!");             
            }
        }       
    }
}
