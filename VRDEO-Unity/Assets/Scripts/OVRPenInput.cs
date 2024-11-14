using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VelUtils;
using VRPen;

/**
 * Whole lotta "inspiration" from Oculus' StylusTip.cs
 * 
 * Brook
 */

public class OVRPenInput : VRPenInput
{
    [Header("Parameters I Copypasted 😬")]
    bool snappedToChecker = false;
    VRPen.Display snappedDisplay = null;
    public AnimationCurve pressureCurve;
    public Transform snappedTo;
    public TriggerListener triggers;
    public Transform actualTipLoc;



    public GameObject objToMove;
    private bool movedLastFrame;

    public Transform target;

    public Transform head;

    private OneEuroFilter xFilter;
    private OneEuroFilter yFilter;
    private OneEuroFilter zFilter;

    private const int MaxBreadCrumbs = 60;
    private const float BreadCrumbMinSize = 0.005f;
    private const float BreadCrumbMaxSize = 0.02f;

    [Header("External")]
    [SerializeField] private Transform m_trackingSpace;
    [SerializeField] private Transform m_controllerAnchor;

    [Header("Settings")]
    [SerializeField] private GameObject controllerVisuals;
    [SerializeField] private OVRInput.Handedness m_handedness = OVRInput.Handedness.LeftHanded;
    [SerializeField] private GameObject m_breadCrumbPf;
    [SerializeField] private bool m_shouldDrawBreadCrumbs;

    private GameObject m_breadCrumbContainer;
    private GameObject[] m_breadCrumbs;

    private int m_breadCrumbIndexPrev = -1;
    private int m_breadCrumbIndexCurr = 0;

    private OVRInput.Controller m_controller;
    private OVRInput.RawAxis1D m_index_trigger;
    [Header("Cursor")]
    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject cursorMeshDraw;
    [SerializeField] private GameObject cursorMeshPoint;
    [SerializeField] private GameObject cursorMeshOther;
    [SerializeField] private Vector3 cursorRotOffset;

    [Header("Smoothing")]
    [SerializeField] private Slider beta_slider;
    [SerializeField] private Slider min_cutoff_slider;
    [SerializeField] private float beta = 0.0f;
    [SerializeField] private float min_cutoff = 1.0f;
    [SerializeField] private float clickThreshold;
    private ToolState previousTool;

    private LoginInput loginInput;

    private const string filename = "LocalOVRPenInput";
    private string[] headers = new string[] {
        "userid",
        "currentCanvas",
        "tool",
        "pressure",
        "x",
        "y",
    };

    private void Awake()
    {
        target = target == null ? this.transform : target;
        m_controller = m_handedness == OVRInput.Handedness.LeftHanded ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        m_index_trigger = m_handedness == OVRInput.Handedness.LeftHanded ? OVRInput.RawAxis1D.LIndexTrigger : OVRInput.RawAxis1D.RIndexTrigger;
        if (m_shouldDrawBreadCrumbs)
        {
            // Create the bread crumbs
            m_breadCrumbContainer = new GameObject($"BreadCrumbContainer ({m_handedness})");
            m_breadCrumbs = new GameObject[MaxBreadCrumbs];
            for (int i = 0; i < m_breadCrumbs.Length; ++i)
            {
                // Create bread crumb
                GameObject breadCrumb = GameObject.Instantiate(m_breadCrumbPf, m_breadCrumbContainer.transform);
                breadCrumb.name = $"BreadCrumb ({i})";
                breadCrumb.SetActive(false);

                // Store bread crumb
                m_breadCrumbs[i] = breadCrumb;
            }
        }

        beta_slider?.onValueChanged.AddListener((f)=>beta=f);
        min_cutoff_slider?.onValueChanged.AddListener((f)=>min_cutoff=f);
        loginInput = FindFirstObjectByType<LoginInput>();
        VelUtils.Logger.SetHeaders(filename, headers);
    }

    private new void Start()
    {
        //set triggers
        triggers.triggerEnter.AddListener(triggerEnter);
        triggers.triggerStay.AddListener(triggerStay);
        triggers.triggerExit.AddListener(triggerExit);

        base.Start();
    }

