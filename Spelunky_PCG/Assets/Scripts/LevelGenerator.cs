// Pol Lozano Llorens
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelGenerator : MonoBehaviour
{
    [Header("Level Size")]
    [Range(1,16)]

    [SerializeField] private int levelHeight = 4;
    [Range(1,16)]
    [SerializeField] private int levelWidth = 4;

    private readonly int roomHeight = 8;
    private readonly int roomWidth = 10;

    enum gridSpace { empty, dirt, stone, ladder, random };

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

    [Header("Tiles")]
    [SerializeField] private TileBase dirtTile;
    [SerializeField] private TileBase doorTile;
    [SerializeField] private TileBase ladderTile;
    [SerializeField] private TileBase randomTile;
    [SerializeField] private TileBase itemTile;
    [SerializeField] private TileBase stoneTile;
    [SerializeField] private TileBase bgTile;

    Dictionary<Color, TileBase> byColor;

    [Header("Room templates")]
    [SerializeField] private RoomTemplate[] templates = new RoomTemplate[4];

    //Store room templates by their type
    [System.Serializable]
    public struct RoomTemplate
    {
        public string name;
        public Texture2D[] images;
    }

    private Player player;

    void Awake()
    {
        //Store tiles by their color
        byColor = new Dictionary<Color, TileBase>()
        {
            [Color.black] = dirtTile,
            [Color.blue] = stoneTile,
            [Color.red] = ladderTile,
            [Color.green] = randomTile,
            [Color.white] = null,
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
        int width = (levelWidth * roomWidth);
        int height = (levelHeight * roomHeight);
        BoundsInt area = new BoundsInt(0, -height + roomHeight, 0, width, height, 1);

        TileBase[] tileArray = new TileBase[width * height];

        for (int x = -1; x <= width; x++)
        {
            for (int y = -1; y <= height; y++)
            {
                //Fill border
                if (x == -1 || y == -1 || x == width || y == height)
                    tilemap.SetTile(new Vector3Int(x, -y + roomHeight - 1, 0), dirtTile);
                //Fill background
                else
                    tileArray[y * width + x] = bgTile;
            }
        }
        background.SetTilesBlock(area, tileArray);
    }

    //Room path generation algorithm
    private void GenerateRoomPath()
    {
        //Pick a room from the top row and place the entrance
        int start = Random.Range(0, levelWidth);
        Vector2 dir;

        int x = start, prevX = start;
        int y = 0, prevY = 0;

        rooms[x, y].Type = 1;
        entrance = rooms[x, y];

        //Stop when it attempts to go out of y bounds
        while (y < levelHeight)
        {
            //Select next random direction to move          
            dir = RandomDirection(x);

            //Try to move to the right            
            if (dir == Vector2.right)
            {
                if (x < levelWidth - 1)
                {
                    //Check if room is empty and move to the right if it is
                    if (rooms[x + 1, y].Type == 0) x += 1;
                }
                else if (x > 0)
                {
                    //Move to the left i
                    if (rooms[x - 1, y].Type == 0) x -= 1;
                }
                else
                {
                    dir = Vector2.down;
                }
            }
            //Try to move to the left            
            if (dir == Vector2.left)
            {
                if (x > 0)
                {
                    if (rooms[x - 1, y].Type == 0) x -= 1;
                }
                else if (x < levelWidth - 1)
                {
                    //Check if room is empty
                    if (rooms[x + 1, y].Type == 0) x += 1;
                }
                else
                {
                    dir = Vector2.down;
                }
            }
            //Try to move down
            if (dir == Vector2.down)
            {
                y++;

                //If not out of bounds
                if (y < levelHeight)
                {
                    //Room you drop into
                    rooms[prevX, prevY].Type = 2;
                    //Room you drop from
                    rooms[x, y].Type = 3;
                }
                else
                {
                    exit = rooms[x, y - 1];
                }
            }
            else 
            {
                //Corridor you run through
                rooms[x, y].Type = 1;
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
            FillRoom(r);
            PlaceItems(r);

            //Possible door location
            if (r == entrance)
            {
                //Non interactable door
                Vector3Int doorPos = PlaceDoor(r,itemTilemap);
                //Place player spawn position
                player.transform.position = tilemap.GetCellCenterWorld(doorPos);
            }
            else if (r == exit)
            {
                PlaceDoor(r,doorTilemap);
            }

        }
    }

    Vector3Int PlaceDoor(Room r, Tilemap t)
    {
        List<Vector3Int> availablePos = new List<Vector3Int>();
        foreach (Vector3Int pos in r.tiles)
        {
            //If there is a floor below make position available for door placement
            if (tilemap.GetTile(pos) == null 
                && tilemap.GetTile(pos + Vector3Int.down) != null
                && tilemap.GetTile(pos + Vector3Int.up) == null)
                availablePos.Add(pos);
        }
        Vector3Int doorPos = availablePos[Random.Range(0, availablePos.Count)];
        t.SetTile(doorPos, doorTile);
        return doorPos;
    }

    void FillRoom(Room room)
    {
        //Try to get template from list
        Texture2D template = templates[room.Type].images[Random.Range(0, templates[room.Type].images.Length)];

        //Left to right
        int offsetX = room.X * roomWidth;
        //Top to bottom
        int offsetY = -room.Y * roomHeight;

        //Flattened array of image
        Color32[] colors = template.GetPixels32();

        for (int y = 0; y < roomHeight; y++)
        {
            for (int x = 0; x < roomWidth; x++)
            {
                //Get position for tile
                Vector3Int pos = new Vector3Int(x + offsetX, y + offsetY, 0);
                if (byColor.TryGetValue(colors[y * roomWidth + x], out TileBase toPlace))
                {
                    if (toPlace == randomTile)
                    {
                        if (Random.value <= .25f)
                        {
                            //Place tile in x and y pos with room offset
                            tilemap.SetTile(pos, toPlace);
                        }else if(Random.value <= .25f)
                        {
                            //Place dirt tile
                            tilemap.SetTile(pos, dirtTile);
                        }
                           
                    }
                    else if (toPlace == ladderTile)
                        ladderTilemap.SetTile(pos, toPlace);
                    //Place tile in x and y pos with room offset
                    else
                        tilemap.SetTile(pos, toPlace);
                }
                //Store tiles pos in each room
                room.tiles[x, y] = pos;
            }
        }
    }

    //Place item in a room depending on surrounding walls 
    void PlaceItems(Room room)
    {
        foreach (Vector3Int pos in room.tiles)
        {
            if (tilemap.GetTile(pos) == null && tilemap.GetTile(pos + Vector3Int.down) != null)
            {
                if (CheckWallsAroundTile(pos) > 2 && Random.value < .5) 
                    itemTilemap.SetTile(pos, itemTile);
                else if(Random.value < .2)
                    itemTilemap.SetTile(pos, itemTile);
            }
        }
    }

    //Check moore neighbourhood on a tile
    int CheckWallsAroundTile(Vector3Int pos)
    {
        int wallsAroundTile = 0;

        //Check moore neighborhood around tile
        for (int checkX = -1; checkX <= 1; checkX++)
        {
            for (int checkY = -1; checkY <= 1; checkY++)
            {
                if ((checkX != 0 && checkY != 0) || (checkX == 0 && checkY == 0))
                {
                    //skip center and corners
                    continue;
                }
                if (tilemap.GetTile(new Vector3Int(pos.x + checkX, pos.y + checkY, 0)) != null)
                    wallsAroundTile++;
            }
        }
        return wallsAroundTile;
    }

    //Pick random direction to go
    Vector2 RandomDirection(int x)
    {
        int choice = Mathf.FloorToInt(Random.value * 4.99f);
        switch (choice)
        {
            //40% Chance to go right or left and 20% to go down
            case 0: case 1: return Vector2.left;
            case 2: case 3: return Vector2.right;
            default: return Vector2.down;
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

        foreach (Room r in rooms)
        {
            if (r == entrance)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(r.Center(), new Vector3(1, 1));
            }
            else if (r == exit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(r.Center(), new Vector3(1, 1));
            }
            Gizmos.color = new Color32(255, 253, 0, 128);
            Gizmos.DrawWireCube(r.Center(), new Vector3(roomWidth, roomHeight));
            Handles.Label(r.tiles[0, r.tiles.GetUpperBound(1)] + new Vector3(.5f, .5f), r.Type.ToString(), style);
        }

        Room previous = null;
        foreach (Room i in path)
        {
            if (previous != null)
            {
                Handles.color = Color.white;
                Handles.DrawDottedLine(i.Center(), previous.Center(), 3);
                Handles.color = Color.magenta;
                Handles.ConeHandleCap(0, (i.Center() + previous.Center()) / 2 + (previous.Center() - i.Center()).normalized, Quaternion.LookRotation((i.Center() - previous.Center()).normalized), 1f, EventType.Repaint);
            }
            previous = i;
        }
    }
#endif
}

