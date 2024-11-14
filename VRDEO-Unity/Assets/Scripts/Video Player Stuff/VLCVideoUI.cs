using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VLCVideoUI : MonoBehaviour {

#if UNITY_WINDOWS
    private VLCVideo video;

    private void Awake() {
        video = FindObjectOfType<VLCVideo>();
    }

    public void playToggle() {
        
    }

    public void setTimePercent(float percent) {
        video.setPercentTime(percent);
    }
#endif
}
