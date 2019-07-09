﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Range(1,8)]
    public int deathlimit;
    [Range(1,8)]
    public int birthlimit;
    public int width;
    public int height;

    public int StepSimulations;
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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            GenerateMap();

    }
    void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();
        for ( int i = 0; i < StepSimulations; i++)
        {
            SimulationStep();
            map = newmap;
        }
        procressMap();

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

    void procressMap()
    {
        List<List<Coord>> wallRegions = getRegions(1);
        int wallThresholdSize = 50;
        foreach(List<Coord> wallRegion in wallRegions)
        {
            if(wallRegion.Count < wallThresholdSize)
            {
                foreach(Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }
        List<List<Coord>> roomRegions = getRegions(0);
		int roomThresholdSize = 50;
		
		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
					map[tile.tileX,tile.tileY] = 1;
				}
			}
		}

    }

    List<List<Coord>> getRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width,height];
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y <height; y++)
            {
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

    bool IsInMapRange(int x, int y)
    {
        bool aux = false;
        if(x >= 0 && x < width && y >= 0 && y < height)
            aux = true;

        return aux;
    }

    void SimulationStep()
    {
        newmap = new int[width, height];
        int neightbours = 0;
        for( int i = 0; i < width; i++)
        {
            for( int j = 0; j < height; j++)
            {
                neightbours = countNeightbours(i,j);
                if(map[i,j] == 1)
                {
                    if(neightbours < deathlimit)
                    {
                        newmap[i,j] = 0;
                    }
                    else
                    {
                        newmap[i,j] = 1;
                    }
                }
                else
                {
                    if(neightbours > birthlimit)
                    {
                        newmap[i,j] = 1;
                    }
                    else
                    {
                        newmap[i,j] = 0;
                    }
                }
            }
        }
    }

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
}
