using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPen;
using Debug = VRPen.Debug;

public class Locomotion : MonoBehaviour {
    public static Locomotion s_instance;
    public StarTablet2 tablet;
    public Transform cameraTransform;
    public Transform playerTransform;

    private const int move_left_btn = 0;
    private const int move_right_btn = 1;
    private const int move_up_btn = 2;
    private const int move_down_btn = 3;

    private void Awake() {
        s_instance = this;
    }
    // void Update() {
    //     
    //     //read buttons
    //     float strafe = (tablet.getButton(move_left_btn) ? 1f : 0) + (tablet.getButton(move_right_btn) ? -1f : 0);
    //     float forward = (tablet.getButton(move_up_btn) ? 1f : 0) + (tablet.getButton(move_down_btn) ? -1f : 0);
    //     
    //     //read camera
    //     Vector3 transVelocity3 = Vector3.ProjectOnPlane(cameraTransform.TransformVector(new Vector3(strafe, 0, forward)), Vector3.up).normalized * Time.deltaTime;
    //     
    //     //apply translation
    //     playerTransform.position += transVelocity3;
    //
    // }

    public void translate(Vector2 trans) {
        
        //read camera
        Vector3 transVelocity3 = new Vector3(trans.x,0,trans.y) * Time.deltaTime;
        
        //apply translation
        playerTransform.position += transVelocity3;
    }

    public void rotatePlayerLeft() {
        rotatePlayer(-10);
    }
    
    public void rotatePlayerRight() {
        rotatePlayer(10);
    }
    
    void rotatePlayer(float degrees) {
        playerTransform.Rotate(Vector3.up, degrees);
    }

    public void setPosition(Vector3 pos) {
        //Vector3 offset = cameraTransform.position - playerTransform.position;
        playerTransform.position = pos;// - offset;
    }
    
    public void setYRot(float rot) {
        playerTransform.eulerAngles = new Vector3(0,rot,0);
        //playerTransform.rotation = Quaternion.identity;
        //playerTransform.RotateAround(cameraTransform.position, Vector3.up, rot);
    }
    
}