    private new void Update()
    {
        // Get stylus tip data
        float stylusTipForce = GetPressure();
        bool isStylusTipTouching = stylusTipForce > 0;


        if (m_shouldDrawBreadCrumbs)
        {
            DrawBreadCrumb(stylusTipForce, isStylusTipTouching);
        }

        var shouldMoveCanvi = OVRInput.Get(OVRInput.Button.One, m_controller);
        if (shouldMoveCanvi)
        {
            if (!movedLastFrame) //we just started moving check if there's an anchor to erase
            {
                AutoOrigin ao = objToMove.GetComponent<AutoOrigin>();
                if(ao != null)
                {
                    ao.network_uuid = ""; //will not try to find the anchor now
                    ao.moving = true;
                    StartCoroutine(ao.eraseAnchor());
                }
            }

            Vector3 forward = m_controllerAnchor.forward;
            forward.y = -90;
            forward = forward.normalized;
            Quaternion forwardQuat = Quaternion.LookRotation(forward, Vector3.up);
            objToMove.transform.SetPositionAndRotation(target.position, forwardQuat);
            movedLastFrame = true;
        } else if (movedLastFrame) //and we didn't move this frame, we stopped, notify the anchor if it exists
        {
            movedLastFrame = false;
            AutoOrigin ao = objToMove.GetComponent<AutoOrigin>();
            if (ao != null)
            {
                ao.moving = false;
            }
        }

        var shouldErase = OVRInput.Get(m_index_trigger, m_controller) > .6f;
        if (shouldErase && state != ToolState.ERASE)
        {
            previousTool = state;
            switchTool(ToolState.ERASE);
        } else if (!shouldErase && state == ToolState.ERASE)
        {
            switchTool(previousTool);
        }
        //check for marker input
        input();

        if (snappedDisplay == null)
        {
            idle();
            cursor.SetActive(false);
            //controllerVisuals.SetActive(true);
            controllerVisuals?.GetComponent<OVRControllerFader>()?.Unfade();
            xFilter = null;
            yFilter = null;
            zFilter = null;
        }
        //set some stuff that normally gets set in editor
        //targetTransform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        
        base.Update();
    }

    private void FixedUpdate()
    {
        if (snappedToChecker)
        {
            snappedToChecker = false;
        }
        else
        {
            if (snappedTo != null)
            {
                snappedTo = null;
                snappedDisplay = null;
            }
        }
    }

    /**
     * adapted from https://jaantollander.com/post/noise-filtering-using-one-euro-filter
     * brook
     */
    class OneEuroFilter
    {
        //tunable parameters
        public float min_cutoff = 1.0f;
        public float beta = 0.0f;

        //apparently this is just some constant lol
        private float d_cutoff = 1.0f;

        //previous frame
        private float x_prev;
        private float dx_prev;

        public OneEuroFilter(float x0)
        {
            x_prev = x0;
            dx_prev = 0.0f;
        }

        private float smoothing_factor(float t_e, float cutoff)
        {
            float r = 2 * Mathf.PI * cutoff * t_e;
            return r / (r + 1);
        }

        private float exponential_smoothing(float a, float x, float x_prev)
        {
            return a * x + (1 - a) * x_prev;
        }

        public float next(float t_d, float x)
        {
            // filtered derivative of the x-signal
            float a_d = smoothing_factor(t_d, d_cutoff);
            float dx = (x - x_prev) / t_d;
            float dx_hat = exponential_smoothing(a_d, dx, dx_prev);

            // filtered x-signal
            float cutoff = min_cutoff + beta * Mathf.Abs(dx_hat);
            float a = smoothing_factor(t_d, cutoff);
            float x_hat = exponential_smoothing(a, x, x_prev);

            x_prev = x_hat;
            dx_prev = dx_hat;

            return x_hat;
        }
    }

    protected override InputData getInputData()
    {

        //init returns
        InputData data = new InputData();

        //raycast
        Vector3 origin = actualTipLoc.position;
        RaycastHit[] hitsDown = Physics.RaycastAll(origin, Vector3.down, .5f);
        RaycastHit[] hitsUp = Physics.RaycastAll(origin, Vector3.down, .5f);
        RaycastHit[] hits = new RaycastHit[hitsDown.Length + hitsUp.Length];
        Array.Copy(hitsDown, hits, hitsDown.Length);
        Array.Copy(hitsUp, 0, hits, hitsDown.Length, hitsUp.Length);
        UnityEngine.Debug.DrawRay(origin, Vector3.down, Color.red, Time.deltaTime);
        UnityEngine.Debug.DrawRay(origin, Vector3.up, Color.red, Time.deltaTime);


        //detect hit based off priority
        raycastPriorityDetection(ref data, hits);

        //break if there is nothing detected
        if (data.hit.collider == null)
        {
            //VRPen.Debug.LogWarning("Marker is inside of the snap zone, but it couldnt find anything to snap to when raycasting. IDK if this will cause an issue. Be wary.");
            snappedDisplay = null;
            return data;
        }

        // change cursor visualization
        if (data.hover == HoverState.DRAW)
        {
            setCursorMesh(0);
        }
        else if (data.hover == HoverState.PALETTE || data.hover == HoverState.GRABBABLE ||
                 data.hover == HoverState.SELECTABLE || data.hover == HoverState.AREAINPUT)
        {
            setCursorMesh(1);
        }
        else
        {
            setCursorMesh(2);
        }

        // create filters if needed
        if(xFilter == null)
        {
            xFilter = new OneEuroFilter(data.hit.point.x);
            yFilter = new OneEuroFilter(data.hit.point.y);
            zFilter = new OneEuroFilter(data.hit.point.z);
        }

        // update filter co-efficients
        
        xFilter.beta = beta;
        xFilter.min_cutoff = min_cutoff;

        yFilter.beta = beta;
        yFilter.min_cutoff = min_cutoff;

        zFilter.beta = beta;
        zFilter.min_cutoff = min_cutoff;
        
        //smooth hit point
        float smoothedX = xFilter.next(Time.deltaTime, data.hit.point.x);
        float smoothedY = yFilter.next(Time.deltaTime, data.hit.point.y);
        float smoothedZ = zFilter.next(Time.deltaTime, data.hit.point.z);

        data.hit.point = new Vector3(smoothedX, smoothedY, smoothedZ);

        
        // hide the controller
        //controllerVisuals.SetActive(false);
        controllerVisuals.GetComponent<OVRControllerFader>().Fade();




        //pressure
        float rawPressure = GetPressure();
        float pressure = pressureCurve.Evaluate(rawPressure);
        data.pressure = pressure;
        if (state == ToolState.ERASE) data.pressure *= 4;

        //display
        snappedDisplay = data.display = data.hit.collider.GetComponentInParent<VRPen.Display>();

        // move cursor to hit point
        Vector3 cursorSize = cursor.transform.localScale;
        cursor.transform.parent = data.display.transform;
        cursor.transform.position = data.hit.point;
        cursor.transform.localRotation = Quaternion.Euler(cursorRotOffset.x, cursorRotOffset.y, cursorRotOffset.z);
        cursor.transform.localScale = cursorSize;


        // clicked?
        UIClickDown = pressure > clickThreshold;

        Vector3 pos = data.hit.collider.transform.InverseTransformPoint(data.hit.point);
        //this is funky because the colliders on canvi are funky
        float x = pos.y + .5f;
        float y = -pos.x + .5f;

        var log_data = new string[] { loginInput.userId, snappedDisplay.name, state.ToString(), data.pressure.ToString(), x.ToString(), y.ToString()};
        VelUtils.Logger.LogRow(filename, log_data);

        return data;

    }

