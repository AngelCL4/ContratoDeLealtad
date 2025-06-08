using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialMenu : MonoBehaviour
{
    public GameObject panel;
    public TutorialDatabase database;
    public RectTransform listaContainer;
    public GameObject itemTemplate;
    public TextMeshProUGUI descripcionTexto;
    
    private List<TutorialItem> tutorialesVisibles;
    private int indiceSeleccionado = 0;
    private TextMeshProUGUI[] textos;

    private bool activo = false;
    public bool Activo => activo;

    void Start()
    {
        panel.SetActive(false);
        GenerarLista();
    }

    void Update()
    {
        if (!activo) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            indiceSeleccionado = (indiceSeleccionado - 1 + textos.Length) % textos.Length;
            ActualizarVisual();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            indiceSeleccionado = (indiceSeleccionado + 1) % textos.Length;
            ActualizarVisual();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Cerrar();
        }
    }

    public void Abrir()
    {
        panel.SetActive(true);
        activo = true;
        indiceSeleccionado = 0;
        ActualizarVisual();
    }

    public void Cerrar()
    {
        panel.SetActive(false);
        GameObject campManagerObj = GameObject.Find("CampManager");
        if (campManagerObj != null)
        {
            CampManager campManager = campManagerObj.GetComponent<CampManager>();
            if (campManager != null)
            {
                campManager.isMenuActive = true;
            }
        }
        activo = false;
    }

    void GenerarLista()
    {
        var visibles = new List<TutorialItem>();
        foreach (var t in database.tutoriales)
        {
            if (GestorTutoriales.instancia.EstaDesbloqueado(t.id))
                visibles.Add(t);
        }

        textos = new TextMeshProUGUI[visibles.Count];
        for (int i = 0; i < visibles.Count; i++)
        {
            GameObject item = Instantiate(itemTemplate, listaContainer);
            item.SetActive(true);
            TextMeshProUGUI txt = item.GetComponent<TextMeshProUGUI>();
            txt.text = visibles[i].titulo;
            textos[i] = txt;
        }

        tutorialesVisibles = visibles; // Guárdalos para luego mostrar la descripción correcta
    }

    void ActualizarVisual()
    {
        for (int i = 0; i < textos.Length; i++)
        {
            textos[i].color = (i == indiceSeleccionado) ? Color.yellow : Color.white;
        }

        descripcionTexto.text = tutorialesVisibles[indiceSeleccionado].descripcion;
    }
}
