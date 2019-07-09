using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;
    public MeshFilter walls;
    List<Vector3> vertices;
    List<int> triangles;
    Dictionary<int,List<Triangle>> triangleDictionary = new Dictionary<int,List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] map, float squareSize) {
		
        //Clean the previous cave info
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        //New squareGrid, parametres are the map already created in mapGenerator and the size
        squareGrid = new SquareGrid(map, squareSize);

        //Initialate vars
		vertices = new List<Vector3>();
		triangles = new List<int>();

        //We go through the squareGrid triangulating each square one by one
		for (int x = 0; x < squareGrid.squares.GetLength(0); x ++) {
			for (int y = 0; y < squareGrid.squares.GetLength(1); y ++) {
				TriangulateSquare(squareGrid.squares[x,y]);
			}
		}

		Mesh mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();

        CreateWallMesh();

	}

    void CreateWallMesh()
    {
        CalculateMeshOutlines();
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        foreach (List<int> outline in outlines)
        {
            for(int i = 0; i < outline.Count-1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]);
                wallVertices.Add(vertices[outline[i+1]]);
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);
                wallVertices.Add(vertices[outline[i+1]] - Vector3.up * wallHeight);
            
                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;
    }


    //Based on the veriable configartion which gives a value to each square based on the active nodes. Each square has
    //4 control nodes (corners) and 4 nodes (centres). By asigning each node a value we can know the composition of active
    //control nodes by each case based in 4 bits. 
    void TriangulateSquare(Square square) {
		switch (square.configuration) {
		case 0:
			break;

		// 1 points:
		case 1:
			MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
			break;
		case 2:
			MeshFromPoints(square.centreBottom, square.centreRight, square.bottomRight);
			break;
		case 4:
			MeshFromPoints(square.centreRight, square.centreTop, square.topRight);
			break;
		case 8:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
			break;

		// 2 points:
		case 3:
			MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			break;
		case 6:
			MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
			break;
		case 9:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
			break;
		case 12:
			MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
			break;
		case 5:
			MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
			break;
		case 10:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
			break;

		// 3 point:
		case 7:
			MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			break;
		case 11:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
			break;
		case 13:
			MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
			break;
		case 14:
			MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
			break;

		// 4 point:
		case 15:
			MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
			
            //At the end we want to draw the border and this is de unice case which all the nodes belong to corners so we
            //mark them as checked here in order to do it more optimal
            checkedVertices.Add(square.topLeft.vertexIndex);
            checkedVertices.Add(square.topRight.vertexIndex);
			checkedVertices.Add(square.bottomRight.vertexIndex);
			checkedVertices.Add(square.bottomLeft.vertexIndex);
            break;
		}
    }

    //Create triangles based on triangulation on the active nodes
	void MeshFromPoints(params Node[] points) {
		AssignVertices(points);

		if (points.Length >= 3)
			CreateTriangle(points[0], points[1], points[2]);
		if (points.Length >= 4)
			CreateTriangle(points[0], points[2], points[3]);
		if (points.Length >= 5) 
			CreateTriangle(points[0], points[3], points[4]);
		if (points.Length >= 6)
			CreateTriangle(points[0], points[4], points[5]);

	}

	void AssignVertices(Node[] points) {
		for (int i = 0; i < points.Length; i ++) {
			if (points[i].vertexIndex == -1) {
				points[i].vertexIndex = vertices.Count;
				vertices.Add(points[i].position);
			}
		}
	}

	void CreateTriangle(Node a, Node b, Node c) {
		triangles.Add(a.vertexIndex);
		triangles.Add(b.vertexIndex);
		triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex,b.vertexIndex,c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
	}

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if(triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else{
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey,triangleList);
        }
    }

    void CalculateMeshOutlines()
    {
        for(int i = 0; i < vertices.Count; i++)
        {
            if(!checkedVertices.Contains(i))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(i);
                if(newOutlineVertex != -1)
                {
                    checkedVertices.Add(i);
                    List<int> newOutline = new List<int>();
                    newOutline.Add(i);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count-1);
                    outlines[outlines.Count-1].Add(i);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if(nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        for(int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for(int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if( vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if(IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }
        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary [vertexA];
        int sharedTriangleCount = 0;
        bool unique = true;

        for(int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if(trianglesContainingVertexA[i].Contains(vertexB))
                sharedTriangleCount ++;

            if(sharedTriangleCount > 1)
            {
                unique = false;
                break;                
            }
        }
        return unique;
    }

    public class Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    public class SquareGrid {
	    public Square[,] squares;

	    public SquareGrid(int[,] map, float squareSize) {
		    int nodeCountX = map.GetLength(0);
		    int nodeCountY = map.GetLength(1);
		    float mapWidth = nodeCountX * squareSize;
		    float mapHeight = nodeCountY * squareSize;

		    ControlNode[,] controlNodes = new ControlNode[nodeCountX,nodeCountY];

		    for (int x = 0; x < nodeCountX; x ++) {
		    	for (int y = 0; y < nodeCountY; y ++) {
		    		Vector3 pos = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, 0, -mapHeight/2 + y * squareSize + squareSize/2);
		    		controlNodes[x,y] = new ControlNode(pos,map[x,y] == 1, squareSize);
		    	}
		    }

		    squares = new Square[nodeCountX -1,nodeCountY -1];
		    for (int x = 0; x < nodeCountX-1; x ++) {
			    for (int y = 0; y < nodeCountY-1; y ++) {
				    squares[x,y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
			    }
		    }
	    }
    }

    //Each Square groups 4 control nodes. Each control node representates a pixel in previous versions. Between them we have nodes that
    //will help us triangulate the map in a more organic way. 
    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
		public Node centreTop, centreRight, centreBottom, centreLeft;
        //Var responsible of counting how many active control nodes we have
		public int configuration;

		public Square (ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
			topLeft = _topLeft;
			topRight = _topRight;
			bottomRight = _bottomRight;
			bottomLeft = _bottomLeft;

			centreTop = topLeft.right;
			centreRight = bottomRight.above;
			centreBottom = bottomLeft.right;
			centreLeft = bottomLeft.above;

			if (topLeft.active)
				configuration += 8;
			if (topRight.active)
				configuration += 4;
			if (bottomRight.active)
				configuration += 2;
			if (bottomLeft.active)
				configuration += 1;
		}
    }

    //Nodes between 2 control nodes
    public class Node
    {
        public Vector3 position;
        //This var will help us find this node for fowards triangulation
        public int vertexIndex = -1;

        public Node(Vector3 _pos) 
        {
            position = _pos;
        }
    }

    //Representates the areas that can be activated or not
    public class ControlNode : Node
    {
        public bool active;
        //This will help us find faster and easily all the nodes next to our control nodes
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            //The nodes position are between 2 control nodes
            above = new Node(position + Vector3.forward * squareSize/2f); 
            right = new Node(position + Vector3.right * squareSize/2f);
        }
    }
}
