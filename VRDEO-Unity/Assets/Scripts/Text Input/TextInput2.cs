using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = VRPen.Debug;
using VRPen;
using Display = VRPen.Display;

public class TextInput2 : MonoBehaviour {

    public TabletInput localTabletInput;
    public HiddenInputField input;
    public RectTransform inputParent;
    public UIInputArea area;
    public UIManager uiman;
    public Slider sizeSlider;
    public GameObject dropdownMenu;
    public Button addButton;
    public Button completeButton;
    public Button clearButton;
    public Button dropdownButton;
    public Text dropdownText;

    private bool focused = false;
    private float size;
    private Color currentColor;

    private void Start() {
        updateSize();
        updateText("Add", Color.red);
        //addButton.onClick.AddListener(delegate { uiman.highlightTimer(.2f); });
        //completeButton.onClick.AddListener(delegate { uiman.highlightTimer(.2f); });
        //clearButton.onClick.AddListener(delegate { uiman.highlightTimer(.2f); });
        //dropdownButton.onClick.AddListener(delegate { uiman.highlightTimer(.2f); });
    }
    
    private void Update() {
        
        //update color
        updateColor();
        
        //update text
        if (input.isFocused) updateText("Type", Color.green);
        
        // if (input.gameObject.activeSelf) {
        //     if (focused && !input.isFocused) {
        //         //clearBtn();
        //     }
        //     else if (!focused && input.isFocused) {
        //         updateText("Type", Color.green);
        //     }
        //
        //     focused = input.isFocused;
        // }
        // else {
        //     focused = false;
        // }

    }
    
    public void updateSize() {
        size = sizeSlider.value;
        inputParent.localScale = Vector3.one * size;
    }

    void updateColor() {
        currentColor = localTabletInput.getColor();
        currentColor.a = 1;
        input.textComponent.color = currentColor;
    }
        

    public void areaInput() {
        input.Select();
        input.caretPosition = input.text.Length;
        inputParent.localPosition = new Vector3(area.getPos().x, area.getPos().y, 0);
        //Debug.Log("AREA:" + area.getPos());
    }

    public void toggleDropdown() {
        if (dropdownMenu.activeSelf) dropdownMenu.SetActive(false);
        else dropdownMenu.SetActive(true);
    }

    public void addBtn() {
        enableInput();
    }

    public void completeBtn() {
        if (uiman.display.currentLocalCanvas == null) return;
        renderText();
        clearText();
        disableInput();
    }

    public void clearBtn() {
        clearText();
        disableInput();
    }

    void clearText() {
        input.text = "";
    }

    void updateText(string str, Color c) {
        dropdownText.text = str;
        dropdownText.color = c;
    }
    
    void renderText() {
        Debug.Log("entered text");
        Vector3 pos = uiman.display.canvasParent.InverseTransformPoint(inputParent.position);
        VectorStamp stamp = VectorDrawing.s_instance.stamp(StampType.text, input.text, currentColor, null, 0, localTabletInput.ownerID, NetworkManager.s_instance.localGraphicIndex,
            -pos.x * uiman.display.canvasParent.transform.parent.localScale.x /
            uiman.display.canvasParent.transform.parent.localScale.y, -pos.y, size, .5f, uiman.display.currentLocalCanvas.canvasId, true);
        NetworkManager.s_instance.localGraphicIndex++;
        VectorDrawing.s_instance.undoStack.Add(stamp);
    }

    void enableInput() {
        area.gameObject.SetActive(true);
        completeButton.gameObject.SetActive(true);
        addButton.gameObject.SetActive(false);
        clearButton.gameObject.SetActive(true);
        inputParent.gameObject.SetActive(true);
        updateText("Place", Color.yellow);
    }

    void disableInput() {
        area.gameObject.SetActive(false);
        completeButton.gameObject.SetActive(false);
        addButton.gameObject.SetActive(true);
        clearButton.gameObject.SetActive(false);
        inputParent.gameObject.SetActive(false);
        updateText("Add", Color.red);
    }

}
