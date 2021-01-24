// Pol Lozano Llorens
using UnityEngine;
using Code.Utils;

public class Room
{
    public int Id { get; set; }
    public int Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public struct Tile
    {
        public LevelGenerator.TileID id;
        public Vector3Int pos;
    }

    //Store tile pos and id
    public Tile[] tiles = new Tile[Config.ROOM_WIDTH * Config.ROOM_HEIGHT];

    public Room(int id,int x,int y)
    {
        X = x;
        Y = y;
        Id = id;
        Type = 0;
    }

    //Get world pos of center of the room
    public Vector2 Center()
    {
        int offsetX = X * Config.ROOM_WIDTH; //Left to right
        int offsetY = -Y * Config.ROOM_HEIGHT; //Top to bottom
        return new Vector2(offsetX + Config.ROOM_WIDTH/2, offsetY+Config.ROOM_HEIGHT/2);
    }

    public Vector2 Origin()
    {
        int offsetX = X * Config.ROOM_WIDTH; //Left to right
        int offsetY = -Y * Config.ROOM_HEIGHT; //Top to bottom
        return new Vector2(offsetX, offsetY + Config.ROOM_HEIGHT);
    }
}
