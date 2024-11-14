using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VelUtils;
using Logger = UnityEngine.Logger;
using Unity.XR.PXR;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
using VRPen;
using Debug = VRPen.Debug;
using Display = VRPen.Display;

public class VRDEOLogging : MonoBehaviour {
    
    public static VRDEOLogging s_instance;

    public LoginInput loginInput;

    //eye tracking
    public Transform head;
    public Display pdfBoard;
    public Display videoBoard;
    public Transform eyeTrackerPoint;
    public Transform gazeTrackerPoint;
    public Transform eyeTransform;

    public TextMeshPro eyeTrackingDebug;
    public TextMeshPro gazeTrackingDebug;
    
    //objs
    public List<Transform> objectsToTrack;

    //canvases
    public bool exportCanvases;
    private Dictionary<byte, bool> exportQueued = new Dictionary<byte, bool>();
    
    private void Awake() {
        s_instance = this;
        SetupLogHeaders();
    }

    private void Start() {
        //eye track
        //StartCoroutine(eyeTrackEveryFrame());

        //general
        InvokeRepeating(nameof(general), 1, 0.05f);
        
        //saving images
        if (exportCanvases) StartCoroutine(exportRoutine(2f));
    }

    

    void SetupLogHeaders()
    {
        VelUtils.Logger.SetHeaders(GeneralStateFileName, GeneralStateHeaders);
        VelUtils.Logger.SetHeaders(FlashSyncFileName, FlashSyncHeaders);
        VelUtils.Logger.SetHeaders(AnnotationCanvasSwitchFileName, AnnotationCanvasSwitchHeaders);
        VelUtils.Logger.SetHeaders(PDFCanvasSwitchFileName, PDFCanvasSwitchHeaders);
        VelUtils.Logger.SetHeaders(DisplaySwitchFileName, DisplaySwitchHeaders);
        VelUtils.Logger.SetHeaders(EyeGazeFileName, GazeHeaders);
        VelUtils.Logger.SetHeaders(HeadGazeFileName, GazeHeaders);
        VelUtils.Logger.SetHeaders(EyeTrackingObjectHitsFileName, EyeTrackingObjectHitsHeaders);
        VelUtils.Logger.SetHeaders(VideoEventsFileName, VideoEventsHeaders);
        VelUtils.Logger.SetHeaders(DrawPacketFileName, DrawPacketHeaders);
    }

    private const string GeneralStateFileName = "general_state";
    private string[] GeneralStateHeaders = new string[] {
        "userid",
        "head.pos.x",
        "head.pos.y",
        "head.pos.z",
        "head.rot.x",
        "head.rot.y",
        "head.rot.z",
        "head.rot.w",
        "currentPdfCanvasId",
        "currentVideoCanvasId",
        "video.url",
        "video.currenttime",
        "video.isPlaying",
    };
    void general() {

        bool videoLoaded = VideoController.s_instance ? VideoController.s_instance.video.isPrepared : false;
        
        string[] data = new[] {
            loginInput.userId,
            //head
            head.position.x.ToString(),
            head.position.y.ToString(),
            head.position.z.ToString(),
            
            head.rotation.x.ToString(),
            head.rotation.y.ToString(),
            head.rotation.z.ToString(),
            head.rotation.w.ToString(),
            
            //boards
            pdfBoard?.currentLocalCanvas == null ? "-1": pdfBoard.currentLocalCanvas.canvasId.ToString(),
            videoBoard?.currentLocalCanvas == null ? "-1": videoBoard.currentLocalCanvas.canvasId.ToString(),
            
            //video
            videoLoaded? VideoController.s_instance.video.url : "n/a",
            videoLoaded? VideoController.s_instance.getActualTime().ToString() : "-1",
            videoLoaded? VideoController.s_instance.video.isPlaying.ToString() : "n/a",
            
        };

        VelUtils.Logger.LogRow(GeneralStateFileName, data);
    }

