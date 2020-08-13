using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    const float minPathUpdateTime = 0.2f; // only update path after this duration has passed between update
    const float pathUpdateMoveThreshold = 0.1f; // only update path if the target movement is less than this
    public Transform target;
    public float speed = 20;
    Vector3[] path;
    int targetIndex;

    void Start() {
        // PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
        // request the path from the path finder
        // after requesting, execute the callback "OnPathFound" (OnPathFound = to Follow the path)
        // executing callback is done in FinishedProcessingPath of PathRequestManager 
        // but callback function to execute is defined here and passed to the PathRequestManager.RequestPath as Action variable

        StartCoroutine(UpdatePath());
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful) {
        if (pathSuccessful) {
            path = newPath;
            StopCoroutine("FollowPath"); // to make sure it is not already running
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator UpdatePath() { // to do path request and following again if the target moves
        
        if (Time.timeSinceLevelLoad < 0.3f) { // start the path finding and updating only after 0.3s of level load
            yield return new WaitForSeconds(0.3f);
        }
        PathRequestManager.RequestPath(transform.position, target.position, OnPathFound); // to call for the first time
        
        float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
        Vector3 targetPosOld = target.position;
        while (true) {
            yield return new WaitForSeconds(minPathUpdateTime);
            if ((target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
                PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
                targetPosOld = target.position;
            }
            
        }
    }

    IEnumerator FollowPath () {
        Vector3 currentWaypoint = path[0]; // first waypoint in path

        while (true) {
            if (transform.position == currentWaypoint) { // if the seeker reaches the current waypoint, go to next waypoint
                targetIndex++;
                if (targetIndex >= path.Length) { // if the final waypoint is reached, stop.
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed*Time.deltaTime);
            yield return null;
        }
    }

    public void OnDrawGizmos() {
        if (path != null) {
            for (int i = targetIndex; i < path.Length; i++) { // start from the seeker current position
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one*0.5f);

                if (i ==  targetIndex) {
                    Gizmos.DrawLine(transform.position, path[i]); // first line (from seeker to current seeking waypoints)
                }
                else {
                    Gizmos.DrawLine(path[i-1], path[i]); // other lines between waypoints
                }
            }
        }

    }

}
