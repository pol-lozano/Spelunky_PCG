// Pol Lozano Llorens
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class Room
{
    public int Id { get; set; }
    public int Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    //Store tile world positions
    public Vector3Int[,] tiles = new Vector3Int[10, 8];

    public Room(int id, int x, int y)
    {
        X = x;
        Y = y;
        Id = id;
        Type = 0;
    }

    //Get world pos of center of the room
    public Vector3 Center()
    {
        return tiles[tiles.GetUpperBound(0) / 2 + 1, tiles.GetUpperBound(1) / 2 + 1];
    }
}
