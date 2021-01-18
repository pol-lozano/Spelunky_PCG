// Pol Lozano Llorens
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Code.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelGenerator : MonoBehaviour
{
    [Header("Level Size")]
    [Range(1, 16)]
    [SerializeField] private int levelHeight = 4;
    [Range(1, 16)]
    [SerializeField] private int levelWidth = 4;

    public enum TileID : uint
    {
        DIRT,
        STONE,
        DOOR,
        LADDER,
        ITEM,
        RANDOM,
        BACKGROUND,
        EMPTY
    }

    [Header("Tiles")]
    [SerializeField] TileBase[] tiles;

    private Room[,] rooms;
    private HashSet<Room> path;
    private Room entrance;
    private Room exit;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Tilemap doorTilemap;
    [SerializeField] private Tilemap itemTilemap;
    [SerializeField] private Tilemap ladderTilemap;
    [SerializeField] private Tilemap background;

    public Tilemap Tilemap { get { return tilemap; } }

    [Header("Room templates")] //0 random, 1 corridor, 2 drop from, 3 drop to
    [SerializeField] private RoomTemplate[] templates = new RoomTemplate[4];

    //Store room templates by their type
    [System.Serializable]
    public struct RoomTemplate
    {
        public Texture2D[] images;
    }

    private Player player;

    Dictionary<Color32, TileID> byColor;

    void Awake()
    {
        //Store tiles by their color
        byColor = new Dictionary<Color32, TileID>()
        {
            [Color.black] = TileID.DIRT,
            [Color.blue] = TileID.STONE,
            [Color.red] = TileID.LADDER,
            [Color.green] = TileID.RANDOM,
            [Color.white] = TileID.EMPTY,
        };

        player = FindObjectOfType<Player>();

        GenerateLevel();
    }

    public void GenerateLevel()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Initialize();
        GenerateBorder();
        GenerateRoomPath();
        BuildRooms();

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Debug.Log("Generation Time: " + elapsedMs + "ms");
    }

    //Clears tilemaps and rooms
    private void Initialize()
    {
        tilemap.ClearAllTiles();
        ladderTilemap.ClearAllTiles();
        itemTilemap.ClearAllTiles();
        doorTilemap.ClearAllTiles();

        rooms = new Room[levelWidth, levelHeight];
        path = new HashSet<Room>();

        for (int x = 0; x < rooms.GetLength(0); x++)
        {
            for (int y = 0; y < rooms.GetLength(1); y++)
            {
                rooms[x, y] = new Room((y * levelWidth + x), x, y);
            }
        }
    }

    //Places a border around the rooms and a background
    private void GenerateBorder()
    {
        int width = (levelWidth * Config.ROOM_WIDTH);
        int height = (levelHeight * Config.ROOM_HEIGHT);
        BoundsInt area = new BoundsInt(0, -height + Config.ROOM_HEIGHT, 0, width, height, 1);

        TileBase[] tileArray = new TileBase[width * height];

        for (int x = -1; x <= width; x++)
        {
            for (int y = -1; y <= height; y++)
            {
                //Fill border
                if (x == -1 || y == -1 || x == width || y == height)
                    tilemap.SetTile(new Vector3Int(x, -y + Config.ROOM_HEIGHT - 1, 0), tiles[(uint)TileID.DIRT]);
                //Fill background
                else
                    tileArray[y * width + x] = tiles[(uint)TileID.BACKGROUND];
            }
        }
        background.SetTilesBlock(area, tileArray);
    }

    //Room path generation algorithm
    private void GenerateRoomPath()
    {
        //Pick a room from the top row and place the entrance
        int start = Random.Range(0, levelWidth);
        int x = start, prevX = start;
        int y = 0, prevY = 0;

        rooms[x, y].Type = 1;
        entrance = rooms[x, y];

        //Generate path until bottom floor
        while (y < levelHeight)
        {
            //Select next random direction to move          
            switch (RandomDirection())
            {
                case Direction.RIGHT:
                    if (x < levelWidth - 1 && rooms[x + 1, y].Type == 0) x++; //Check if room is empty and move to the right if it is
                    else if (x > 0 && rooms[x - 1, y].Type == 0) x--; //Move to the left 
                    else goto case Direction.DOWN;
                    rooms[x, y].Type = 1; //Corridor you run through
                    break;
                case Direction.LEFT:
                    if (x > 0 && rooms[x - 1, y].Type == 0) x--; //Move to the left 
                    else if (x < levelWidth - 1 && rooms[x + 1, y].Type == 0) x++; //Move to the right
                    else goto case Direction.DOWN;
                    rooms[x, y].Type = 1; //Corridor you run through
                    break;
                case Direction.DOWN:
                    y++;
                    //If not out of bounds
                    if (y < levelHeight)
                    {
                        rooms[prevX, prevY].Type = 2; //Room you fall from
                        rooms[x, y].Type = 3; //Room you drop into
                    }
                    else exit = rooms[x, y - 1]; //Place exit room     
                    break;
            }

            path.Add(rooms[prevX, prevY]);
            prevX = x;
            prevY = y;
        }
    }

    void BuildRooms()
    {
        foreach (Room r in rooms)
        {
            r.FillRoom(tilemap, ladderTilemap, templates, tiles, byColor);
            r.PlaceItems(tilemap, itemTilemap, tiles[(uint)TileID.ITEM]);

            //Possible door location
            if (r == entrance)
            {
                //Non interactable door
                Vector3Int doorPos = r.PlaceDoor(tilemap, itemTilemap, tiles[(uint)TileID.DOOR]);
                //Place player spawn position
                player.transform.position = tilemap.GetCellCenterWorld(doorPos);
            }
            else if (r == exit)
            {
                r.PlaceDoor(tilemap, doorTilemap, tiles[(uint)TileID.DOOR]);
            }

        }
    }

    enum Direction
    {
        UP,
        LEFT,
        RIGHT,
        DOWN
    };

    //Pick random direction to go
    Direction RandomDirection()
    {
        int choice = Mathf.FloorToInt(Random.value * 4.99f);
        switch (choice)
        {
            //40% Chance to go right or left and 20% to go down
            case 0: case 1: return Direction.LEFT;
            case 2: case 3: return Direction.RIGHT;
            default: return Direction.DOWN;
        }
    }

