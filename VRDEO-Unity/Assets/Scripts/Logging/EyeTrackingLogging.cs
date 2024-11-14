using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;

public class EyeTrackingLogging : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //eyetracking
        //check collision
        Ray eyeRay = new Ray(transform.position, transform.forward);
        //LayerMask eyeLm = LayerMask.GetMask("EyeTrack2D");
        RaycastHit[] eyeHits = Physics.RaycastAll(eyeRay, 100);
        foreach (var hit in eyeHits)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("EyeTrack2D"))
            {
                Vector3 pos = hit.collider.transform.InverseTransformPoint(hit.point);
                float x = pos.x + .5f;
                float y = pos.y + .5f;
                VRDEOLogging.s_instance.eyeTrack2DLog(true, hit.collider.transform.parent, x, y, eyeRay.origin, eyeRay.direction, hit.distance, gameObject.name);
            }
        }

        VRDEOLogging.s_instance.logEyeTrackObjects(eyeRay.origin, eyeRay.direction, gameObject.name);
    }
}
