using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

// Grid is a 2D array of Nodes(defined by Node class)
public class Grid : MonoBehaviour
{
    public bool displayGridGizmos;

    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius = 0.5f;
    public TerrainType[] walkableRegions;
    public int obstacleProximityPenalty = 10;
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    Node[,] grid; // a 2D array of Nodes called grid

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
	int penaltyMax = int.MinValue;
    void Awake() 
    // void FixedUpdate() // MYO, to update the map if obstacles move
    // better to update only the movement of obsatcle detected
    {
        nodeDiameter = nodeRadius*2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);

        foreach (TerrainType region in walkableRegions) {
            walkableMask.value |= region.terrainMask.value; // bitwise addition
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value,2),region.terrainPenalty);                
        }

        Stopwatch sw = new Stopwatch(); // to record cputime
        sw.Start();
        CreateGrid();
        sw.Stop();
        print(" Grid Generation Time: " + sw.ElapsedMilliseconds + " ms.");
    }

    public int MaxSize {
        get {
            return gridSizeX*gridSizeY;
        }
    }

    // Create the grid map using user-defined Grid Size and resolution
    // Do the collision check with the obstacles to define unwalkable area in the grid
    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right*gridWorldSize.x/2 - Vector3.forward*gridWorldSize.y/2;

        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeX; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right*(x*nodeDiameter + nodeRadius) + Vector3.forward*(y*nodeDiameter +nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint,nodeRadius,unwalkableMask));
                
                int movementPenalty = 0;
                // Raycast to find the objects in Road layer
                
                Ray ray = new Ray(worldPoint + Vector3.up*50, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100, walkableMask)) { // do 100m Raycast to the walkableMask with the "ray" , and store the result in "hit"
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }

                if (!walkable) { // to increase the movementPenalty near the obstacles/at obstacles
                    movementPenalty += obstacleProximityPenalty;
                }
                

                grid[x,y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }
        BlurPenaltyMap(10);
    }

    void BlurPenaltyMap(int blurSize) { // not study in detail yet // copied 
		int kernelSize = blurSize * 2 + 1;
		int kernelExtents = (kernelSize - 1) / 2;

		int[,] penaltiesHorizontalPass = new int[gridSizeX,gridSizeY];
		int[,] penaltiesVerticalPass = new int[gridSizeX,gridSizeY];

		for (int y = 0; y < gridSizeY; y++) {
			for (int x = -kernelExtents; x <= kernelExtents; x++) {
				int sampleX = Mathf.Clamp (x, 0, kernelExtents);
				penaltiesHorizontalPass [0, y] += grid [sampleX, y].movementPenalty;
			}

			for (int x = 1; x < gridSizeX; x++) {
				int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
				int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX-1);

				penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - grid [removeIndex, y].movementPenalty + grid [addIndex, y].movementPenalty;
			}
		}
			
		for (int x = 0; x < gridSizeX; x++) {
			for (int y = -kernelExtents; y <= kernelExtents; y++) {
				int sampleY = Mathf.Clamp (y, 0, kernelExtents);
				penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
			}

			int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
			grid [x, 0].movementPenalty = blurredPenalty;

			for (int y = 1; y < gridSizeY; y++) {
				int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
				int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY-1);

				penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y-1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
				blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
				grid [x, y].movementPenalty = blurredPenalty;

				if (blurredPenalty > penaltyMax) {
					penaltyMax = blurredPenalty;
				}
				if (blurredPenalty < penaltyMin) {
					penaltyMin = blurredPenalty;
				}
			}
		}

	}

    // Function to find the neighbourNodes of the currentNode
    public List<Node> GetNeighbours(Node node) { // use List since no. of neighbour is unknown
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++) { // search neighbours in 3x3 matrix centered with current node
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) // skip the own node(current node)
                    continue;
                
                int checkX = node.gridX + x; // get the position in grid
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) { // check if the positon is inside the grip map
                    neighbours.Add(grid[checkX,checkY]);
                }
            }
        }
        return neighbours;
    }

    // Function to convert Unity World Position to the Node point in the grid map
    public Node NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
        // if the player(start node) is outside of the Map(grid) --> error
        // to avoid that clamp between 0 and 1
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        // index of grid of the player
        int x = Mathf.RoundToInt((gridSizeX-1) * percentX); // -1 since array index starts with 0
        int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
        return grid[x,y];
    }


	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y));
		if (grid != null && displayGridGizmos) {
			foreach (Node n in grid) {

				Gizmos.color = Color.Lerp (Color.white, Color.black, Mathf.InverseLerp (penaltyMin, penaltyMax, n.movementPenalty));
				Gizmos.color = (n.walkable)?Gizmos.color:Color.red;
				Gizmos.DrawCube(n.WorldPosition, Vector3.one * (nodeDiameter));
			}
		}
	}

    [System.Serializable] // to let user specify the layer that has same penalty cost
    public class TerrainType {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
}
