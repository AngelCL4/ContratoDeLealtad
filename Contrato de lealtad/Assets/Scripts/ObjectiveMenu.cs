using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class ObjectiveMenu : MonoBehaviour
{
    public Tilemap escapeTilemap;  // Referencia al Tilemap de escape
    public Tile yellowTile;        // Referencia al tile amarillo
    public TextMeshProUGUI victoryDetailsText;
    public TextMeshProUGUI defeatDetailsText;
    public ObjectiveData data;

    // Referencia al PanelLimiteTurnos y LimiteText
    public GameObject PanelLimiteTurnos;  // Panel de límite de turnos
    public TextMeshProUGUI LimiteText;    // Texto del límite de turnos

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => !string.IsNullOrEmpty(ChapterManager.instance.currentChapter));

        string chapter = ChapterManager.instance.currentChapter;
        TextAsset json = Resources.Load<TextAsset>($"Data/{chapter}Objective");

        if (json == null)
        {
            Debug.LogError($"No se encontró el archivo de objetivos para el capítulo: {chapter}");
            yield break;
        }

        data = JsonUtility.FromJson<ObjectiveData>(json.text);

        // Verificar si el objetivo es "Escapar"
        if (data.victoryCondition == "Escapar")
        {
            // Crear la casilla amarilla en el tilemap en las coordenadas x, y dadas
            Vector3 escapePosition = new Vector3(data.x, data.y, 0); // Usar Vector3 para coordenadas float
            Vector3Int gridPosition = escapeTilemap.WorldToCell(escapePosition); // Convertir a coordenadas de celda

            escapeTilemap.SetTile(gridPosition, yellowTile);
        }

        // Si el objetivo es "Sobrevivir", activar el panel y actualizar el texto
        if (data.victoryCondition == "Sobrevivir" && !ChapterManager.instance.chapterCompleted)
        {
            PanelLimiteTurnos.SetActive(true);  // Mostrar el panel

            // Actualizar el texto del panel con el turno actual y el objetivo
            if (TurnManager.Instancia != null)
            {
                LimiteText.text = $"{TurnManager.Instancia.TurnoActual} / {data.turnos}";
            }
        }
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.S))
        {
            gameObject.SetActive(false);
        }
        if (ChapterManager.instance.chapterCompleted)
        {
            PanelLimiteTurnos.SetActive(false);
        }
    }

    public void ActualizarTextoLimiteTurnos()
    {
        if (TurnManager.Instancia.TurnoActual <= data.turnos)
        {
            LimiteText.text = $"{TurnManager.Instancia.TurnoActual} / {data.turnos}";
        }
        else 
        {
            PanelLimiteTurnos.SetActive(false);
        }
    }

    public void Abrir()
    {
        gameObject.SetActive(true);
        
        victoryDetailsText.text = data.victoryDetails;
        defeatDetailsText.text = data.defeatDetails;        
    }
}

[System.Serializable]
public class ObjectiveData
{
    public string victoryCondition;
    public string victoryDetails;
    public string defeatDetails;

    public int turnos; // Solo para "Sobrevivir"
    public float x;      // Solo para "Escapar"
    public float y;      // Solo para "Escapar"
}
