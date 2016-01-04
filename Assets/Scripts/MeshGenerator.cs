using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour {
	
	public SquareGrid squareGrid;
	public MeshFilter walls;
	List<Vector3> vertices;
	List<int> triangles;

	// vertex - triangle dictionary
	Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
	List<List<int>> outlines = new List<List<int>> ();
	HashSet<int> checkedVertices = new HashSet<int> ();

	public void GenerateMesh(int [,] map, float squareSize) {

		outlines.Clear ();
		checkedVertices.Clear ();
		triangleDict.Clear ();

		squareGrid = new SquareGrid (map, squareSize);

		vertices = new List<Vector3> ();
		triangles = new List<int> ();

		for (int x = 0; x < squareGrid.squares.GetLength (0); x++) {
			for (int y = 0; y < squareGrid.squares.GetLength (1); y++) {
				TriangulateSquare (squareGrid.squares [x, y]);
			}
		}

		Mesh m = new Mesh ();
		GetComponent<MeshFilter> ().mesh = m;

		m.vertices = vertices.ToArray ();
		m.triangles = triangles.ToArray ();
		m.RecalculateNormals ();

		createWallMesh ();
	}

	void createWallMesh() {

		CalculateMeshOutlines ();

		List<Vector3> wallVertices = new List<Vector3> ();
		List<int> wallTriangles = new List<int> ();

		Mesh wallMesh = new Mesh ();
		float wallHeight = 20; 

		foreach (List<int> outline in outlines) {
			for (int i = 0; i < outline.Count - 1; i++) {
				int startIndex = wallVertices.Count;
				wallVertices.Add (vertices [outline [i]]); // left
				wallVertices.Add (vertices [outline [i+1]]); // right
				wallVertices.Add (vertices [outline [i]] - Vector3.up * wallHeight); // bottom left
				wallVertices.Add (vertices [outline [i+1]] - Vector3.up * wallHeight); // bottom right

				wallTriangles.Add (startIndex + 0);
				wallTriangles.Add (startIndex + 2);
				wallTriangles.Add (startIndex + 3);

				wallTriangles.Add (startIndex + 3);
				wallTriangles.Add (startIndex + 1);
				wallTriangles.Add (startIndex + 0);

				wallTriangles.Add (startIndex + 3);
				wallTriangles.Add (startIndex + 2);
				wallTriangles.Add (startIndex + 0);

				wallTriangles.Add (startIndex + 0);
				wallTriangles.Add (startIndex + 1);
				wallTriangles.Add (startIndex + 3);


			}
		}
		wallMesh.vertices = wallVertices.ToArray ();
		wallMesh.triangles = wallTriangles.ToArray();
		walls.mesh = wallMesh;
	}


	// long af
	void TriangulateSquare(Square sq) {
		// 16 configs 
		switch (sq.config) {
		case 0:
			break;
		// 1 point cases:
		case 1:
			MeshFromPoints (sq.leftMid, sq.bottomMid, sq.bottomLeft);
			break;
		case 2:
			MeshFromPoints (sq.bottomRight, sq.bottomMid, sq.rightMid);
			break;
		case 4:
			MeshFromPoints (sq.topRight, sq.rightMid, sq.topMid);
			break;
		case 8:
			MeshFromPoints (sq.topLeft, sq.topMid, sq.leftMid);
			break;

		// 2 point cases
		case 3:
			MeshFromPoints (sq.rightMid, sq.bottomRight, sq.bottomLeft, sq.leftMid);
			break;
		case 6:
			MeshFromPoints (sq.topMid, sq.topRight, sq.bottomRight, sq.bottomMid);
			break;
		case 9:
			MeshFromPoints (sq.topLeft, sq.topMid, sq.bottomMid, sq.bottomLeft);
			break;
		case 12:
			MeshFromPoints (sq.topLeft, sq.topRight, sq.rightMid, sq.leftMid);
			break;
		case 5:
			MeshFromPoints (sq.topMid, sq.topRight, sq.bottomMid, sq.bottomLeft, sq.leftMid);
			break;
		case 10:
			MeshFromPoints (sq.topLeft, sq.topMid, sq.rightMid, sq.bottomRight, sq.bottomMid, sq.leftMid);
			break;

		// 3 point cases
		case 7:
			MeshFromPoints (sq.topMid, sq.topRight, sq.bottomRight, sq.bottomLeft, sq.leftMid);
			break;
		case 11:
			MeshFromPoints (sq.topLeft, sq.topMid, sq.rightMid, sq.bottomRight, sq.bottomLeft);
			break;
		case 13:
			MeshFromPoints (sq.topLeft, sq.topRight, sq.rightMid, sq.bottomMid, sq.bottomLeft);
			break;
		case 14:
			MeshFromPoints (sq.topLeft, sq.topRight, sq.bottomRight, sq.bottomMid, sq.leftMid);
			break;

		// 4 points case
		case 15:
			MeshFromPoints (sq.topLeft, sq.topRight, sq.bottomRight, sq.bottomLeft);
			checkedVertices.Add (sq.topLeft.vertexIndex);
			checkedVertices.Add (sq.topRight.vertexIndex);
			checkedVertices.Add (sq.bottomRight.vertexIndex);
			checkedVertices.Add (sq.bottomLeft.vertexIndex);
			break;
			
		}
		
	}

	
	void MeshFromPoints(params Midpoint[] points) {
		AssignVertices (points);

		// if we have 3 or more points to make a triangle with
		if (points.Length >= 3)
			CreateTriangle (points [0], points [1], points [2]);
		if (points.Length >= 4)
			CreateTriangle (points [0], points [2], points [3]);
		if (points.Length >= 5)
			CreateTriangle (points [0], points [3], points [4]);
		if (points.Length >= 6)
			CreateTriangle (points [0], points [4], points [5]);
	}

	void AssignVertices(Midpoint[] points) {
		for (int i = 0; i < points.Length; i++) {
			// check if its been assigned
			// we assign all vertexIndex to -1 at Midpoint creation
			if (points [i].vertexIndex == -1) {
				points [i].vertexIndex = vertices.Count;
				vertices.Add (points [i].position);
			}
		}
	}

	void CreateTriangle(Midpoint a, Midpoint b, Midpoint c) {
		triangles.Add(a.vertexIndex);
		triangles.Add(b.vertexIndex);
		triangles.Add(c.vertexIndex);

		Triangle triangle = new Triangle (a.vertexIndex, b.vertexIndex, c.vertexIndex);
		AddTriangleToDictionary (triangle.vertexIndexA, triangle);
		AddTriangleToDictionary (triangle.vertexIndexB, triangle);
		AddTriangleToDictionary (triangle.vertexIndexC, triangle);
	}

	void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle) {
		if (triangleDict.ContainsKey (vertexIndexKey)) {
			triangleDict [vertexIndexKey].Add (triangle);
		} else {
			List<Triangle> triangleList = new List<Triangle> ();
			triangleList.Add (triangle);
			triangleDict.Add (vertexIndexKey, triangleList);
		}

	}

	void CalculateMeshOutlines() {

		for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++) {
			if (!checkedVertices.Contains (vertexIndex)) {
				int newOutlineVertex = GetConnectedOutlineVertex (vertexIndex);
				if (newOutlineVertex != -1) {
					checkedVertices.Add (vertexIndex);

					List<int> newOutline = new List<int> ();
					newOutline.Add (vertexIndex);
					outlines.Add (newOutline);
					FollowOutline (newOutlineVertex, outlines.Count - 1);
					outlines [outlines.Count - 1].Add (vertexIndex);

				}
			}
		}
	}

	void FollowOutline (int vertexIndex, int outlineIndex) {
		outlines [outlineIndex].Add (vertexIndex);
		checkedVertices.Add(vertexIndex);
		int nextVertexIndex = GetConnectedOutlineVertex (vertexIndex);

		if (nextVertexIndex != -1) {
			FollowOutline (nextVertexIndex, outlineIndex);
		}			
	}

	int GetConnectedOutlineVertex(int vertexIndex) {
		List<Triangle> trianglesContainingVertex = triangleDict [vertexIndex];

		for (int i = 0; i < trianglesContainingVertex.Count; i++) {
			Triangle triangle = trianglesContainingVertex [i];

			for (int j = 0; j < 3; j++) {
				int vertexB = triangle [j];
				if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)) {	
					if (IsOutlineEdge (vertexIndex, vertexB)) {
						return vertexB;
					}
				}
			}
		}
		return -1;
	}



	bool IsOutlineEdge(int vertexA, int vertexB) {
		List<Triangle> vertexA_triangles = triangleDict [vertexA];
		int sharedTrianglesCount = 0;

		for (int i = 0; i < vertexA_triangles.Count; i++) {
			if (vertexA_triangles [i].Contains (vertexB)) {
				sharedTrianglesCount++;
				if (sharedTrianglesCount > 1) {
					break;
				}
			}
		}
		return sharedTrianglesCount == 1;
	}

	struct Triangle {
		public int vertexIndexA;
		public int vertexIndexB;
		public int vertexIndexC;
		int[] vertices;


		public Triangle (int a, int b, int c) {
			vertexIndexA = a;
			vertexIndexB = b;
			vertexIndexC = c;

			vertices = new int[3];
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}

		public int this[int i] {
			get { return vertices [i]; }

		}

		public bool Contains(int vertexIndex) {
			return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
		}
	}

	#region SquareGrid class definition
	public class SquareGrid {
		
		public Square[,] squares;

		public SquareGrid(int[,] map, float squareSize) {
			int midpointCountX = map.GetLength(0);
			int midpointCountY = map.GetLength(1);
			float mapWidth = midpointCountX * squareSize;
			float mapHeight = midpointCountY * squareSize;

			CornerPoint[,] cornerPoints = new CornerPoint[midpointCountX, midpointCountY];

			for(int x = 0; x < midpointCountX; x++) {
				for (int y = 0; y < midpointCountY; y++) {
					Vector3 pos = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, 0, -mapHeight/2 + y * squareSize + squareSize/2);
					cornerPoints[x, y] = new CornerPoint(pos, map[x,y] == 1, squareSize);
				}
			}

			squares = new Square[midpointCountX - 1, midpointCountY - 1];
			for(int x = 0; x < midpointCountX - 1; x++) {
				for (int y = 0; y < midpointCountY - 1; y++) {
					squares[x, y] = new Square(cornerPoints[x,y+1], cornerPoints[x+1,y+1], cornerPoints[x+1,y], cornerPoints[x,y]);
				}
			}
				

		}
	}
	#endregion



	#region Square class definition
	public class Square {

		public CornerPoint topLeft, topRight, bottomRight, bottomLeft;
		public Midpoint topMid, rightMid, bottomMid, leftMid;
		// one of the 16 different configurations for the cornerPoints
		// where cornerpoints are 1 bit of a 4bit binary #
		public int config;

		public Square(CornerPoint _topLeft, CornerPoint _topRight, CornerPoint _bottomRight, CornerPoint _bottomLeft) {
			topLeft = _topLeft;
			topRight = _topRight;
			bottomRight = _bottomRight;
			bottomLeft = _bottomLeft;

			topMid = topLeft.right;
			rightMid = bottomRight.above;
			bottomMid = bottomLeft.right;
			leftMid = bottomLeft.above;

			if (topLeft.active)
				config += 8;
			if(topRight.active)
				config += 4;
			if(bottomRight.active)
				config += 2;
			if(bottomLeft.active)
				config += 1;
		}
	}
	#endregion


	#region Midpoint and Cornerpoint class definition
	public class Midpoint {

		public Vector3 position;
		public int vertexIndex = -1;
	

		public Midpoint(Vector3 _pos){
			position = _pos;
		}
	}

	public class CornerPoint : Midpoint {
		public bool active;
		public Midpoint above, right;

		public CornerPoint(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
			active = _active;
			above = new Midpoint(position + Vector3.forward * squareSize/2f);
			right = new Midpoint(position + Vector3.right * squareSize/2f);
		}
	}
	#endregion
}
