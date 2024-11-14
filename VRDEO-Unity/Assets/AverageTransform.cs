using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AverageTransform : MonoBehaviour
{
    public Transform t1;
    public Transform t2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = (t1.position + t2.position) / 2; //midpoint between the two positions
        transform.rotation = Quaternion.RotateTowards(t1.rotation, t2.rotation, Quaternion.Angle(t1.rotation, t2.rotation) / 2);
    }
}
