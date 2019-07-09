using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //In each simulation we need at least x neightbours alive or the cell dies
    [Range(1,8)]
    public int deathlimit;
    //In each simulation if we have more than x neightbours alive the cell revives
    [Range(1,8)]
    public int birthlimit;
    //Map dimesions
    public int width;
    public int height;

    //Times we want the simulation to repeat
    public int StepSimulations;

    //Custom or random seed
    public string seed;
    public bool useRandomSeed;

    [Range(0,100)]
    public int randomFillPercent;

    int[,] map;
    int[,] newmap;
    void Start()
    {
        GenerateMap();   
    }

    //We get a new simulation by leftclick on edit-mode
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            GenerateMap();

    }

    //Main method to create our cave. First we generate a random map based on our RandomFillPercent. Afterwards we repeat our loop depending
    //on our stepSimulation and acording to our birth and death selected variables.
    void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();
        for ( int i = 0; i < StepSimulations; i++)
        {
            SimulationStep();
            //Our new map is created according to the old one so we need 2 maps or the new changes on our map will affect to fowards decisions.
            //After the process we take the newmap as the current one.
            //map = newmap;
        }
        procressMap();

        //We create a border on the map just to make it more pretty.
        int borderSize = 1;
		int[,] borderedMap = new int[width + borderSize * 2,height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x ++) {
			for (int y = 0; y < borderedMap.GetLength(1); y ++) {
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
					borderedMap[x,y] = map[x-borderSize,y-borderSize];
				}
				else {
					borderedMap[x,y] =1;
				}
			}
		}

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }

    //Method for postprocess the map in order to delete the regions that don´t have a minimum of size
    void procressMap()
    {
        //First we compare the wall in our map
        List<List<Coord>> wallRegions = getRegions(1);
        int wallThresholdSize = 50;
        
        foreach(List<Coord> wallRegion in wallRegions)
        {
            if(wallRegion.Count < wallThresholdSize)
            {
                foreach(Coord tile in wallRegion)
                {
                    //We transform the walls into room
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        //We compare the room in our map
        List<List<Coord>> roomRegions = getRegions(0);
		List<Room> survivingRooms = new List<Room>();
        int roomThresholdSize = 50;
        
		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
                    //We transform the room into wall
					map[tile.tileX,tile.tileY] = 1;
				}
			}
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
		}
        connectClosestRooms(survivingRooms);
    }

    void connectClosestRooms(List<Room> allRooms)
    {
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach(Room roomA in allRooms)
        {
            foreach(Room roomB in allRooms)
            {
                possibleConnectionFound = false;
                if(roomA == roomB)
                {
                    continue;
                }
                if(roomA.isConnected(roomB))
                {
                    possibleConnectionFound = false;
                    break;
                }
                for(int tileA = 0; tileA < roomA.edgeTiles.Count; tileA++)
                {
                    for(int tileB = 0; tileB < roomB.edgeTiles.Count; tileB++)
                    {
                        Coord edgeTileA = roomA.edgeTiles[tileA];
                        Coord edgeTileB = roomB.edgeTiles[tileB];
                        int distanceBetweenRooms = (int)(Math.Pow(edgeTileA.tileX-edgeTileB.tileX,2) + Math.Pow(edgeTileA.tileY-edgeTileB.tileY,2));
                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = edgeTileA;
                            bestTileB = edgeTileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
        }
        if(possibleConnectionFound)
        {
            createPassage(bestRoomA,bestRoomB,bestTileA,bestTileB);
        }
    }

    void createPassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms (roomA, roomB);
		Debug.DrawLine (CoordToWorldPoint (tileA), CoordToWorldPoint (tileB), Color.green, 100);
    }

    Vector3 CoordToWorldPoint(Coord tile)
    {
		return new Vector3 (-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
    }
    //We get the region that share the same type as the tile analyzed. This region will be used afterwards to compare its size 
    //with the minimum wanted.
    List<List<Coord>> getRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();

        //Mapflag is used for not repeating comparations. 1 == already compared.
        int[,] mapFlags = new int[width,height];
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y <height; y++)
            {
                //Not compared tile && the same type we are looking for
                if( mapFlags[x,y] == 0 && map[x,y] == tileType)
                {
                    List<Coord> newRegion = getRegionTiles(x,y);
                    regions.Add(newRegion);

                    foreach(Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    //This methods compares the tiles one by one in " + " pattern. We use a queue in order to make the comparation more regular and efficient
    List<Coord> getRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int [,] mapFlags = new int[width,height];
        int tileType = map[startX,startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX,startY] = 1;

        while(queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if(IsInMapRange(x,y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if(mapFlags[x,y] == 0 && map[x,y] == tileType)
                        {
                            mapFlags[x,y] = 1;
                            queue.Enqueue(new Coord(x,y));
                        }
                    }
                }
            }
        }
        return tiles;
    }

    //Method that returns if a tile is inside the map
    bool IsInMapRange(int x, int y)
    {
        bool aux = false;
        if(x >= 0 && x < width && y >= 0 && y < height)
            aux = true;

        return aux;
    }

    //This lets us repeat the process and compare the neightbours in our to draw the new map based on the birth and death limits
    void SimulationStep()
    {
        for (int x = 0; x < width; x ++) {
			for (int y = 0; y < height; y ++) {
				int neighbourWallTiles = countNeightbours(x,y);

				if (neighbourWallTiles > birthlimit)
					map[x,y] = 1;
				else if (neighbourWallTiles < deathlimit)
					map[x,y] = 0;

			}
		}
    }

    //Returns an int with the number of neightbours alive
    int countNeightbours(int x, int y)
    {
        int count = 0;
        for(int i = x-1; i <= x+1 ; i++)
        {
            for( int j = y-1; j <= y+1; j++)
            {
                if( i == x && j == y)
                {

                }
                else if(!IsInMapRange(i,j ) || map[i,j] == 1)
                {
                    count++;
                }
            }
        }
        return count;
    }

    //On our first ride we fill our map using a seed selected by us or a pseudorandom one by getting the milliseconds of our clock. 
    //This seed will generate a pseudorandom numbre on each tile of the map which will be compared with the randomFillPercent selected
    //and select the cell that will be alive and dead.
    void RandomFillMap()
    {
        if (useRandomSeed) {
			seed =  DateTime.Now.Millisecond.ToString();
		}

		System.Random pseudoRandom = new System.Random(seed.GetHashCode());

		for (int x = 0; x < width; x ++) {
			for (int y = 0; y < height; y ++) {
				if (x == 0 || x == width-1 || y == 0 || y == height -1) {
					map[x,y] = 1;
				}
				else {
					map[x,y] = (pseudoRandom.Next(0,100) < randomFillPercent)? 1: 0;
				}
			}
		}
    }

    struct Coord 
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    class Room
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, int[,] map) {
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room>();

			edgeTiles = new List<Coord>();
			foreach (Coord tile in tiles) {
				for (int x = tile.tileX-1; x <= tile.tileX+1; x++) {
					for (int y = tile.tileY-1; y <= tile.tileY+1; y++) {
						if (x == tile.tileX || y == tile.tileY) {
							if (map[x,y] == 1) {
								edgeTiles.Add(tile);
							}
						}
					}
				}
			}
		}

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool isConnected(Room otherRoom)
        {
            bool aux = false;
            if(connectedRooms.Contains(otherRoom))
            {
                aux = true;
            }
            return aux;
        }
    }
}
