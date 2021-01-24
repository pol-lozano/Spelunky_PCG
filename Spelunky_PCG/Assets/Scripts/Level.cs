using System.Collections.Generic;
using UnityEngine;

public class Level
{
    public Level(int w, int h)
    {
        width = w;
        height = h;
    }

    private int width;
    private int height;

    private Room[] rooms;
    private HashSet<Room> path;
    private Room entrance;
    private Room exit;

    private Vector3Int spawnPos;

    public Room[] Rooms { get => rooms; }
    public HashSet<Room> Path { get => path; }
    public Room Entrance { get => entrance; }
    public Room Exit { get => exit; }
    public Vector3Int SpawnPos { get => spawnPos; set => spawnPos = value; }

    public void Generate()
    {
        Initialize();
        GenerateRoomPath();
    }

    private void Initialize()
    {
        rooms = new Room[width * height];
        path = new HashSet<Room>();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++)
                rooms[GetRoomID(x, y)] = new Room(GetRoomID(x, y),x,y);
        }
    }

    //Room path generation algorithm
    private void GenerateRoomPath()
    {
        //Pick a room from the top row and place the entrance
        int start = Random.Range(0, width);
        int x = start, prevX = start;
        int y = 0, prevY = 0;

        rooms[GetRoomID(x, y)].Type = 1;
        entrance = rooms[GetRoomID(x, y)];

        //Generate path until bottom floor
        while (y < height)
        {
            //Select next random direction to move          
            switch (RandomDirection())
            {
                case Direction.RIGHT:
                    if (x < width - 1 && rooms[GetRoomID(x+1, y)].Type == 0) x++; //Check if room is empty and move to the right if it is
                    else if (x > 0 && rooms[GetRoomID(x-1, y)].Type == 0) x--; //Move to the left 
                    else goto case Direction.DOWN;
                    rooms[GetRoomID(x, y)].Type = 1; //Corridor you run through
                    break;
                case Direction.LEFT:
                    if (x > 0 && rooms[GetRoomID(x - 1, y)].Type == 0) x--; //Move to the left 
                    else if (x < width - 1 && rooms[GetRoomID(x + 1, y)].Type == 0) x++; //Move to the right
                    else goto case Direction.DOWN;
                    rooms[GetRoomID(x, y)].Type = 1; //Corridor you run through
                    break;
                case Direction.DOWN:
                    y++;
                    //If not out of bounds
                    if (y < height)
                    {
                        rooms[GetRoomID(prevX, prevY)].Type = 2; //Room you fall from
                        rooms[GetRoomID(x, y)].Type = 3; //Room you drop into
                    }
                    else exit = rooms[GetRoomID(x, y-1)]; //Place exit room     
                    break;
            }

            path.Add(rooms[GetRoomID(prevX, prevY)]);
            prevX = x;
            prevY = y;
        }
    }

    enum Direction
    {
        UP = 0,
        LEFT = 1,
        RIGHT = 2,
        DOWN = 3
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

    private int GetRoomID(int x, int y) {
        return y * width + x;
    }
}
