using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

// Grid is a 2D array of Nodes(defined by Node class)
public class Grid : MonoBehaviour
{
    public bool onlyDisplayPathGizmos;
    public Transform player;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius = 0.5f;
    Node[,] grid; // a 2D array of Nodes called grid

    float nodeDiameter;
    int gridSizeX, gridSizeY;
    // void Start() 
    void FixedUpdate() // MYO, to update the map if obstacles move
    // better to update only the movement of obsatcle detected
    {
        nodeDiameter = nodeRadius*2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
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
                grid[x,y] = new Node(walkable, worldPoint, x, y);
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

    public List<Node> path;
    void OnDrawGizmos() 
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        
        if (onlyDisplayPathGizmos) {
            if (path != null) {
                foreach (Node n in path) {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(n.WorldPosition, Vector3.one*(nodeDiameter-0.01f));
                }
            }
        }
        else {
            if (grid != null) {
                Node playerNode = NodeFromWorldPoint(player.position);
                foreach (Node n in grid) {
                    Gizmos.color = (n.walkable)?Color.white:Color.red;
                    if (path != null) {
                        if (path.Contains(n))
                            Gizmos.color = Color.black;
                    }
                    Gizmos.DrawCube(n.WorldPosition, Vector3.one*(nodeDiameter-0.01f));
                }
            }
        }    
    }

}
