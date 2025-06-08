using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CargadorDeUnidades : MonoBehaviour
{
    public DatosJuego datosJuego;

    void Start()
    {
        // Cargar las unidades desde el archivo JSON
        CargarDatosJSON();
    }

    void CargarDatosJSON()
    {
        // Ruta del archivo JSON dentro de la carpeta Resources
        string path = "unitsData"; // Se asume que el archivo se llama "unidades.json" y está en la carpeta Resources
        TextAsset jsonText = Resources.Load<TextAsset>(path);

        if (jsonText != null)
        {
            // Deserializamos el JSON a la clase DatosJuego
            datosJuego = JsonUtility.FromJson<DatosJuego>(jsonText.ToString());
            Debug.Log(jsonText.ToString());
        }
        else
        {
            Debug.LogError("No se encontró el archivo JSON.");
        }
    }
}