    private const string FlashSyncFileName = "flash_sync";
    private string[] FlashSyncHeaders = new string[] {
        "userid",
        "isSent",
    };

    public void logFlashSyncEvent(bool isSent) {
        string[] data = new[] {
            loginInput.userId,
            isSent? "Sent": "Received",
        };

        VelUtils.Logger.LogRow(FlashSyncFileName, data);
    }

    #region canvas

    private IEnumerator exportRoutine(float period) {
        #if UNITY_EDITOR
        while (true) {
            foreach (VectorCanvas c in VectorDrawing.s_instance.canvases) {
                //add listener
                if (!exportQueued.ContainsKey(c.canvasId)) {
                    exportQueued.Add(c.canvasId, true);
                    c.majorCanvasUpdateEvent += id => {
                        exportQueued[id] = true;
                    };
                }
                //export
                if (exportQueued[c.canvasId]) {
                    VectorDrawing.s_instance.saveImage(c.canvasId);
                    exportQueued[c.canvasId] = false;
                }
            }

            yield return new WaitForSeconds(period);
        }

        #endif
        yield break; //return if not in editor
    }

    private const string AnnotationCanvasSwitchFileName = "annotation_canvas_switch";
    private string[] AnnotationCanvasSwitchHeaders = new string[] {
        "userid",
        "displayId",
        "canvasId",
        "videoId",
    };

    public void logAnnotationCanvasSwitch(int displayID, int canvasId, float videoTime = -1) {
        
        //#if UNITY_ANDROID && !UNITY_EDITOR
        string[] data = new[] {
            loginInput.userId,
            displayID.ToString(),
            canvasId.ToString(),
            videoTime.ToString(),
        };

        VelUtils.Logger.LogRow(AnnotationCanvasSwitchFileName, data);
        //#endif
    }

    private const string PDFCanvasSwitchFileName = "pdf_canvas_switch";
    private string[] PDFCanvasSwitchHeaders = new string[] {
        "userid",
        "displayId",
        "canvasId",
        "canvasName",
    };

    public void logPDFCanvasSwitch(int displayID, int canvasId, string canvasName) {
        
        //#if UNITY_ANDROID && !UNITY_EDITOR
        string[] data = new[] {
            loginInput.userId,
            displayID.ToString(),
            canvasId.ToString(),
            canvasName,
        };

        VelUtils.Logger.LogRow(PDFCanvasSwitchFileName, data);
        //#endif
    }

    private const string DisplaySwitchFileName = "display_switch";
    private string[] DisplaySwitchHeaders = new string[] {
        "userid",
        "displayId",
    };

    public void logChangeDisplay(int displayID) {
        
        //#if UNITY_ANDROID && !UNITY_EDITOR
        string[] data = new[] {
            loginInput.userId,
            displayID.ToString(),
        };

        VelUtils.Logger.LogRow(DisplaySwitchFileName, data);
        //#endif
    }

    #endregion
    
    #region eyeTracking

    IEnumerator eyeTrackEveryFrame() {
        while (true) {
            eyeTrackPico();
            yield return null;
        }
    }
    
