using System.Collections;
using UnityEngine;

// A class defining the structure of a node
// with  constructor that assign the parameters (walkable and position)
public class Node : IHeapItem<Node> // Node need to implement IHeapItem interface to used Node items in Heap "Heap<Node>"
{

    public bool walkable;
    public Vector3 WorldPosition;

    public int gridX, gridY; // position in the grid (start from Left-Bottom), Not world position
    public int gCost;
    public int hCost;
    public Node parent;
    int heapIndex;

    public Node(bool _walkable, Vector3 _WorldPosition, int _gridX, int _gridY)
    {
        walkable      = _walkable;
        WorldPosition = _WorldPosition;
        gridX         = _gridX;
        gridY         = _gridY;
    }

    public int fCost {
        get {
            // return Mathf.RoundToInt(gCost + 1*hCost);
            return gCost + hCost;
        }
    }

    // Add funtions needed to implement IHeapItem interface (need only Heap is used)
    public int HeapIndex {
        get {
            return heapIndex;
        }
        set {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare) {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0) { // if fCost are equal
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare; 
    }

}
