using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;

public class Pathfinding : MonoBehaviour
{
    // to call FinishedProcessingPath from the PathRequestManager, need reference to that calss
    PathRequestManager requestManager;
    Grid grid; // a Grid object called "grid" is created
    // public Transform seeker, target;
    // Start is called before the first frame update
    void Awake()
    {
        grid = GetComponent<Grid>(); // get the grid(Map) by finding the Grid in the A*(current game object)
        requestManager = GetComponent<PathRequestManager>();
    }


    // void Update() // update method is no longer required since FindPath is called from the Unit class
    // {
    //     FindPath(seeker.position, target.position);
    // }

    public void StartFindPath(Vector3 startPos, Vector3 targetPos) {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos) { // note this is changed to IEnumerator from void function to call from StartFindPath
        Stopwatch sw = new Stopwatch(); // to record cputime
        sw.Start();

        Vector3[] waypoints = new Vector3[0]; // for FinishedProcessingPath
        bool pathSuccess = false; // for FinishedProcessingPath

        Node startNode  = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if (startNode.walkable && targetNode.walkable) { // only do pathfinding both are walkable
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
                    pathSuccess = true;
                    // RetracePath(startNode, targetNode); // this is move to the end
                    break; // found the path (previously this was return, can't use return in IEnumerator, so just break the loop)
                }

                foreach (Node neighbour in grid.GetNeighbours(currentNode)) {
                    if (!neighbour.walkable || closedSet.Contains(neighbour)) {
                        continue; // not evaluate if not walkable and if it is in the closedSet
                    }

                    // if the new path gCost is lower
                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty; // GetDistance here is just one node movement to current to neighbour
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
        yield return null; // to make it wait for one frame before returning (Since this becomes IEnumerator)
        if (pathSuccess) {
            waypoints = RetracePath(startNode, targetNode);
            pathSuccess = waypoints.Length > 0;
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess); // to follow the path

    }

    // retrace the found path in reverse
    Vector3[] RetracePath(Node startNode, Node endNode) { // retrace the found path in reverse
        List<Node> path = new List<Node>();
        Node currentNode = endNode; // start with endNode
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;

        // grid.path = path; // send the path to the grid object to visualize in Gizmos
    }

    Vector3[] SimplifyPath(List<Node> path) {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++) {
            Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX, path[i-1].gridY - path[i].gridY);
            if (directionNew != directionOld) {
                waypoints.Add(path[i].WorldPosition); // if the direction change, add that Node worldposition to waypoints
            }
            directionOld = directionNew;
        }
        return waypoints.ToArray(); // list to array

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
