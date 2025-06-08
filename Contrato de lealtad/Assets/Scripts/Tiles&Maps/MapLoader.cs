using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class MapLoader : MonoBehaviour
{
    public Tilemap tilemap;
    public TerrainLibrary terrainLibrary; // ScriptableObject que contiene la lista de terrenos
    public PointerController pointerController; // Referencia al PointerController
    public CameraController cameraController; // Referencia al CameraController
    public MapData currentMapData { get; private set; }

    public void LoadMapFromJson(string mapName)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Maps/{mapName}");
        if (jsonAsset == null)
        {
            Debug.LogError($"No se encontró el archivo JSON: {mapName}");
            return;
        }

        currentMapData = JsonUtility.FromJson<MapData>(jsonAsset.text);
        tilemap.ClearAllTiles();

        foreach (var cell in currentMapData.cells)
        {
            var terrainType = terrainLibrary.GetTerrainByName(cell.terrain);
            if (terrainType != null)
            {
                // Buscamos un TerrainTile que tenga el mismo TerrainType (podrías tener un diccionario si quieres optimizar)
                TerrainTile terrainTile = terrainLibrary.GetTileForTerrain(terrainType);
                int correctedX = cell.x;
                int correctedY = currentMapData.mapHeight - cell.y - 1; // Invertir el eje Y

                tilemap.SetTile(new Vector3Int(correctedX, correctedY, 0), terrainTile);
                Vector3 scale = new Vector3(cell.flipX ? -1 : 1, cell.flipY ? -1 : 1, 1);
                Quaternion rotation = Quaternion.Euler(0, 0, cell.rotation);
                Matrix4x4 transformMatrix = Matrix4x4.TRS(Vector3.zero, rotation, scale);

                tilemap.SetTransformMatrix(new Vector3Int(correctedX, correctedY, 0), transformMatrix);
            }
        }

        if (pointerController != null)
        {
            pointerController.tilemap = tilemap;
            Debug.Log("Tilemap asignado a PointerController.");
        }
        else
        {
            Debug.LogWarning("PointerController no asignado en MapLoader.");
        }

        if (cameraController != null)
        {
            cameraController.tilemap = tilemap;
            Debug.Log("Tilemap asignado a CameraController.");
        }
        else
        {
            Debug.LogWarning("CameraController no asignado en MapLoader.");
        }

        if (currentMapData.playerSpawnPositions != null && currentMapData.playerSpawnPositions.Length > 0 && pointerController != null)
        {
            Vector2 primeraPos = currentMapData.playerSpawnPositions[0];
            pointerController.transform.position = new Vector3(primeraPos.x, currentMapData.mapHeight - primeraPos.y - 1, 0);
        }

        // Aquí podrías spawnear unidades en las posiciones de playerSpawnPositions y enemySpawnPositions
        Debug.Log($"Mapa {mapName} cargado correctamente");
    }
}
