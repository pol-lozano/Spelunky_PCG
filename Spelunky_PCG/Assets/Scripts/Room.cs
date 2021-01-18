// Pol Lozano Llorens
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Code.Utils;

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

    public Vector3Int PlaceDoor(Tilemap source, Tilemap target, TileBase tile)
    {
        List<Vector3Int> availablePos = new List<Vector3Int>();
        foreach (Vector3Int pos in tiles)
        {
            //If there is a floor below make position available for door placement
            if (source.GetTile(pos) == null
                && source.GetTile(pos + Vector3Int.down) != null
                && source.GetTile(pos + Vector3Int.up) == null)
                availablePos.Add(pos);
        }
        Vector3Int doorPos = availablePos[Random.Range(0, availablePos.Count)];
        target.SetTile(doorPos, tile);
        return doorPos;
    }

    public void FillRoom(Tilemap tilemap, Tilemap ladderTilemap, LevelGenerator.RoomTemplate[] templates, TileBase[] _tiles, Dictionary<Color32, LevelGenerator.TileID> byColor)
    {
        //Left to right
        int offsetX = X * Config.ROOM_WIDTH;
        //Top to bottom
        int offsetY = -Y * Config.ROOM_HEIGHT;
        //Try to get template from list, and store pixels into flattened array
        Color32[] colors = templates[Type].images[Random.Range(0, templates[Type].images.Length)].GetPixels32();

        for (int y = 0; y < Config.ROOM_HEIGHT; y++)
        {
            for (int x = 0; x < Config.ROOM_WIDTH; x++)
            {
                //Get position for tile
                Vector3Int pos = new Vector3Int(x + offsetX, y + offsetY, 0);
                if (byColor.TryGetValue(colors[y * Config.ROOM_WIDTH + x], out LevelGenerator.TileID toPlace))
                {
                    //Store tiles pos in each room
                    tiles[x, y] = pos;
                    if (toPlace == LevelGenerator.TileID.EMPTY) continue; //Skip empty tiles
                    switch (toPlace)
                    {
                        case LevelGenerator.TileID.RANDOM:
                            if (Random.value <= .25f)
                            {
                                //Place tile in x and y pos with room offset
                                tilemap.SetTile(pos, _tiles[(uint)toPlace]);
                            }
                            else if (Random.value <= .25f)
                            {
                                //Place dirt tile
                                tilemap.SetTile(pos, _tiles[(uint)LevelGenerator.TileID.DIRT]);
                            }
                            break;
                        case LevelGenerator.TileID.LADDER:
                            //Place ladder
                            ladderTilemap.SetTile(pos, _tiles[(uint)toPlace]);
                            break;
                        default:
                            //Place tile in x and y pos with room offset
                            tilemap.SetTile(pos, _tiles[(uint)toPlace]);
                            break;
                    }
                }
            }
        }
    }

    //Place item in a room depending on surrounding walls 
    public void PlaceItems(Tilemap source, Tilemap target, TileBase tile)
    {
        foreach (Vector3Int pos in tiles)
        {
            if (source.GetTile(pos) == null && source.GetTile(pos + Vector3Int.down) != null)
            {
                if (CheckWallsAroundTile(pos, source) > 2 && Random.value < .5f)
                    target.SetTile(pos, tile);
                else if (Random.value < .2f)
                    target.SetTile(pos, tile);
            }
        }
    }

    //Check moore neighbourhood on a tile in a tilemap
    int CheckWallsAroundTile(Vector3Int pos, Tilemap tilemap)
    {
        int wallsAroundTile = 0;
        for (int checkX = -1; checkX <= 1; checkX++)
        {
            for (int checkY = -1; checkY <= 1; checkY++)
            {
                if ((checkX != 0 && checkY != 0) || (checkX == 0 && checkY == 0)) continue; //skip center and corners
                if (tilemap.GetTile(new Vector3Int(pos.x + checkX, pos.y + checkY, 0)) != null) wallsAroundTile++;
            }
        }
        return wallsAroundTile;
    }
}
