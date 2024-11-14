using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = VRPen.Debug;

public class LocomotionUI : MonoBehaviour {

    public UIInputArea joystickInput;
    public RectTransform joystick;

    public void returnJoystick() {
        joystick.localPosition = Vector3.zero;
    }

    public void setJoystick() {
        Vector2 input = joystickInput.getPos();
        //if not within the outer joystick circle, cap it
        if (input.magnitude > 50) {
            input = input.normalized * 50;
        }

        joystick.localPosition = new Vector3(input.x, input.y, 0);
        
        Locomotion.s_instance.translate(input/50f);
        
    }

    public void turnLeft() {
        Locomotion.s_instance.rotatePlayerLeft();
    }
    
    public void turnRight() {
        Locomotion.s_instance.rotatePlayerRight();
    }
    
}
