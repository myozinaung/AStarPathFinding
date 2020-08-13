using System.Collections;
using System.Collections.Generic; // to used Queue
using UnityEngine;
using System; // to use Action (the callback to follow the path after obtaining a successful path)

public class PathRequestManager : MonoBehaviour
{

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest; // currently processing PathRequest

    static PathRequestManager instance; // own class instance to access the RequestPath static function

    Pathfinding pathfinding; // reference to the pathfinding calss to use it from this script
    bool isProcessing;
    void Awake() {
        instance = this; // instance = this instnace of class
        pathfinding = GetComponent<Pathfinding>(); // obtain the Pathfinding class from the Game Object
    }
    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback) {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback); // create a PathRequest struct using the request variables from the seeker
        instance.pathRequestQueue.Enqueue(newRequest); // the request is added to the Queue
        instance.TryProcessNext(); // try to process the path request (if there is a currently processing request, wait)
    }

    void TryProcessNext() {
        if (!isProcessing && pathRequestQueue.Count > 0) {
            currentPathRequest = pathRequestQueue.Dequeue(); // take out the path request from Queue to process
            isProcessing = true;
            pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
        }
    }

    public void FinishedProcessingPath(Vector3[] path, bool success) { // this one is called in the Pathfinding class
        currentPathRequest.callback(path, success); // after getting the path execute the callback defined in Unit calss
        isProcessing = false;
        TryProcessNext();
    }

    struct PathRequest { // data structure to store all the information passed to the RequestPath function
    // cannot use the normal indivitual variables since there will be many requests from many seeker
    // These requests will be store in a Queue, and process the requests in different frames to distribute the computational load
         public Vector3 pathStart;
         public Vector3 pathEnd;
         public Action<Vector3[], bool> callback; 

         public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback) { // constructor
             pathStart = _start;
             pathEnd   = _end;
             callback  = _callback;
         }
    }

}
