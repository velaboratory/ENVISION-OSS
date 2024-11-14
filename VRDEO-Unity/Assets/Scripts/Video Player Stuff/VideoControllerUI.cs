using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = VRPen.Debug;

public class VideoControllerUI : MonoBehaviour {

    public bool ignoreSliderLock;
    public float scrubRefreshFrequency;

    private float lastScrubLockTime = float.MinValue;
    
    public Slider visualSlider;
    public Slider grabbableSlider;

    private VideoController controller;

    private float lastSliderInput = 0;
    private bool sliderInputQueued = false;

    public Button playButton;
    public Button pauseButton;

    public Text leftTime;
    public Text rightTime;
    public Text canvasText;
    
    public GameObject grabSliderHandle;
    public GameObject visualSliderHandle;
    public RectTransform unlockedTime;
    
    //tabs
    public GameObject AnnotationTabPrefab;
    public Transform AnnotationTabParent;

    private void Start() {
        controller = GameManager.s_instance.videoControl;
        setCanvasText(false);
        InvokeRepeating(nameof(updateVideoTime), 1, scrubRefreshFrequency);
    }


    private void Update() {
        
        
        //make sure the right button is enabled
        if (controller.video.isPlaying) {
            playButton.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(true);
        }
        else {
            playButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
        }
        
        //set play button inactive if scrubbing
        if ((lastScrubLockTime + scrubRefreshFrequency) < Time.time) playButton.interactable = true;
        else playButton.interactable = false;
        
        //enable editable handle if selected
        // if (sliderInputQueued) {
        //     grabSliderHandle.SetActive(true);
        //     visualSliderHandle.SetActive(false);
        // }
        // else {
        //     grabSliderHandle.SetActive(false);
        //     visualSliderHandle.SetActive(true);
        // }
        if (controller.video.isPlaying) toggleSliderHandles(false);
        
        //set slider
        visualSlider.SetValueWithoutNotify(controller.getPercentTime());
        if (controller.video.isPrepared) {
            leftTime.text = floatToTextTime((float) controller.video.time);
            rightTime.text = floatToTextTime((float) controller.video.length);
            unlockedTime.anchorMin = new Vector2(controller.getVideoUnlockedTime(controller.video.url), 0);
        }
        else {
            leftTime.text = floatToTextTime(0);
            rightTime.text = floatToTextTime(0);
            unlockedTime.anchorMin = new Vector2(1, 0);
        }

    }

    void toggleSliderHandles(bool grabHandle) {
        grabSliderHandle.SetActive(grabHandle);
        visualSliderHandle.SetActive(!grabHandle);
    }

    void updateVideoTime() {
        if (sliderInputQueued) {
            sliderInputQueued = false;
            //Debug.Log(lastSliderInput.ToString());
            //updated pause loication
            pause(lastSliderInput);
            lastScrubLockTime = Time.time;
        }
    }

    public string floatToTextTime(float time) {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        return timeSpan.ToString("mm':'ss");
    }
    
    public void sliderInput(float percent) {
        if (!controller.video.isPrepared) return;
        if (!ignoreSliderLock && percent > controller.getVideoUnlockedTime()) {
            percent = controller.getVideoUnlockedTime();
            grabbableSlider.SetValueWithoutNotify(percent);
        }
        //Debug.Log("ge " + percent.ToString());
        lastSliderInput = percent;
        sliderInputQueued = true;
        toggleSliderHandles(true);
        
        //pause if playing
        //if (controller.video.isPlaying) pause();
    }

    public void play() {
        controller.play(true);
    }

    void pause(float percentTime) {
        //controller.pause(true);
        controller.pauseToNearbyAnnotation(percentTime);
    }
    
    public void pause() {
        //controller.pause(true);
        controller.pauseToNearbyAnnotation();
    }
    
    

    public void setCanvasText(bool canvasEn, byte canvasId = 0) {
        if (canvasEn) {
            canvasText.text = "Annotation: " +(canvasId - controller.minAnnotationCanvasId + 1);
            canvasText.color = Color.black;
        }
        else {
            canvasText.text = "Annotation: N/A";
            canvasText.color = Color.red;
        }
    }

    public void setAnnotationTabColor(byte canvasId, bool allOff = false) {
        
        //disable annotation tab colors
        foreach (VideoController.Annotation annotation in controller.annotations) {
            annotation.tab.setColor(false);
        }

        //set one to red
        if (!allOff) {
            AnnotationTab tab = controller.annotations.Find(x=> x.getCanvasId() == canvasId).tab;
            tab.setColor(true);
            tab.gameObject.transform.SetAsLastSibling();
        }
    }

    public void nextAnnotationBtn(bool right) {
        controller.pauseToNextAnnotation(right);
    }

}
