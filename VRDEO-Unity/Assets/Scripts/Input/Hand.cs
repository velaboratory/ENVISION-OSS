using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.XR;
using Debug = VRPen.Debug;

public class Hand : MonoBehaviour {
    
    public bool isRight;
    private InputDevice device;

    public Collider pdfBoardCollider;
    public Transform pdfBoardTransform;
    private bool pdfCollided = false;
    private Vector3 pdfPosOffset;
    private bool pdfHeld =false;
    
    public Collider videoBoardCollider;
    public Transform videoBoardTransform;
    private bool videoCollided = false;
    private Vector3 videoPosOffset;
    private bool videoHeld =false;

    public bool moveBoardsTogether;
    public Transform parentTransform;
    
    private bool lastHoldBtn = false;

    private const float rotateSpeed = 50;
    private const float scaleSpeed = 0.5f;
    private const float minScale = 0.5f;
    private const float maxScale = 2.5f;
    
    private void Start() {
        if (isRight) {
            device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        }
        else {
            device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
        }
    }

    private void Update() {

        //get values from controller
        float triggerValue;
        device.TryGetFeatureValue(CommonUsages.grip, out triggerValue);
        bool holdBtn = triggerValue > 0.5f;
        Vector2 thumbStick;
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbStick);
        if (Mathf.Abs(thumbStick.y) < 0.2f) thumbStick = new Vector2(thumbStick.x, 0); //y deadzone 
        if (Mathf.Abs(thumbStick.x) < 0.2f) thumbStick = new Vector2(0, thumbStick.y); //x deadzone 
        
        //grab
        if (!lastHoldBtn && holdBtn) {
            //offset
            if (pdfCollided) {
                if (moveBoardsTogether) pdfPosOffset = transform.position - parentTransform.position;
                else pdfPosOffset = transform.position - pdfBoardTransform.position;
                pdfHeld = true;
            }
            if (videoCollided) {
                if (moveBoardsTogether) videoPosOffset = transform.position - parentTransform.position;
                else videoPosOffset = transform.position - videoBoardTransform.position;
                videoHeld = true;
            }
        }
        else if (lastHoldBtn && !holdBtn) {
            pdfHeld = false;
            videoHeld = false;
        }
        if (pdfHeld) {
            if (moveBoardsTogether) parentTransform.position = transform.position - pdfPosOffset;
            else pdfBoardTransform.position = transform.position - pdfPosOffset;
        }
        if (videoHeld) {
            if (moveBoardsTogether) parentTransform.position = transform.position - videoPosOffset;
            else videoBoardTransform.position = transform.position - videoPosOffset;
        }
        lastHoldBtn = holdBtn;
        
        //rot
        if (pdfCollided) {
            if (moveBoardsTogether) parentTransform.Rotate(Vector3.up, -thumbStick.x*Time.deltaTime * rotateSpeed);
            else pdfBoardTransform.Rotate(Vector3.up, -thumbStick.x*Time.deltaTime * rotateSpeed);
        }
        if (videoCollided) {
            if (moveBoardsTogether) parentTransform.Rotate(Vector3.up, -thumbStick.x*Time.deltaTime * rotateSpeed);
            else videoBoardTransform.Rotate(Vector3.up, -thumbStick.x*Time.deltaTime * rotateSpeed);
        }
        
        //scale
        if (pdfCollided) {
            Vector3 scale;
            if (moveBoardsTogether) scale = parentTransform.localScale + (Vector3.one * thumbStick.y*Time.deltaTime * scaleSpeed);
            else scale = pdfBoardTransform.localScale + (Vector3.one * thumbStick.y*Time.deltaTime * scaleSpeed);
            if (scale.x < minScale) scale = Vector3.one * minScale;
            else if (scale.x > maxScale) scale = Vector3.one * maxScale;
            if (moveBoardsTogether) parentTransform.localScale = scale;
            else pdfBoardTransform.localScale = scale;
        }
        if (videoCollided) {
            Vector3 scale;
            if (moveBoardsTogether) scale = parentTransform.localScale + (Vector3.one * thumbStick.y*Time.deltaTime * scaleSpeed);
            else scale = videoBoardTransform.localScale + (Vector3.one * thumbStick.y*Time.deltaTime * scaleSpeed);
            if (scale.x < minScale) scale = Vector3.one * minScale;
            else if (scale.x > maxScale) scale = Vector3.one * maxScale;
            if (moveBoardsTogether) parentTransform.localScale = scale;
            else videoBoardTransform.localScale = scale;
        }
        
    }

    private void OnTriggerEnter(Collider other) {
        if (other == pdfBoardCollider) {
            pdfCollided = true;
        }
        else if (other == videoBoardCollider) {
            videoCollided = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other == pdfBoardCollider) {
            pdfCollided = false;
            pdfHeld = false;
        }
        else if (other == videoBoardCollider) {
            videoCollided = false;
            videoHeld = false;
        }
    }

}
