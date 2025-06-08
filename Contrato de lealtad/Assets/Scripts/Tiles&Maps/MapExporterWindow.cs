using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class MapExporterWindow : EditorWindow
{
    private Tilemap tilemap;
    private string exportFileName = "map.json";
    private string playerSpawnsText = "";
    private string enemySpawnsText = "";

    [MenuItem("MapExporter/Export Map To JSON")]
    public static void ShowWindow()
    {
        GetWindow<MapExporterWindow>("Map Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Map to JSON", EditorStyles.boldLabel);

        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        exportFileName = EditorGUILayout.TextField("File Name", exportFileName);

        GUILayout.Space(10);
        GUILayout.Label("Player Spawn Positions (x y por línea):");
        playerSpawnsText = EditorGUILayout.TextArea(playerSpawnsText, GUILayout.Height(60));

        GUILayout.Label("Enemy Spawn Positions (x y por línea):");
        enemySpawnsText = EditorGUILayout.TextArea(enemySpawnsText, GUILayout.Height(60));

        if (GUILayout.Button("Export JSON"))
        {
            ExportMap();
        }
    }

    private void ExportMap()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap not assigned.");
            return;
        }

        var bounds = tilemap.cellBounds;
        var cells = new System.Collections.Generic.List<MapCell>();

        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile<TerrainTile>(pos);
            if (tile != null && tile.terrainType != null)
            {
                var matrix = tilemap.GetTransformMatrix(pos);

                // Determina si hay flip en X o Y usando la escala
                Vector3 lossyScale = matrix.lossyScale;
                bool flipX = Mathf.Sign(lossyScale.x) < 0;
                bool flipY = Mathf.Sign(lossyScale.y) < 0;

                // Rotación en Z (solo 2D)
                float angle = -Mathf.Atan2(matrix.m01, matrix.m00) * Mathf.Rad2Deg;
                angle = (angle + 360) % 360; // convierte negativos a positivos
                angle = Mathf.Round(angle); // Puedes normalizar si solo usas 0/90/180/270

                cells.Add(new MapCell
                {
                    x = pos.x - bounds.xMin,
                    y = bounds.yMax - pos.y - 1,
                    terrain = tile.terrainType.terrainName,
                    rotation = angle,
                    flipX = flipX,
                    flipY = flipY
                });
            }
        }

        Vector2[] playerSpawns = ParsePositions(playerSpawnsText);
        Vector2[] enemySpawns = ParsePositions(enemySpawnsText);

        MapData mapData = new MapData
        {
            mapWidth = bounds.size.x,
            mapHeight = bounds.size.y,
            cells = cells.ToArray(),
            playerSpawnPositions = playerSpawns,
            enemySpawnPositions = enemySpawns
        };

        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(Path.Combine(Application.dataPath, exportFileName), json);
        AssetDatabase.Refresh();
        Debug.Log($"Map exported to {exportFileName}");
    }

    private Vector2[] ParsePositions(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new Vector2[0];

        var lines = input.Split('\n');
        var positions = new System.Collections.Generic.List<Vector2>();

        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                positions.Add(new Vector2(x, y));
            }
        }

        return positions.ToArray();
    }

    [System.Serializable]
    public class MapCell
    {
        public int x;
        public int y;
        public string terrain;

        public float rotation; // en grados
        public bool flipX;
        public bool flipY;
    }

    [System.Serializable]
    public class MapData
    {
        public int mapWidth;
        public int mapHeight;
        public MapCell[] cells;
        public Vector2[] playerSpawnPositions;
        public Vector2[] enemySpawnPositions;
    }
}