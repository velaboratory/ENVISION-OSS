using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRStylusTracking : MonoBehaviour
{

    [Header("External")]
    [SerializeField] private Transform m_trackingSpace;

    [Header("Settings")]
    [SerializeField] private OVRInput.Handedness m_handedness = OVRInput.Handedness.LeftHanded;

    private OVRInput.Controller m_controller;
    // Start is called before the first frame update
    void Awake()
    {
        m_controller = m_handedness == OVRInput.Handedness.LeftHanded ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
    }

    // Update is called once per frame
    void Update()
    {
        // Update stylus tip position
        Pose T_device = new Pose(OVRInput.GetLocalControllerPosition(m_controller), OVRInput.GetLocalControllerRotation(m_controller));
        Pose T_world_device = T_device.GetTransformedBy(m_trackingSpace);
        Pose T_world_stylusTip = GetT_Device_StylusTip(m_controller).GetTransformedBy(T_world_device);
        this.transform.SetPositionAndRotation(T_world_stylusTip.position, T_world_stylusTip.rotation);

    }

    private static Pose GetT_Device_StylusTip(OVRInput.Controller controller)
    {
        // @Note: Only the next controller supports the stylus tip, but we compute the
        // transforms for all controllers so we can draw the tip at the correct location.
        Pose T_device_stylusTip = Pose.identity;

        if (controller == OVRInput.Controller.LTouch || controller == OVRInput.Controller.RTouch)
        {
            T_device_stylusTip = new Pose(
                new Vector3(0.0094f, -0.07145f, -0.07565f), //it's reassuring to see that meta is human like us :9]
                Quaternion.Euler(35.305f, 50.988f, 37.901f)
            );
        }

        if (controller == OVRInput.Controller.LTouch)
        {
            T_device_stylusTip.position.x *= -1;
            T_device_stylusTip.rotation.y *= -1;
            T_device_stylusTip.rotation.z *= -1;
        }

        return T_device_stylusTip;
    }
}
