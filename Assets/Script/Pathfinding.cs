using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class Pathfinding : MonoBehaviour
{
    Grid grid; // a Grid object called "grid" is created
    public Transform seeker, target;
    // Start is called before the first frame update
    void Awake()
    {
        grid = GetComponent<Grid>(); // get the grid(Map) by finding the Grid in the A*(current game object)
    }

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetButtonDown("Jump")) {
            FindPath(seeker.position, target.position);
        // }
        
    }

    void FindPath(Vector3 startPos, Vector3 targetPos) {
        Stopwatch sw = new Stopwatch(); // to record cputime
        sw.Start();

        Node startNode  = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        // List<Node> openSet      = new List<Node>(); // using built-in List (in efficient)
        Heap<Node> openSet = new Heap<Node>(grid.MaxSize); // using Heap sort
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // ****Assign the currentNode with lowest fCost Node in openSet *****//
            // Uisng List
            // Node currentNode = openSet[0]; // initialize the currentNode
            // for (int i = 1; i < openSet.Count; i++) { // start with 1 since 0 index is already assigned above
            //     if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost) {
            //         // if fCost are equal, compare the hCost
            //         currentNode = openSet[i];
            //     }
            // }
            // openSet.Remove(currentNode);

            // Uisng Heap
            Node currentNode = openSet.RemoveFirst(); // Just one line

            closedSet.Add(currentNode);

            if (currentNode == targetNode) {
                sw.Stop();
                print("Path Found:" + sw.ElapsedMilliseconds + " ms. " + "fCost: " + targetNode.fCost);
                RetracePath(startNode, targetNode);
                return; // found the path
            }

            foreach (Node neighbour in grid.GetNeighbours(currentNode)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) {
                    continue; // not evaluate if not walkable and if it is in the closedSet
                }

                // if the new path gCost is lower
                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour); // GetDistance here is just one node movement to current to neighbour
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost  = newMovementCostToNeighbour;
                    neighbour.hCost  = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                    else
                        openSet.UpdateItem(neighbour); 
                }
            }
        }

    }

    // retrace the found path in reverse
    void RetracePath(Node startNode, Node endNode) { // retrace the found path in reverse
        List<Node> path = new List<Node>();
        Node currentNode = endNode; // start with endNode
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        grid.path = path; // send the path to the grid object to visualize in Gizmos
    }
    // shortest distance between 2 nodes if there is no obstacles
    // Left/Righ/Up/Down movement = 10
    // diagonal distance(cost) = 14 (=10*sqrt(2))
    int GetDistance(Node nodeA, Node nodeB) {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        int D1 = 10; // cost for lateral movement
        int D2 = 14; // cost for diagonal movement
        if (distX > distY) {
            return D2*distY + D1*(distX - distY);
        }
        return D2*distX + D1*(distY - distX);

        // return Mathf.RoundToInt(10*Mathf.Sqrt(distX^2 + distY^2));
    }

    int GetDistance2(Node nodeA, Node nodeB) { // by MYO (Euclidean distance)
        int distX = 10*Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = 10*Mathf.Abs(nodeA.gridY - nodeB.gridY);

        return Mathf.RoundToInt(Mathf.Sqrt(distX^2 + distY^2));
    }

    int GetDistance3(Node nodeA, Node nodeB) { // Manhattan distance
        int distX = 10*Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = 10*Mathf.Abs(nodeA.gridY - nodeB.gridY);

        return distX + distY;
    }

    int GetDistance4(Node nodeA, Node nodeB) { // Diagonal distance
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        int D1 = 10; // cost for lateral movement
        int D2 = 14; // cost for diagonal movement
        if (distX > distY)
            return D1*(distX + distY) + ((D2 - 2)*D1)* distX;
        return D1*(distX + distY) + ((D2 - 2)*D1)*distY;
    }
}
