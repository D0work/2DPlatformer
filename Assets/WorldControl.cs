using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldControl : MonoBehaviour
{
    public Tilemap lavaTilemap;
    public TileBase lavaTile;
    public Vector3Int startPos;
    public float delay = 0.2f;

    public int width = 100;
    public int height = 20;

    private void Start()
    {
        StartCoroutine(FillLava());
    }

    private System.Collections.IEnumerator FillLava()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(startPos.x + x, startPos.y + y, 0);
                lavaTilemap.SetTile(pos, lavaTile);
            }
            yield return new WaitForSeconds(delay);
        }
    }
}