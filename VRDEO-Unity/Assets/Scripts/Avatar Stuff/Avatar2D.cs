using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//using Vuplex.WebView;

public class Avatar2D : MonoBehaviour {
    public ulong id;
    public AvatarList parent;
    public GameObject bg;
    public TextMeshPro nameText;
    public RemoteTablet tablet;
    public GameObject cursor0;
    public GameObject cursor1;
    private bool isLocal = false;


    Color[] avatarColors = new Color[] {
        new Color(0, 0.5f, 0),
        new Color(0, 0, 0.5f),
        new Color(0.5f, 0, 0.5f),
        new Color(0, 0.5f, 0.5f),
        new Color(0.5f, 0.5f, 0),
    };

    public void instantiate(ulong id, bool isLocal) {
        //set vars
        this.id = id;
        this.isLocal = isLocal;
        tablet.ownerID = id;

        //set text
        nameText.text = (isLocal ? "You: " : "") + "P" + id;

        //en remote tablet if remote
        if (!isLocal) tablet.gameObject.SetActive(true);

        //set color
        setColor();

    }

    public void setColor() {
        byte index = (byte) (((byte) id - 1) % avatarColors.Length);
        bg.GetComponent<Renderer>().material.color = avatarColors[index];
        if (!isLocal) {
            cursor0.GetComponent<Renderer>().material.color = avatarColors[index];
            cursor1.GetComponent<Renderer>().material.color = avatarColors[index];
        }
    }

}