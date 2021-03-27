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

    //Keep track of level
    Level level;
    public Vector3 spawnPos;

    public enum TileID : uint
    {
        DIRT,
        STONE,
        ENTRANCE,
        EXIT,
        LADDER,
        ITEM,
        RANDOM,
        BACKGROUND,
        EMPTY
    }

    [Header("Tiles")]
    [SerializeField] TileBase[] tiles;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Tilemap doorTilemap;
    [SerializeField] private Tilemap itemTilemap;
    [SerializeField] private Tilemap ladderTilemap;
    [SerializeField] private Tilemap background;

    public Tilemap Tilemap { get => tilemap; }

    //Store room templates by their type
    [System.Serializable]
    public struct RoomTemplate
    {
        public Texture2D[] images;
    }

    [Header("Room templates")] //0 random, 1 corridor, 2 drop from, 3 drop to
    [SerializeField] public RoomTemplate[] templates = new RoomTemplate[4];

    [Header("Color dictionary")]
    public Dictionary<Color32, TileID> byColor;

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
            [Color.clear] = TileID.EMPTY
        };

        player = FindObjectOfType<Player>();

        GenerateLevel();
    }

    public bool doingSetup;
    public Player player;

    public void GenerateLevel()
    {
        doingSetup = true;
        //Keep track of time it takes to generate levels
        var watch = System.Diagnostics.Stopwatch.StartNew();

        ClearTiles();
        GenerateBorder();
        level = new Level(levelWidth, levelHeight);
        level.Generate();
        BuildRooms();

        //Spawn player in
        player.transform.position = spawnPos;

        //Stop timer and print time elapsed
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Debug.Log("Generation Time: " + elapsedMs + "ms");
        doingSetup = false;
    }

    //Clears tilemaps and rooms
    private void ClearTiles()
    {
        tilemap.ClearAllTiles();
        ladderTilemap.ClearAllTiles();
        itemTilemap.ClearAllTiles();
        doorTilemap.ClearAllTiles();
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
                else tileArray[y * width + x] = tiles[(uint)TileID.BACKGROUND];
            }
        }
        background.SetTilesBlock(area, tileArray);
    }

    private void BuildRooms()
    {
        foreach (Room r in level.Rooms)
        {
            int offsetX = r.X * Config.ROOM_WIDTH; //Left to right
            int offsetY = -r.Y * Config.ROOM_HEIGHT; //Top to bottom

            //Try to get template from list, and store pixels into flattened array
            Color32[] colors = templates[r.Type].images[Random.Range(0, templates[r.Type].images.Length)].GetPixels32();
            for (int y = 0; y < Config.ROOM_HEIGHT; y++)
            {
                for (int x = 0; x < Config.ROOM_WIDTH; x++)
                {
                    Vector3Int pos = new Vector3Int(x + offsetX, y + offsetY, 0); //Set position for tile
                    //Try to parse
                    if (byColor.TryGetValue(colors[y * Config.ROOM_WIDTH + x], out TileID id))
                    {
                        r.tiles[y * Config.ROOM_WIDTH + x].pos = pos;
                        r.tiles[y * Config.ROOM_WIDTH + x].id = id;
                        //Skip empty tiles
                        if (id == TileID.EMPTY) continue;
                        switch (id)
                        {
                            case TileID.RANDOM:
                                if (Random.value <= .25f) 
                                    tilemap.SetTile(pos, tiles[(uint)id]);
                                else if (Random.value <= .25f)
                                    tilemap.SetTile(pos, tiles[(uint)TileID.DIRT]);
                                break;
                            case TileID.LADDER:
                                ladderTilemap.SetTile(pos, tiles[(uint)id]);
                                break;
                            default:
                                tilemap.SetTile(pos, tiles[(uint)id]);
                                break;
                        }
                    }
                    else Debug.LogError("Error parsing image!");
                }
            }
            //Place items down
            PlaceItems(r);
            //Place entrance, exit and set spawn pos
           if (r == level.Entrance) spawnPos = tilemap.GetCellCenterWorld(PlaceEntrance(r)); 
           else if (r == level.Exit) PlaceExit(r);
        }
    }

    //Place item in a room depending on surrounding walls 
    public void PlaceItems(Room r)
    {
        foreach (Room.Tile t in r.tiles)
        {         
            var pos = t.pos;
            if (tilemap.GetTile(pos) == null && tilemap.GetTile(pos + Vector3Int.down) != null)
            {
                if (CheckWallsAroundTile(pos, tilemap) > 2 && Random.value < .5f)
                    itemTilemap.SetTile(pos, tiles[(uint)TileID.ITEM]);
                else if (Random.value < .2f)
                    itemTilemap.SetTile(pos, tiles[(uint)TileID.ITEM]);
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
    public Vector3Int PlaceEntrance(Room r)
    {
        Vector3Int pos = RandomDoorPosition(r);
        itemTilemap.SetTile(pos, tiles[(uint)TileID.ENTRANCE]);
        return pos;
    }

    public void PlaceExit(Room r)
    {
        Vector3Int pos = RandomDoorPosition(r);
        doorTilemap.SetTile(pos, tiles[(uint)TileID.ENTRANCE]);
    }

    public Vector3Int RandomDoorPosition(Room r)
    {
        List<Vector3Int> availablePos = new List<Vector3Int>();
        foreach (Room.Tile t in r.tiles)
        {
            var pos = t.pos;
            //If there is a floor below make position available for door placement
            if (tilemap.GetTile(pos) == null
                && tilemap.GetTile(pos + Vector3Int.down) != null
                && tilemap.GetTile(pos + Vector3Int.up) == null)
                availablePos.Add(pos);
        }
        Vector3Int doorPos = availablePos[Random.Range(0, availablePos.Count)];
        return doorPos;
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
        if (!Application.isPlaying || doingSetup) return;
        DrawRooms();
        DrawPath();    
    }

    void DrawRooms()
    {
        foreach (Room r in level.Rooms)
        {
            //Draw room ID and boundary
            Gizmos.color = new Color32(255, 253, 0, 128);
            Gizmos.DrawWireCube(r.Center(), new Vector2(Config.ROOM_WIDTH, Config.ROOM_HEIGHT));
            Handles.Label(r.Origin() + new Vector2(.5f,-.5f), r.Type.ToString(), style);

            if (r == level.Entrance) Gizmos.color = Color.green;
            else if (r == level.Exit) Gizmos.color = Color.red;
            else continue;
            Gizmos.DrawWireCube(r.Center(), new Vector3(1, 1));
        }
    }

    void DrawPath()
    {
        Room previous = null;
        foreach (Room i in level.Path)
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

