using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnidadVisual : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public void AsignarDatos(Unidad unidad)
    {
        string rutaSprite = $"Sprites/{unidad.clase.nombre}{unidad.nombre}";
        Sprite spriteClasePersonalizado = Resources.Load<Sprite>(rutaSprite);

        if (spriteClasePersonalizado != null)
        {
            spriteRenderer.sprite = spriteClasePersonalizado;
            return;
        }
        else
        {
            Debug.LogWarning($"No se encontró sprite para {rutaSprite}, revisa la carpeta o el nombre.");
        }
    }
}
