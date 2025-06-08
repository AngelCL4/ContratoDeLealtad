using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntercambioPanelUI : MonoBehaviour
{
    public Image iconoUnidad1;
    public Image iconoUnidad2;
    public TextMeshProUGUI nombre1, nombre2, cantidad1, cantidad2;

    private System.Action onCancelar;
    private System.Action onConfirmar;

    public void Mostrar(Objeto o1, Objeto o2, System.Action cancelar, System.Action confirmar)
    {
        gameObject.SetActive(true);
        onCancelar = cancelar;
        onConfirmar = confirmar;

        if (o1 != null)
        {
            iconoUnidad1.sprite = o1.icono;
            nombre1.text = o1.nombre;
            cantidad1.text = o1.cantidad.ToString() ?? "";
        }
        else
        {
            iconoUnidad1.sprite = null;
            nombre1.text = "";
            cantidad1.text = "";
        }

        if (o2 != null)
        {
            iconoUnidad2.sprite = o2.icono;
            nombre2.text = o2.nombre;
            cantidad2.text = o2.cantidad.ToString() ?? "";
        }
        else
        {
            iconoUnidad2.sprite = null;
            nombre2.text = "";
            cantidad2.text = "";
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            onCancelar?.Invoke();
            gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            onConfirmar?.Invoke();
            gameObject.SetActive(false);
        }
    }

    public void Cerrar()
    {
        gameObject.SetActive(false);
    }
}
