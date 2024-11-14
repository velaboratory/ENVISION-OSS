using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoScreenOff : MonoBehaviour
{
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
