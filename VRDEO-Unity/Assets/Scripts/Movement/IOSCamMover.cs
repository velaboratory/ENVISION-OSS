using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOSCamMover : MonoBehaviour
{

    public Camera cam;

    public Vector3 sideBySideTargetRight;
    public Vector3 sideBySideTargetLeft;
    public Vector3 videoTarget;
    public Vector3 pdfTarget;

    void Update()
    {
        #if UNITY_IOS && !UNITY_EDITOR
        updateTarget();
        #endif
    }

    void updateTarget() {

        Vector3 target;

        if (Screen.orientation == ScreenOrientation.LandscapeLeft) target = sideBySideTargetLeft;
        else if (Screen.orientation == ScreenOrientation.LandscapeRight) target = sideBySideTargetRight;
        else {
            if (GameManager.s_instance.currentWhiteboardId == 1) target = videoTarget;
            else target = pdfTarget;
        }

        cam.gameObject.transform.position = Vector3.Lerp(cam.gameObject.transform.position, target, .2f);
    }
}
