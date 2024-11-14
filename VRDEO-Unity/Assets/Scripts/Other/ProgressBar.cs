using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = VRPen.Debug;

public class ProgressBar : MonoBehaviour {

    public int barWidth;

    public RectTransform whiteBar;
    public RectTransform greenBar;
    public RectTransform redBar;


    public void success() {
        whiteBar.gameObject.SetActive(false);
        greenBar.gameObject.SetActive(true);
        redBar.gameObject.SetActive(false);
    }

    public void error() {
        whiteBar.gameObject.SetActive(false);
        greenBar.gameObject.SetActive(false);
        redBar.gameObject.SetActive(true);
    }

    public void setProgress(float progress) {

        if (progress < 0) progress = 0;
        
        whiteBar.gameObject.SetActive(true);
        greenBar.gameObject.SetActive(false);
        redBar.gameObject.SetActive(false);

        whiteBar.localPosition = new Vector3(barWidth * progress / 2f- barWidth/2f, 0, 0);
        whiteBar.sizeDelta = new Vector2(barWidth * progress, whiteBar.sizeDelta.y);

    }

}
