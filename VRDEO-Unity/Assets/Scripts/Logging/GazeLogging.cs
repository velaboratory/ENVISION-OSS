using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeLogging : MonoBehaviour
{
    Transform gazeTrackerPoint;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {


        //gaze track
        //set visuals
        //gazeTrackerPoint.position = transform.position + transform.forward.normalized;
        //gazeTrackingDebug.text = "no hit";

        //check collision
        Ray gazeRay = new Ray(transform.position, transform.forward);
        LayerMask gazeLm = LayerMask.GetMask("EyeTrack2D");
        RaycastHit[] gazeHits = Physics.RaycastAll(gazeRay, 100);
        foreach (var hit in gazeHits)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("EyeTrack2D"))
            {
                Vector3 pos = hit.collider.transform.InverseTransformPoint(hit.point);
                float x = pos.x + .5f;
                float y = pos.y + .5f;
                VRDEOLogging.s_instance.eyeTrack2DLog(false, hit.collider.transform.parent, x, y, gazeRay.origin, gazeRay.direction, hit.distance, gameObject.name);
            }
        }

        VRDEOLogging.s_instance.logEyeTrackObjects(gazeRay.origin, gazeRay.direction, gameObject.name);
    }
}
