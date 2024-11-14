using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VelNet;

public class RoomSelect : MonoBehaviour {
    
    public Text currentRoomText;
    public Text enteredText;
    private bool joinedRoom = false;

    public static RoomSelect s_instance;

    private void Awake() {
        s_instance = this;
    }


    private void Start() {
        currentRoomText.text = "N/A";
    }

    private void Update() {
        if (!joinedRoom && VelNetManager.InRoom) {
            joinedRoom = true;
            currentRoomText.text = VelNetManager.Room;
        }
    }

    public void addText(string text) {
        
        if (enteredText.text.Length < 4) {
            
            //ignore addtiional leading zeros
            if (enteredText.text.Equals("0")) {
                enteredText.text = "";
            }
            //actually add text
            enteredText.text += text;
        }
    }

    public void go() {

        if (enteredText.text.Length == 0) return;
        Debug.LogError("go()ing for some reason");
        //set room name
        VelNetNetworkMan.s_instance.roomToJoin = enteredText.text;
        
        //disconnect
        //VelNetManager.Leave();
        IOSRecording.s_instance?.stopRecording();
        
        //reset callback for velnet, no idea why i need to do this (it fixed some errors tho :/)
        VelNetManager.OnLoggedIn = null;
        VelNetManager.OnPlayerLeft = null;
        VelNetManager.OnConnectedToServer = null;
        VelNetManager.OnJoinedRoom = null;
        VelNetManager.OnLeftRoom = null;
        VelNetManager.OnPlayerJoined = null;
        VelNetManager.CustomMessageReceived = null;
        VelNetManager.RoomDataReceived = null;
        
        //reload
        SceneManager.LoadScene("Main");
    }

    public void go(string text) {
        enteredText.text = text;
        go();
    }
    
    public void backspace() {
        
        if (enteredText.text.Length == 0) return;
        enteredText.text = enteredText.text.Substring(0, enteredText.text.Length - 1);
    }
    
}
