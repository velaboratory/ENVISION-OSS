using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPen;
using Debug = VRPen.Debug;
using Display = VRPen.Display;

public class GazeVRPenInput :  VRPenInput {
    // Start is called before the first frame update

    [Header("Gaze Parameters")]        
    [Space(10)]
    [Tooltip("If not assigned, the main camera will be used")]
    public Transform gaze;
    
    
    [Header("Extra Values")]
    [Space(10)]
    public float pressure = 0;

    //bool takeInput = false;
    private float lastHMDButtonClick;
    private const float cursorToggleDoubleTapTime = 0.5f;
    private bool hmdInputEn = false;
    public GameObject hmdInputCursor;
    public GameObject hmdCursorUI;
    public Transform head;

    new void Start() {
        
        base.Start();
    }

    // Update is called once per frame
    void Update() {


        if (Input.GetKeyUp(KeyCode.Joystick1Button0)) {
            if (Time.time - cursorToggleDoubleTapTime < lastHMDButtonClick && !hmdInputEn) {
                Invoke(nameof(toggleHmdInput), 0.1f); //do toggle for cursor here
            }
            lastHMDButtonClick = Time.time;
        }

        //set cursor
        hmdInputCursor.SetActive(false);
        if (hmdInputEn) {
            Ray gazeRay = new Ray(head.position, head.forward);
            LayerMask gazeLm = LayerMask.GetMask("EyeTrack2D");
            RaycastHit gazeHit;
            if (Physics.Raycast(gazeRay, out gazeHit, 100, gazeLm)) {
                if (gazeHit.collider.gameObject.layer == LayerMask.NameToLayer("EyeTrack2D")) {
                    Vector3 offset = head.position - hmdInputCursor.transform.position;
                    offset = offset.normalized * 0.007f;
                    hmdInputCursor.transform.position = gazeHit.point + offset;
                    hmdInputCursor.SetActive(true);
                }
            }
        }

        //pressure
        pressure = 1;
        
        
        //check for marker input
        if (!hmdInputEn) {
            idle();
        }
        else if (Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKey(KeyCode.Joystick1Button0)) input();
        else if (Input.GetKeyUp(KeyCode.Joystick1Button0)) {
            UIClickDown = true;
            input();
        }
        else {
            idle();
        }


        

        //reset click value at end of frame
        UIClickDown = false;

        //base
        base.Update();

        
    }
    
    public void toggleHmdInput() {
        hmdInputEn = !hmdInputEn;
        hmdCursorUI.SetActive(hmdInputEn);
    }

    protected override InputData getInputData() {

        //init returns
        InputData data = new InputData();

        //raycast
        RaycastHit[] hits;
		Ray ray = new Ray(gaze.position, gaze.forward);
        hits = Physics.RaycastAll(ray, 10);
        UnityEngine.Debug.DrawRay(ray.origin, ray.direction, Color.blue);
        
        //detect hit based off priority
        raycastPriorityDetection(ref data, hits);
        

        //no hit found
        if (data.hover == HoverState.NONE) {
            return data;
        }

        //pressure
        data.pressure = pressure;

        //find display
        Display localDisplay = null;
        Display[] displays = FindObjectsOfType<Display>();
        foreach(Display display in displays) {
            if (data.hit.collider.transform.IsChildOf(display.transform)) {
                localDisplay = display;
                break;
            }
        }
        if (localDisplay == null) {
            VRPen.Debug.LogError("could not find display");
        }
        else {
            data.display = localDisplay;
        }
        
        

        //return
        return data;


    }
		

}
