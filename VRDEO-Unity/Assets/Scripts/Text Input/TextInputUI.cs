using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = VRPen.Debug;

public class TextInputUI : MonoBehaviour {

    public HiddenInputField input;
    public Button selectBtn;
    private int index;


    public void init(int id) {
        index = id;
    }
    
    private void Start() {
        selectBtn.onClick.RemoveAllListeners(); //need to do this since vrpen auto adds the highlight timer
        selectBtn.onClick.AddListener(@select); //which is an issue since it pulls selection from the input field
    }

    public void add() {
        
        if (input.text.Length == 0) return;
        
        //make stamp
        textStamp();
        clear();
        GameManager.s_instance.disableTextInputUI(index);
    }

    public void select() {
        
        //make sure its selected
        input.Select();
    }

    public void clear() {
        
        //clear feidl
        input.text = "";
    }
    
    void textStamp() {
        GameManager.s_instance.whiteboards[GameManager.s_instance.currentWhiteboardId].UIMan.stampPassthrough(input.text, Color.black);
        input.text = "";
    }

}
