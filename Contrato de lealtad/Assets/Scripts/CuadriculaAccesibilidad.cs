using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CuadriculaAccesibilidad : MonoBehaviour
{
    [SerializeField] private Tilemap gridTilemap;
    [SerializeField] private Tilemap mapaPrincipalTilemap;  // Tilemap del nivel real
    [SerializeField] private TileBase bordeTile;

    public float opacidadGuardada;

    private void Start()
    {
        float opacidadGuardada = PlayerPrefs.GetFloat("OpacidadCuadricula", 0f);
        GenerarCuadricula();
        ActualizarOpacidad(opacidadGuardada);
    }

    public void GenerarCuadricula()
    {
        if (gridTilemap == null || mapaPrincipalTilemap == null || bordeTile == null)
        {
            Debug.LogWarning("Faltan referencias para generar la cuadrícula.");
            return;
        }

        gridTilemap.ClearAllTiles();

        BoundsInt bounds = mapaPrincipalTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int originalPos = new Vector3Int(x, y, 0);

                if (mapaPrincipalTilemap.HasTile(originalPos))
                {
                    gridTilemap.SetTile(originalPos, bordeTile);
                }
            }
        }

        Debug.Log("Cuadrícula generada sin reflejo.");
    }

    public void ActualizarOpacidad(float opacidad)
    {
        PlayerPrefs.SetFloat("OpacidadCuadricula", opacidad);

        Color nuevoColor = new Color(1f, 1f, 1f, opacidad);

        foreach (var pos in gridTilemap.cellBounds.allPositionsWithin)
        {
            if (gridTilemap.HasTile(pos))
            {
                gridTilemap.SetColor(pos, nuevoColor);
            }
        }
    }
}
