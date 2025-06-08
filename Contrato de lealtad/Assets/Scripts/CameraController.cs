using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    public Transform pointer; // Objeto que sigue la cámara
    public Tilemap tilemap;
    public Camera cam;
    private Vector3 minBounds, maxBounds;
    private float camHeight, camWidth;

    private void Start()
    {
        if (tilemap == null || cam == null)
        {
            Debug.LogError("Tilemap o Cámara no asignados.");
            return;
        }

        // Obtener límites del Tilemap
        BoundsInt bounds = tilemap.cellBounds;
        minBounds = tilemap.CellToWorld(bounds.min);
        maxBounds = tilemap.CellToWorld(bounds.max);

        // Calcular tamaño de la cámara
        camHeight = cam.orthographicSize * 2;
        camWidth = camHeight * cam.aspect;

        // Ajustar tamaño de la cámara si el Tilemap es menor
        float mapWidth = (maxBounds.x - minBounds.x);
        float mapHeight = (maxBounds.y - minBounds.y);

        // Ajustamos la cámara para que vea el mapa completo en ambos ejes
        if (mapWidth < camWidth)
            cam.orthographicSize = mapWidth / (2 * cam.aspect);

        // Aquí aseguramos que la cámara vea todas las 9 casillas en altura
        if (mapHeight < camHeight)
            cam.orthographicSize = mapHeight / 2;

        // Ajustamos también la cámara para que vea el mapa de manera correcta, en base al ancho y la altura máxima
        float maxTileHeight = Mathf.Max(mapWidth, mapHeight);
        if (maxTileHeight < camHeight)
        {
            cam.orthographicSize = Mathf.Max(mapWidth, mapHeight) / 2;
        }
    }

    private void LateUpdate()
    {
        if (pointer == null) return;

        // Seguir al puntero
        Vector3 newPos = pointer.position;
        newPos.z = cam.transform.position.z;

        // Restringir dentro de los límites del Tilemap
        float halfWidth = cam.orthographicSize * cam.aspect;
        float halfHeight = cam.orthographicSize;

        newPos.x = Mathf.Clamp(newPos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        cam.transform.position = newPos;
    }
}