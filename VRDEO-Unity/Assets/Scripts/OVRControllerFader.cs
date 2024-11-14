using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRControllerFader : MonoBehaviour
{
    public List<GameObject> fadable;
    public Material faded;
    public Material unfaded;

    public void Fade()
    {
        //swap the material for faded material
        fadable.ForEach((o) => { o.GetComponentInChildren<Renderer>().material = faded; });
    }

    public void Unfade()
    {
        //swap back to the original unfaded material
        fadable.ForEach((o) => { o.GetComponent<Renderer>().material = unfaded; });
    }
}
