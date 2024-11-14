using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //version
        gameObject.GetComponent<TextMeshPro>().text = "v " + Application.version;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
