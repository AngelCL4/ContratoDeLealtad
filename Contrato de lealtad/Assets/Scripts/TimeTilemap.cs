using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TimeTilemap : MonoBehaviour
{
    [SerializeField] public Tilemap timeTilemap;
    [SerializeField] private Tilemap mapaPrincipalTilemap;  // Tilemap del nivel real
    [SerializeField] private TileBase eveningTile;
    [SerializeField] private TileBase nightTile;

    public void GenerarCuadriculaTarde()
    {
        if (timeTilemap == null || mapaPrincipalTilemap == null || eveningTile == null)
        {
            Debug.LogWarning("Faltan referencias para generar la cuadrícula.");
            return;
        }

        timeTilemap.ClearAllTiles();

        BoundsInt bounds = mapaPrincipalTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int originalPos = new Vector3Int(x, y, 0);

                if (mapaPrincipalTilemap.HasTile(originalPos))
                {
                    timeTilemap.SetTile(originalPos, eveningTile);
                }
            }
        }

        Debug.Log("Cuadrícula generada sin reflejo.");
    }

    public void GenerarCuadriculaNoche()
    {
        if (timeTilemap == null || mapaPrincipalTilemap == null || nightTile == null)
        {
            Debug.LogWarning("Faltan referencias para generar la cuadrícula.");
            return;
        }

        timeTilemap.ClearAllTiles();

        BoundsInt bounds = mapaPrincipalTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int originalPos = new Vector3Int(x, y, 0);

                if (mapaPrincipalTilemap.HasTile(originalPos))
                {
                    timeTilemap.SetTile(originalPos, nightTile);
                }
            }
        }

        Debug.Log("Cuadrícula generada sin reflejo.");
    }
}
