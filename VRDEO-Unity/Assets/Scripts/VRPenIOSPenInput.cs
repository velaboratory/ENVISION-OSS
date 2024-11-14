using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VRPen;
using Debug = VRPen.Debug;
using Display = VRPen.Display;
using Pointer = UnityEngine.InputSystem.Pointer;

public class VRPenIOSPenInput : VRPenInput {
    // Start is called before the first frame update

    public Camera cam;
    public float pressure = 0;

    public Renderer colorMat;

    //canvas tranlation vars
    public bool canvasMove = false;
    public bool firstCanvasMoveInput = true;
    Vector3 lastCanvasMovePos;

    //slider
    public bool useThicknessSlider;
    public Slider thicknessSlider;


    public GameObject cursor;
    
    private bool lastFrameInput = false;
    
    new void Start() {
        base.Start();
    }

    // Update is called once per frame
    void Update() {

        cursor.SetActive(false);

        if (Pen.current == null) pressure = 0;
        else pressure = Mathf.Clamp(Pen.current.pressure.ReadValue(), 0, 0.5f) * 2;

        bool thisFrameInput = pressure > 0;
        if (useThicknessSlider) pressure *= thicknessSlider.value;

        //VRPen.Debug.LogError(Input.touchSupported + "  " + pressure + "  " + Input.GetMouseButtonDown(0) + "  " + Input.GetMouseButton(0) + "  " + Input.GetMouseButtonUp(0));

        //check for marker input
        if (thisFrameInput) input();
        else if (lastFrameInput) {
            UIClickDown = true;
            input();
        }
        else {
            firstCanvasMoveInput = true;
            idle();
            //nasty apple pen special sauce since we don't have hover to help with lift off
            if(DidClick && LastClicked != null)
            {
                LastClicked.onClick.Invoke();
                LastClicked = null;
                DidClick = false;
            }

            UIClickDown = false;
        }


        //base
        base.Update();

        lastFrameInput = thisFrameInput;
    }

    protected override InputData getInputData() {
        //init returns
        InputData data = new InputData();

        //raycast
        RaycastHit[] hits;
        Vector3 pos = new Vector3(Pen.current.position.ReadValue().x, Pen.current.position.ReadValue().y, 0);
        //Debug.Log("read pos " + pos);
        Ray ray = cam.ScreenPointToRay(pos);
        hits = Physics.RaycastAll(ray, 10);
        UnityEngine.Debug.DrawRay(ray.origin, ray.direction, Color.blue);

        //detect hit based off priority
        raycastPriorityDetection(ref data, hits);


        //no hit found
        if (data.hover == HoverState.NONE) {
            return data;
        }

        cursor.SetActive(true);
        cursor.transform.position = data.hit.point;
        cursor.transform.up = data.hit.normal;
        
        //find display
        Display localDisplay = null;
        Display[] displays = FindObjectsOfType<Display>();
        foreach (Display display in displays) {
            if (data.hit.collider.transform.IsChildOf(display.transform)) {
                localDisplay = display;
                break;
            }
        }

        if (localDisplay == null) {
            Debug.LogError("could not find display");
        }
        else {
            data.display = localDisplay;
            targetLocalToDisplayID = localDisplay.uniqueIdentifier;
        }

        //if we need to move th canvas
        if (data.hover == HoverState.DRAW && canvasMove) {
            if (firstCanvasMoveInput) {
                firstCanvasMoveInput = false;
            }
            else {
                float zBefore = data.display.transform.localPosition.z;
                Vector3 delta = data.hit.point - lastCanvasMovePos;
                data.display.transform.position += delta;
                data.display.transform.localPosition = new Vector3(data.display.transform.localPosition.x,
                    data.display.transform.localPosition.y, zBefore);
            }

            lastCanvasMovePos = data.hit.point;


            //we dont wanna add any draw points so deselect the canvas
            data.hover = HoverState.NONE;
            return data;
        }

        //pressure
        data.pressure = pressure;


        //return
        return data;
    }
    
    
}