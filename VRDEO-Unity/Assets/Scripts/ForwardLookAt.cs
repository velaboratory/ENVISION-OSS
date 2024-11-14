using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForwardLookAt : MonoBehaviour
{
    [SerializeField] private Transform m_lookat;
    
    // Update is called once per frame
    void Update()
    {
        transform.LookAt(m_lookat, Vector3.forward);
    }
}
