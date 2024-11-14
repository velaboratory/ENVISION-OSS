using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PdfUIOption : MonoBehaviour {
    
    public Text text;
    public Text timeText;
    public GameObject lockObj;
    public Button btn;

    public bool isLocked = true;

    private void Awake() {
        //set default color
        setColor(false);
    }

    public void unlockOption() {
        isLocked = false;
        btn.interactable = true;
        timeText.gameObject.SetActive(false);
        lockObj.SetActive(false);
    }

    public void setColor(bool green) {
        ColorBlock colorBlock = btn.colors;
        if (green) colorBlock.normalColor = new Color(.3f, .6f, .3f); //green
        else colorBlock.normalColor = new Color(.6f, .6f, .3f);//yellow
        btn.colors = colorBlock;
    }
    
    void lockOption() {
        isLocked = true;
        btn.interactable = false;
        timeText.gameObject.SetActive(true);
        lockObj.SetActive(true);
    }

    public void disable() {
        lockOption();
        gameObject.SetActive(false);
    }
    
    public void enable(string s, float time) {
        lockOption();
        text.text = s;
        //timeText.text = (time / 60).ToString("00") + (time % 60).ToString("00");
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        timeText.text = timeSpan.ToString("mm':'ss");
        gameObject.SetActive(true);
    }

    public void select() {
        PdfManager.s_instance.optionSelected(this);
    }
    
}
