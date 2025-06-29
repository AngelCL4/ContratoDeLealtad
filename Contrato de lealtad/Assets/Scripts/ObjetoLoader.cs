using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjetoLoader : MonoBehaviour
{
    public static List<Objeto> objetosDisponibles;
    public DatosObjetos datos;

    void Awake()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("objetos");
        if (jsonData != null)
        {
            DatosObjetos datos = JsonUtility.FromJson<DatosObjetos>(jsonData.text);
            objetosDisponibles = new List<Objeto>(datos.objetos);
            Debug.Log($"Objetos cargados: {objetosDisponibles.Count}");
        }

        // Cargar sprites
        foreach (var obj in objetosDisponibles)
        {
            obj.icono = Resources.Load<Sprite>(obj.spritePath);
        }
    }
}