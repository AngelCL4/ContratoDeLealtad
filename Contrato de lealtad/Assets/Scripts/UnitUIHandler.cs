using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitUIHandler : MonoBehaviour
{
    public Image barraVerde;
    public Image barraRoja;
    public Image barraAmarilla;
    private Unidad unidad;

    public void Configurar(Unidad unidad)
    {
        this.unidad = unidad;
        ActualizarPV();
    }

    public void ActualizarPV()
    {
        if (unidad != null && barraVerde != null)
        {
            float porcentaje = (float)unidad.PV / unidad.MaxPV;
            Debug.Log(porcentaje);
            barraVerde.fillAmount = porcentaje;
        }
    }

    public void MostrarDanioProyectado(int daño)
    {
        if (unidad == null || barraAmarilla == null || barraVerde == null) return;

        int pvRestante = Mathf.Max(0, unidad.PV - daño);
        float porcentajeActual = (float)unidad.PV / unidad.MaxPV;
        float porcentajeRestante = (float)pvRestante / unidad.MaxPV;
        float porcentajeDanio = porcentajeActual - porcentajeRestante;

        // Mostrar solo la parte del daño previsto
        barraAmarilla.fillAmount = porcentajeDanio;

        // Asegurar que el pivot esté en el lado izquierdo (0)
        barraAmarilla.rectTransform.pivot = new Vector2(0f, 0.5f);

        // Calcular la posición X relativa desde el centro
        float anchoTotal = barraVerde.rectTransform.rect.width;
        float offsetX = (porcentajeRestante - 0.5f) * anchoTotal;

        // Posicionar la barra amarilla correctamente alineada
        barraAmarilla.rectTransform.anchoredPosition = new Vector2(offsetX, 0f);
    }

    public void LimpiarDanioProyectado()
    {
        if (barraAmarilla != null)
        {
            barraAmarilla.fillAmount = 0f;
        }
    }
}