    void eyeTrackPico() { 
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        //eyetracking
        uint status = 0;
        PXR_EyeTracking.GetCombinedEyePoseStatus(out status);
        if (status > 0) {

            //matrix
            Matrix4x4 mat = Matrix4x4.TRS(eyeTransform.position, eyeTransform.rotation, Vector3.one);

            //vars
            Vector3 combinedEyeGazePoint = Vector3.zero; 
            PXR_EyeTracking.GetCombineEyeGazePoint(out combinedEyeGazePoint);
            Vector3 combinedEyeGazeVector = Vector3.zero;
            PXR_EyeTracking.GetCombineEyeGazeVector(out combinedEyeGazeVector);

            //set visuals
            eyeTrackerPoint.localPosition = combinedEyeGazePoint + combinedEyeGazeVector.normalized;
            eyeTrackingDebug.text = "no hit";

            //check collision
            Ray eyeRay = new Ray(eyeTransform.position, eyeTrackerPoint.position - eyeTransform.position);
            LayerMask eyeLm = LayerMask.GetMask("EyeTrack2D");
            RaycastHit[] eyeHits = Physics.RaycastAll(eyeRay, 100, eyeLm);
            foreach (var hit in eyeHits) {
                //Debug.Log(hit.collider.transform.InverseTransformPoint(hit.point).ToString());
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("EyeTrack2D")) {
                    Vector3 pos = hit.collider.transform.InverseTransformPoint(hit.point);
                    float x = pos.x + .5f;
                    float y = pos.y + .5f;
                    eyeTrack2DLog(true, hit.collider.transform.parent, x, y, eyeRay.origin, eyeRay.direction, hit.distance);
                    eyeTrackerPoint.position = hit.point;
                    eyeTrackingDebug.text = hit.collider.transform.parent.name + "\n" + x + "\n" + y;
                }
            }

            logEyeTrackObjects(eyeRay.origin, eyeRay.direction, "pico_eye");

        }
        
        //gaze track
        //set visuals
        gazeTrackerPoint.position = head.position + head.forward.normalized;
        gazeTrackingDebug.text = "no hit";