    void setCursorMesh(int x)
    {
        //set active
        cursor.SetActive(true);
        cursorMeshDraw.SetActive(x == 0);
        cursorMeshPoint.SetActive(x == 1);
        cursorMeshOther.SetActive(x == 2);
    }

    private void triggerEnter(Collider other)
    {
        VRPen.Debug.Log("OVRPenInput.triggerEnter");
        Tag tag;
        if ((tag = other.gameObject.GetComponent<Tag>()) != null)
        {
            //if (tag.tag.Equals("marker-visible")) visuals.SetActive(true);
            if (tag.tag.Equals("snap"))
            {
                snappedTo = other.transform;
                snappedToChecker = true;
                snappedDisplay = snappedTo.GetComponentInParent<VRPen.Display>();

            }
        }
    }

    private void triggerStay(Collider other)
    {
        if (snappedTo != null && other.transform == snappedTo) snappedToChecker = true;
    }

    private void triggerExit(Collider other)
    {

        Tag tag;
        if ((tag = other.gameObject.GetComponent<Tag>()) != null)
        {
            //if (tag.tag.Equals("marker-visible")) visuals.SetActive(false);
            if (tag.tag.Equals("snap") && snappedTo != null)
            {

                //check for click
                //UIClickDown = true;
                input();

                snappedTo = null;
                snappedDisplay = null;

            }

        }

    }


    private void DrawBreadCrumb(float stylusTipForce, bool isStylusTipTouching)
    {
        // Set the next crumb position
        GameObject nextCrumb = m_breadCrumbs[m_breadCrumbIndexCurr];
        nextCrumb.transform.position = this.transform.position;

        // Set next crumb visuals based on stylus tip force
        float nextCrumbSize = Mathf.Lerp(BreadCrumbMinSize, BreadCrumbMaxSize, stylusTipForce);
        nextCrumb.transform.localScale = new Vector3(nextCrumbSize, nextCrumbSize, nextCrumbSize);
        nextCrumb.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.red, stylusTipForce);
        nextCrumb.SetActive(isStylusTipTouching);

        float crumbSeparation = 0;
        float distanceToPrevCrumb = Mathf.Infinity;
        if (m_breadCrumbIndexPrev >= 0)
        {
            // Compute next crumb distance to stylus tip
            distanceToPrevCrumb = (this.transform.position - m_breadCrumbs[m_breadCrumbIndexPrev].transform.position).magnitude;

            // Compute next crumb separation by averaging the previous and next crumb sizes
            crumbSeparation = (nextCrumbSize + m_breadCrumbs[m_breadCrumbIndexPrev].transform.localScale.x) * 0.5f;
        }

        // Determine if a new crumb should drop
        if (isStylusTipTouching && (distanceToPrevCrumb >= crumbSeparation))
        {
            // Drop the crumb
            m_breadCrumbIndexPrev = m_breadCrumbIndexCurr;
            m_breadCrumbIndexCurr = (m_breadCrumbIndexCurr + 1) % m_breadCrumbs.Length;
        }
    }

    public float GetPressure()
    {
        return OVRInput.Get(OVRInput.Axis1D.PrimaryStylusForce, m_controller);
    }

}
