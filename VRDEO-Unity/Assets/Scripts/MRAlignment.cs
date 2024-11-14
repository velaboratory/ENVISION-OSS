using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VelUtils;

public class MRAlignment : MonoBehaviour
{
    public GameObject alignmentPointOne;
    public GameObject alignmentPointTwo;

    public GameObject leftTouchPoint;
    public GameObject rightTouchPoint;

    public GameObject rig;

    private bool firstClick = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        //"anchor"
        //if value of rstlyusForce > 0.3 (pressure is high, being touched)
        var leftAlign = InputMan.Button2Up(Side.Left);
        var rightAlign = InputMan.Button2Up(Side.Right);

        if (leftAlign && !firstClick) //on release of button
        {
            alignmentPointOne.transform.SetPositionAndRotation(leftTouchPoint.transform.position, alignmentPointOne.transform.rotation);
            firstClick = true;
        } 
        else if (rightAlign && !firstClick)
        {
            alignmentPointOne.transform.SetPositionAndRotation(rightTouchPoint.transform.position, alignmentPointOne.transform.rotation);
            firstClick = true;
        }
        else if (leftAlign)
        {
            alignmentPointTwo.transform.SetPositionAndRotation(leftTouchPoint.transform.position, alignmentPointTwo.transform.rotation);
            TwoPointAlignment(alignmentPointOne.transform, alignmentPointTwo.transform, rig.transform);
            firstClick = false;

        } else if (rightAlign)
        {
            alignmentPointTwo.transform.SetPositionAndRotation(rightTouchPoint.transform.position, alignmentPointTwo.transform.rotation);
            TwoPointAlignment(alignmentPointOne.transform, alignmentPointTwo.transform, rig.transform);
            firstClick = false;
        }

    }

    private void TwoPointAlignment(Transform t1, Transform t2, Transform t3)
    {
        Vector3 forward = t1.position - t2.position;
        float differenceBetweenPhysicalAndWorld = Vector3.SignedAngle(forward, Vector3.forward, Vector3.up);
        t3.Rotate(new Vector3(0, differenceBetweenPhysicalAndWorld, 0), Space.World);
        t3.position -= t1.position;
    }
}
