using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = VRPen.Debug;

public class PicoCalibrationMan : MonoBehaviour {
    private bool readyToSwitchScenes = false;
    
    // Start is called before the first frame update
    void Update() {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.GetKeyUp(KeyCode.Joystick1Button0)) {
            bool fail = false;
            string bundleId = "com.tobii.usercalibration.neo3"; // your target bundle id
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");

            AndroidJavaObject launchIntent = null;
            try {
                launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleId);
            }
            catch (System.Exception e) {
                fail = true;
            }

            if (fail) {
                //open app in store
                Application.OpenURL("https://google.com");
            }
            else //open the app
                ca.Call("startActivity", launchIntent);

            up.Dispose();
            ca.Dispose();
            packageManager.Dispose();
            launchIntent.Dispose();
            
            readyToSwitchScenes = true;
        }
        #endif

    }
    
    

    private void OnApplicationFocus(bool focus) {
        Debug.Log("focus: " +focus);
        if (focus && readyToSwitchScenes) {
            readyToSwitchScenes = false;
            Invoke(nameof(loadScene), .1f);
        }
    }

    void loadScene() {
        SceneManager.LoadScene("Main");
    }

}