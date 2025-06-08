using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class AlmacenObjetos : MonoBehaviour
{
    public static AlmacenObjetos Instance { get; private set; }
    public List<Objeto> objetosAlmacenados = new();

    private string almacenSavePath = "Assets/Resources/objetosAlmacenados.json"; // Ruta donde se guardarán los objetos.

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void AñadirObjeto(Objeto obj)
    {
        if (!objetosAlmacenados.Contains(obj))
            objetosAlmacenados.Add(obj);
    }

    public void EliminarObjeto(Objeto obj)
    {
        if (objetosAlmacenados.Contains(obj))
            objetosAlmacenados.Remove(obj);
    }

    public List<Objeto> ObtenerObjetos()
    {
        return objetosAlmacenados;
    }

    public void GuardarObjetos()
    {
        string objetosJson = JsonConvert.SerializeObject(objetosAlmacenados, Formatting.Indented);
        File.WriteAllText(almacenSavePath, objetosJson);
        Debug.Log("Objetos almacenados guardados.");
    }

    // Método para cargar los objetos almacenados
    public void CargarObjetos()
    {
        if (File.Exists(almacenSavePath))
        {
            string objetosJson = File.ReadAllText(almacenSavePath);
            objetosAlmacenados = JsonConvert.DeserializeObject<List<Objeto>>(objetosJson);
            Debug.Log("Objetos almacenados cargados.");
        }
        else
        {
            Debug.Log("No se encontró el archivo de objetos almacenados.");
        }
    }
}
