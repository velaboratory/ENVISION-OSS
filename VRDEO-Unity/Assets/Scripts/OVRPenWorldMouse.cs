using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VelUtils;
using VelUtils.Interaction.WorldMouse;

/**
 * Heavily inspired by VelUtils.Interaction.WorldMouse.FingerWorldMouse
 * 
 * how long has it been since ive written a personally inspired pie
 */ 

public class OVRPenWorldMouse : WorldMouse
{

    [SerializeField]
    private OVRPenInput pen;
    [SerializeField]
    private Vector3 offset;

    public Side side;

    [Header("On Click")]
    public float vibrateOnClick = .5f;
    public AudioSource soundOnClick;

    [Header("On Hover")]
    public float vibrateOnHover = .1f;
    public AudioSource soundOnHover;


    public float activationDistance = .1f;
    private bool wasActivated;

    private void Start()
    {
        OnClickDown += OnClicked;
        OnHoverStart += OnHover;
    }

    // Update is called once per frame
    void Update()
    {
        float penPressure = pen.GetPressure();
        if (currentRayLength < activationDistance && penPressure > 0)
        {
            if (!wasActivated)
            {
                wasActivated = true;
                Press();
            }
        }

        if (currentRayLength > activationDistance || float.IsInfinity(currentRayLength) || penPressure < .001f)
        {
            if (wasActivated)
            {
                wasActivated = false;
                Release();
            }
        }
        base.Update();
    }

    private void OnHover(GameObject obj)
    {
        if (obj != null && obj.GetComponent<Selectable>() != null)
        {
            InputMan.Vibrate(side, vibrateOnHover);
            if (soundOnHover != null) soundOnHover.Play();
        }
    }

    private void OnClicked(GameObject obj)
    {
        if (obj != null)
        {
            if (obj.GetComponent<Selectable>() != null)
            {

            }
            InputMan.Vibrate(side, vibrateOnClick);
            if (soundOnClick != null) soundOnClick.Play();
        }
    }
}