#if UNITY_EDITOR
    [Header("Gizmos")]
    public GUIStyle style;

    //Do not allow one room levels we need at least two rooms
    private void OnValidate()
    {
        levelWidth = (levelHeight == 1 && levelWidth < 2) ? 2 : levelWidth;
        levelHeight = (levelWidth == 1 && levelHeight < 2) ? 2 : levelHeight;
    }

    //Draw gizmos
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        DrawRooms();
        DrawPath();
    }

    void DrawRooms()
    {
        foreach (Room r in rooms)
        {
            //Draw room ID and boundary
            Gizmos.color = new Color32(255, 253, 0, 128);
            Gizmos.DrawWireCube(r.Center(), new Vector3(Config.ROOM_WIDTH, Config.ROOM_HEIGHT));
            Handles.Label(r.tiles[0, r.tiles.GetUpperBound(1)] + new Vector3(.5f, .5f), r.Type.ToString(), style);

            if (r == entrance) Gizmos.color = Color.green;
            else if (r == exit) Gizmos.color = Color.red;
            else continue;
            Gizmos.DrawWireCube(r.Center(), new Vector3(1, 1));
        }
    }

    void DrawPath()
    {
        Room previous = null;
        foreach (Room i in path)
        {
            if (previous != null)
            {
                Handles.color = Color.white;
                Handles.DrawDottedLine(i.Center(), previous.Center(), 3);
                Handles.color = Color.magenta;
                Quaternion rot = Quaternion.LookRotation(i.Center() - previous.Center()).normalized;
                Handles.ConeHandleCap(0, (i.Center() + previous.Center()) / 2 + (previous.Center() - i.Center()).normalized, rot, 1f, EventType.Repaint);
            }
            previous = i;
        }
    }
#endif
}

