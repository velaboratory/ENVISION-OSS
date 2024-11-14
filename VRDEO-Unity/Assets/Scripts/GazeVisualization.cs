using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GazeVisualization : MonoBehaviour
{
    public OVREyeGaze gaze;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward,Mathf.Infinity, 0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            //this is where we hit in world space, useful for where to put the cursor
            //hit.point;

            //this is where we hit the object relative to the object, useful for logging translation-invariant locations
            hit.transform.InverseTransformPoint(hit.point);
        }
    }
}
