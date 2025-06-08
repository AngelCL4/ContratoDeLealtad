using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public class UnitController : MonoBehaviour
{
    [Header("Datos de la unidad")]
    public Unit unitData;

    private UnitInstance unitInstance;

    private void Start()
    {
        if (unitData != null)
        {
            unitInstance = new UnitInstance(unitData);
            Debug.Log($"Unidad {unitData.unitName} creada en el mapa. Nivel {unitInstance.currentLevel}");
        }
        else
        {
            Debug.LogError("UnitData no asignado en el UnitController.");
        }
    }

    public void MostrarStats()
    {
        Debug.Log($"--- Stats de {unitData.unitName} ---\n" +
                  $"Nivel: {unitInstance.currentLevel}\n" +
                  $"HP: {unitInstance.currentHP}/{unitInstance.maxHP}\n" +
                  $"Poder: {unitInstance.power}\n" +
                  $"Habilidad: {unitInstance.skill}\n" +
                  $"Velocidad: {unitInstance.speed}\n" +
                  $"Defensa: {unitInstance.defense}\n" +
                  $"Resistencia: {unitInstance.resistance}\n" +
                  $"Suerte: {unitInstance.luck}\n" +
                  $"Exp: {unitInstance.currentExp}/100");
    }

    public void GanarExperiencia(int cantidad)
    {
        unitInstance.GainExp(cantidad);
        Debug.Log($"{unitData.unitName} ganó {cantidad} exp.");
        MostrarStats();
    }

    public void RecibirGolpe(int cantidad)
    {
        unitInstance.currentHP -= cantidad;
        if (unitInstance.currentHP < 0) unitInstance.currentHP = 0;
        Debug.Log($"{unitData.unitName} recibió {cantidad} de daño. HP restante: {unitInstance.currentHP}/{unitInstance.maxHP}");
    }
}
*/