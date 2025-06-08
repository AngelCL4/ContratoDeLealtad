using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnidadInfoUI : MonoBehaviour
{
    public Image retrato;
    public TextMeshProUGUI nombre;
    public TextMeshProUGUI clase;
    public TextMeshProUGUI nivel;
    public TextMeshProUGUI experiencia;
    public Image objetoIcono;
    public TextMeshProUGUI objetoNombre;
    public TextMeshProUGUI pv;
    public TextMeshProUGUI MaxPV;
    public TextMeshProUGUI poder;
    public TextMeshProUGUI habilidad;
    public TextMeshProUGUI velocidad;
    public TextMeshProUGUI suerte;
    public TextMeshProUGUI defensa;
    public TextMeshProUGUI resistencia;
    public TextMeshProUGUI movimiento;

    public void MostrarDatos(Unidad unidad)
    {
        gameObject.SetActive(true);
        if (unidad.estado == "Enemigo"){
            retrato.sprite = Resources.Load<Sprite>($"Portraits/{unidad.clase.nombre.Replace(" ", "")}");
        }
        else if (unidad.estado == "Jefe"){
            retrato.sprite = Resources.Load<Sprite>($"Portraits/{unidad.nombre}");
        }
        else retrato.sprite = Resources.Load<Sprite>($"Portraits/{unidad.nombre}Neutral");
        nombre.text = unidad.nombre;
        clase.text = unidad.clase.nombre;
        nivel.text = unidad.nivel.ToString();
        experiencia.text = unidad.experiencia.ToString();
        if (unidad.objeto == null || string.IsNullOrEmpty(unidad.objeto.nombre))
        {
            objetoNombre.text = "Nada";
            objetoIcono.gameObject.SetActive(false);
        }
        else
        {
            objetoNombre.text = unidad.objeto.nombre;
            objetoIcono.sprite = unidad.objeto.icono;
            objetoIcono.gameObject.SetActive(true);
        }

        pv.text = unidad.PV.ToString();
        MaxPV.text= unidad.MaxPV.ToString();
        poder.text = unidad.poder.ToString();
        habilidad.text = unidad.habilidad.ToString();
        velocidad.text = unidad.velocidad.ToString();
        suerte.text = unidad.suerte.ToString();
        defensa.text = unidad.defensa.ToString();
        resistencia.text = unidad.resistencia.ToString();
        movimiento.text = unidad.movimiento.ToString();
    }

    public void Ocultar()
    {
        gameObject.SetActive(false);
    }

}
