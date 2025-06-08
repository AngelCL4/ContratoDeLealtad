using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public MapLoader mapLoader;

    void Start()
    {
        mapLoader.LoadMapFromJson("map3");
    }
}