using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectPanelUI : MonoBehaviour
{
    public Image icono;
    public TextMeshProUGUI nombre;
    public TextMeshProUGUI cantidad;
    public TextMeshProUGUI descripcion;
    public UnitLoader unidadActual;

    private Objeto objetoActual;
    private System.Action onCerrar;
    private System.Action<Objeto> onUsar;

    public void Mostrar(UnitLoader unidad, Objeto objeto, System.Action onCerrarCallback, System.Action<Objeto> onUsarCallback)
    {
        gameObject.SetActive(true);
        objetoActual = objeto;
        onCerrar = onCerrarCallback;
        onUsar = onUsarCallback;
        unidadActual = unidad;

        icono.sprite = objeto.icono;
        nombre.text = objeto.nombre;
        if (objeto.cantidad == 0)
        {
            cantidad.gameObject.SetActive(false);
        }
        else 
        {
            cantidad.gameObject.SetActive(true);
        }
        cantidad.text = objeto.cantidad.ToString();
        descripcion.text = objeto.descripcion;
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            Cerrar();
        }

        if (Input.GetKeyDown(KeyCode.A) && objetoActual.uso == TipoUso.Activo)
        {
            UsarObjeto(objetoActual, unidadActual);
            onUsar?.Invoke(objetoActual);
            Cerrar();
        }
    }

    public void UsarObjeto(Objeto obj, UnitLoader unidad)
    {
        if (obj.uso == TipoUso.Activo && obj.cantidad > 0)
        {
            switch (obj.tipo)
            {
                case "Curativo":
                    unidad.Curar(obj.valor);
                    break;
                case "Potenciador":
                    if (obj.duracion == 0)
                    {
                        unidad.PotenciarPermanentemente(obj.statAfectada, obj.valor);
                    }
                    else
                    {
                        unidad.Potenciar(obj.statAfectada, obj.valor, obj.duracion, obj.decrementoPorTurno);
                    }
                    break;
            }
            obj.cantidad--;
            if (obj.cantidad <= 0)
            {
                unidad.datos.objeto = null;
            }
        }
    }

    private void Cerrar()
    {
        gameObject.SetActive(false);
        onCerrar?.Invoke();
    }
}
