using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsSaver : MonoBehaviour
{
    public void GuardarOpacidadCuadricula(float valor)
    {
        PlayerPrefs.SetFloat("OpacidadCuadricula", valor);
    }
}