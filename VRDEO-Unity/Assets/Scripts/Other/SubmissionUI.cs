using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRPen;
using Debug = VRPen.Debug;
using Logger = VelUtils.Logger;

public class SubmissionUI : MonoBehaviour {
    
    
    public Toggle logToggle;
    public Toggle recordingToggle;
    public Button confirmButton;
    public ProgressBar recordingProgress;
    public ProgressBar logProgress;

    private void Start() {
        //defauls
        logToggle.isOn = true;
        #if UNITY_ANDROID
        recordingToggle.isOn = true;
        #else
        recordingToggle.isOn = false;
        #endif
    }

    public void confirm() {
        StartCoroutine(uploadRoutine());
    }


    private void OnEnable() {
        resetVisuals();
        
        
        //allow buttons to be clicked again
        confirmButton.interactable = true;
        recordingToggle.interactable = true;
        logToggle.interactable = true;
    }
    

    public IEnumerator uploadRoutine() {

        
        confirmButton.interactable = false;
        recordingToggle.interactable = false;
        logToggle.interactable = false;
        bool uploadLog = logToggle.isOn;
        bool uploadRecording = recordingToggle.isOn;
        Debug.Log("Began submission upload routine --- Log = " + uploadLog+ ", Recording = " + uploadRecording);
        
        
        //init
        if (uploadLog) Logger.UploadZip(false);
        if (uploadRecording) UploadVideo.s_instance.upload();
        
        
        
        //update bars and wait for completion
        while ((uploadLog && Logger.uploading) || (uploadRecording && UploadVideo.s_instance.uploading)) {

            //log
            if (uploadLog) {
                if (Logger.uploading) logProgress.setProgress(Logger.uploadWWW.uploadProgress);
                else if (Logger.lastUploadSucceeded) logProgress.success();
                else  logProgress.error();
            }

            //recording
            if (uploadRecording) {
                if (UploadVideo.s_instance.uploading) recordingProgress.setProgress(UploadVideo.s_instance.getUploadPercent());
                else if (UploadVideo.s_instance.lastUploadWasSuccessful) recordingProgress.success();
                else  recordingProgress.error();
            }
            
            //wait
            yield return null;
        }
        if (uploadLog) {
            //log
            if (Logger.lastUploadSucceeded) logProgress.success();
            else  logProgress.error();
        }
        if (uploadRecording) {
            //recording
            if (UploadVideo.s_instance.lastUploadWasSuccessful) recordingProgress.success();
            else  recordingProgress.error();
        }
        
        
    }


    void resetVisuals() {
        recordingProgress.setProgress(0);
        logProgress.setProgress(0);
    }
    
    
}