        //check collision
        Ray gazeRay = new Ray(head.position, head.forward);
        LayerMask gazeLm = LayerMask.GetMask("EyeTrack2D");
        RaycastHit[] gazeHits = Physics.RaycastAll(gazeRay, 100, gazeLm);
        foreach (var hit in gazeHits) {
            //Debug.Log(hit.collider.transform.InverseTransformPoint(hit.point).ToString());
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("EyeTrack2D")) {
                Vector3 pos = hit.collider.transform.InverseTransformPoint(hit.point);
                float x = pos.x + .5f;
                float y = pos.y + .5f;
                eyeTrack2DLog(false, hit.collider.transform.parent, x, y, gazeRay.origin, gazeRay.direction, hit.distance);
                gazeTrackerPoint.position = hit.point;
                gazeTrackingDebug.text = hit.collider.transform.parent.name + "\n" + x + "\n" + y;
            }
        }

        
        #endif
    }

    private const string EyeGazeFileName = "eye_track_2D_hit";
    private const string HeadGazeFileName = "gaze_track_2D_hit";
    private string[] GazeHeaders = new string[] {
        "userid",
        "surfaceHit.Name",
        "raycastOrigin.Name",
        "surfaceHit.x",
        "surfaceHit.y",
        "surfaceHit.distance",
        "video.url",
        "video.timestamp",
        "video.isPlaying",
        "raycastOrigin.pos.x",
        "raycastOrigin.pos.y",
        "raycastOrigin.pos.z",
        "raycastOrigin.rot.x",
        "raycastOrigin.rot.y",
        "raycastOrigin.rot.z",
        "surfaceHit.pos.x",
        "surfaceHit.pos.y",
        "surfaceHit.pos.z",
        "surfaceHit.rot.x",
        "surfaceHit.rot.y",
        "surfaceHit.rot.z",
        "surfaceHit.rot.w",
        "surfaceHit.scale.x",
        "surfaceHit.scale.y",
        "surfaceHit.scale.z",
    };

    public void eyeTrack2DLog(bool isEyes, Transform obj, float hitPosX, float hitPosY, Vector3 originPos, Vector3 originDir, float distance, string label = null) {
        
        bool videoLoaded = VideoController.s_instance ? VideoController.s_instance.video.isPrepared : false;
        
        string[] data = new[] {
            loginInput.userId,
            
            //main
            obj.name,
            label,
            hitPosX.ToString(),
            hitPosY.ToString(),
            distance.ToString(),
            videoLoaded? VideoController.s_instance.video.url : "n/a",
            videoLoaded? VideoController.s_instance.getActualTime().ToString() : "-1",
            videoLoaded? VideoController.s_instance.video.isPlaying.ToString() : "n/a",

            //origin pos
            originPos.x.ToString(),
            originPos.y.ToString(),
            originPos.z.ToString(),
            
            //origin dir
            originDir.x.ToString(),
            originDir.y.ToString(),
            originDir.z.ToString(),
            
            //pos
            obj.position.x.ToString(),
            obj.position.y.ToString(),
            obj.position.z.ToString(),
            
            //rot
            obj.rotation.x.ToString(),
            obj.rotation.y.ToString(),
            obj.rotation.z.ToString(),
            obj.rotation.w.ToString(),
            
            //scale
            obj.lossyScale.x.ToString(),
            obj.lossyScale.y.ToString(),
            obj.lossyScale.z.ToString(),
            
        };

        if (isEyes) VelUtils.Logger.LogRow(EyeGazeFileName, data);
        else VelUtils.Logger.LogRow(HeadGazeFileName, data);
    }

    private const string EyeTrackingObjectHitsFileName = "eye_tracking_objects";
    private string[] EyeTrackingObjectHitsHeaders = new string[] {
        "userid",
        "raycastOrigin.name",
        "raycastOrigin.pos.x",
        "raycastOrigin.pos.y",
        "raycastOrigin.pos.z",
        "raycastOrigin.rot.x",
        "raycastOrigin.rot.y",
        "raycastOrigin.rot.z",
        "objectHit.name",
        "objectHit.pos.x",
        "objectHit.pos.y",
        "objectHit.pos.z",
        "objectHit.rot.x",
        "objectHit.rot.y",
        "objectHit.rot.z",
        "objectHit.rot.w",
        "objectHit.scale.x",
        "objectHit.scale.y",
        "objectHit.scale.z",
    };

    public void logEyeTrackObjects(Vector3 originPos, Vector3 originDir, string originName) {
        foreach (Transform obj in objectsToTrack) {
            List<string> data = new List<string> {
            loginInput.userId,
            originName,
            
            //origin pos
            originPos.x.ToString(),
            originPos.y.ToString(),
            originPos.z.ToString(),
            
            //origin dir
            originDir.x.ToString(),
            originDir.y.ToString(),
            originDir.z.ToString(),
            //main
            obj.name,
            
            //pos
            obj.position.x.ToString(),
            obj.position.y.ToString(),
            obj.position.z.ToString(),
            
            //rot
            obj.rotation.x.ToString(),
            obj.rotation.y.ToString(),
            obj.rotation.z.ToString(),
            obj.rotation.w.ToString(),
            
            //scale
            obj.lossyScale.x.ToString(),
            obj.lossyScale.y.ToString(),
            obj.lossyScale.z.ToString(),
            };

            VelUtils.Logger.LogRow(EyeTrackingObjectHitsFileName, data);
        }
    }

    #endregion

    #region video

    private const string VideoEventsFileName = "video_events";
    private string[] VideoEventsHeaders = new string[] {
        "userid",
        "videoEvent",
        "video.url",
        "video.timestamp",
    };

    public void logVideoEvent(string videoEvent, string url, float timeSeconds) {
        //#if UNITY_ANDROID && !UNITY_EDITOR
        string[] data = new[] {
            loginInput.userId,
            videoEvent,
            url,
            timeSeconds + ""
        };

        VelUtils.Logger.LogRow(VideoEventsFileName, data);
        //#endif
        
        
    }

    #endregion

    #region drawing


    private const string DrawPacketFileName = "vrpen_packets";
    private string[] DrawPacketHeaders = new string[] {
        "userid",
        "category",
        "senderId",
        "packetData",
    };

    public void logDrawPacket(string category, ulong senderId, byte[] packetData) {
        //#if UNITY_ANDROID && !UNITY_EDITOR
        string base64 = Convert.ToBase64String(packetData);
        
        string[] data = new[] {
            loginInput.userId,
            category,
            senderId.ToString(),
            base64
        };

        VelUtils.Logger.LogRow(DrawPacketFileName, data);
        //#endif
    }
    

    #endregion
    
}
