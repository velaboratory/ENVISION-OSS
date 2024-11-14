using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;
using VRPen;
using Debug = VRPen.Debug;
using Display = VRPen.Display;
using VelNet;

public class GameManager : MonoBehaviour {

    public static GameManager s_instance;

    //public vars
    [Space(5)] [Header("Optional Variables")] [Space(5)]
    
    public bool recordVideo;
    public bool useEyeTracker;
    
    
    
    //public vars
    [Space(5)] [Header("Preset References")] [Space(5)]
    
    //video playing
    public VideoController videoControl;
    
    //video recording
    public TextMeshPro timeText;
    public List<Camera> camerasToFlashSync;
    private float flashSyncTime = 0.1f;
    
    //ui
    public TextMeshPro versionText;
    public GameObject roomJoinUIPrefab;
    public GameObject videoSelectUIPrefab;
    public GameObject locomotionUIPrefab;
    public GameObject textInputUIPrefab;
    public GameObject submitDataUIPrefab;
    private AdditionalUIWindow roomJoinUI;
    private AdditionalUIWindow videoSelectUI;
    private AdditionalUIWindow cameraStreamUI;
    private AdditionalUIWindow submitDataUI;
    private AdditionalUIWindow[] locomotionUI = new AdditionalUIWindow[2];
    private AdditionalUIWindow mapLocomotionUI = new AdditionalUIWindow();
    private AdditionalUIWindow[] textInputUI = new AdditionalUIWindow[2];

    //vrpen stuff
    public Display[] whiteboards;
    public GameObject[] whiteboardsIndicators;
	[System.NonSerialized]
    public int currentWhiteboardId;
    public TabletInput tabletInput;
    private int mainStartingDisplayId = 1;
    
    //avatar
    public AvatarList[] whiteboardsAvatarLists;
    List<Avatar2D> avatars2D = new List<Avatar2D>();
    public GameObject avatar2DPrefab;

    //pico
    public Transform eyeTrackerPoint;
    public Transform gazeTrackerPoint;
    private bool usePassthrough = true;

    //networking
    public GameObject disconnectedIndicator;
    
    

    private void Awake() {
        //windows unity doesnt seem to limit fps for me (AJ), this gets rid of gpu coil todo: remove
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        Application.targetFrameRate = 144;
        #endif
        
        //ios fps set
        #if UNITY_IOS && !UNITY_EDITOR_WIN
        Application.targetFrameRate = 120;
        #endif
        
        //turn off tablet if ios
        #if UNITY_IOS && !UNITY_EDITOR_WIN
        tabletInput.gameObject.SetActive(false);
        #endif

        s_instance = this;

    }

    public void Start() {

        //render higher
        XRSettings.eyeTextureResolutionScale = 1.5f;
        
        //version
        versionText.text = "v " + Application.version;
        
        //setup displays and tablet
        currentWhiteboardId = mainStartingDisplayId;
        setTabletUIMenus(whiteboards[mainStartingDisplayId].UIMan);
        tabletInput.setNewDisplay(whiteboards[mainStartingDisplayId]);
        setWhiteboardIndicator(mainStartingDisplayId, true);
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        //set passthrough
        setPassthrough();
        #endif

        //video
        if (recordVideo) setupVideoRecording();
        
    }


    public void Update() {
        
        //disconnected indicator
        if (VelNetManager.IsConnected) disconnectedIndicator.SetActive(false);
        else disconnectedIndicator.SetActive(true);
        
        //for windows, allow arrow keys to change 
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR
            //if (Input.GetKeyDown(KeyCode.RightArrow)) switchDisplay(true);
            //else if (Input.GetKeyDown(KeyCode.LeftArrow)) switchDisplay(false);
        #endif
        
    }

    
    public void otherUserJoined(ulong id) {
        
        //send pdf unlocks
        VideoNetwork.s_instance.sendPdfUnlocksCatchup(PdfManager.s_instance.getUnlockedPdfs(), id);
        
        //if there is a video loaded, send catchup packet
        if (!videoControl.video.url.Equals("")) {
            VideoNetwork.s_instance.sendVideoCatchupPacket(videoControl.video.url, videoControl.getPercentTime(), videoControl.annotations, videoControl.video.isPlaying, id);
        }
        
        //send unlocked times on videos
        VideoNetwork.s_instance.sendVideoUnlockedTimeCatchup(videoControl.getAllVideoUnlockedTimes(), id);
        
        //send avatarstate
        VideoNetwork.s_instance.sendAvatarState(currentWhiteboardId, (ulong)VelNetManager.LocalPlayer.userid);
        
        //send flashsync event
        VideoNetwork.s_instance.sendFlashSyncEvent();
        
    }
    
    #region video recording

    public void setupVideoRecording() {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR || UNITY_IOS
        whiteboards[0].transform.parent.rotation = Quaternion.identity;
        whiteboards[1].transform.parent.rotation = Quaternion.identity;
        StartCoroutine(showTime());
        #endif
    }

    public IEnumerator showTime() {
        timeText.gameObject.SetActive(true);
        while (true) {
            timeText.text = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff");
            yield return null;
        }
    }

    public void flashSync() {
        StartCoroutine(nameof(flashSyncRoutine));
    }

    IEnumerator flashSyncRoutine() {
        foreach (Camera cam in camerasToFlashSync) {
            cam.backgroundColor = Color.red;
        }

        yield return new WaitForSeconds(flashSyncTime);
        
        foreach (Camera cam in camerasToFlashSync) {
            cam.backgroundColor = Color.black;
        }
    }

    #endregion

    
    #region avatar

    
    public void set2DAvatarState(ulong id, int whiteboardId, bool isLocal) {
		
        //new avatar
        if (!avatars2D.Exists(x => x.id == id)){
            Debug.Log("New 2d avatar, id: " + id);
            Avatar2D av = Instantiate(avatar2DPrefab).GetComponent<Avatar2D>();
            av.instantiate(id, isLocal);
            whiteboardsAvatarLists[whiteboardId].addToList(av);
            avatars2D.Add(av);
        }
		
        //old avatar
        else {
            Avatar2D av = avatars2D.Find(x => x.id == id);
            if (av.parent != null) av.parent.removeFromList(av);
            whiteboardsAvatarLists[whiteboardId].addToList(av);
        }

        //network
        if (isLocal) {
            VideoNetwork.s_instance.sendAvatarState(whiteboardId, id);
        }

    }


    public void removeAvatar(ulong id) {
        
        //not exists
        if (!avatars2D.Exists(x => x.id == id)){
            Debug.LogError("Tried to remove avatar that does not exist");
            return;
        }
        
        //remove
        Avatar2D av = avatars2D.Find(x => x.id == id);
        av.parent.removeFromList(av);
        GameObject.Destroy(av.gameObject);
        
    }

    #endregion


    #region pico stuff

    public void toggleEyeTrackerDot() {
        if (!useEyeTracker) return;

        if (eyeTrackerPoint.gameObject.activeInHierarchy) {
            eyeTrackerPoint.gameObject.SetActive(false);
            gazeTrackerPoint.gameObject.SetActive(false);
        }
        else {
            eyeTrackerPoint.gameObject.SetActive(true);
            gazeTrackerPoint.gameObject.SetActive(true);
        }
    }
    
    
    public void togglePassthrough() {
        usePassthrough = !usePassthrough;
        setPassthrough();
    }
    
    void setPassthrough() {
        PXR_Boundary.EnableSeeThroughManual(usePassthrough);
        if (usePassthrough) {
            Camera.main.clearFlags = CameraClearFlags.Color;
            Camera.main.backgroundColor = new Color(0,0,0,0);
        }
        else Camera.main.clearFlags = CameraClearFlags.Skybox;
    }

    #endregion


    #region whiteboard selection

    public void switchDisplay(bool goRight) {
        
        if (goRight && currentWhiteboardId < whiteboards.Length-1) switchDisplay(currentWhiteboardId+1);
        else if (!goRight && currentWhiteboardId > 0)switchDisplay(currentWhiteboardId-1);

    }
    
    
    void setWhiteboardIndicator(int id, bool active) {
        
#if !UNITY_IOS || UNITY_EDITOR
        whiteboardsIndicators[id].SetActive(active);
#endif
    }

    void switchDisplay(int newId) {
        //switch
        tabletInput.setNewDisplay(whiteboards[newId]);
        for (int x = 0; x < whiteboardsIndicators.Length; x++) {
            if (x == newId) setWhiteboardIndicator(x, true);
            else setWhiteboardIndicator(x, false);
        }
        currentWhiteboardId = newId;
        
        //set input back to draw tool
        tabletInput.switchTool(InputVisuals.ToolState.NORMAL);

        //set new whiteboard for avatar (if connected)
        if (VelNetNetworkMan.s_instance.localIdSet) set2DAvatarState(VelNetNetworkMan.s_instance.localId, currentWhiteboardId, true);
        
        //log
        VRDEOLogging.s_instance.logChangeDisplay(newId);
    }

    #endregion
    
    
    #region ui stuff

    
    public void toggleLocomotionUI() {
        if (locomotionUI[currentWhiteboardId].isEnabled()) locomotionUI[currentWhiteboardId].disable(true);
        else locomotionUI[currentWhiteboardId].enable(true);
    }
    public void toggleMapLocomotionUI() {
        if (mapLocomotionUI.isEnabled()) mapLocomotionUI.disable(true);
        else mapLocomotionUI.enable(true);
    }

    public void toggleSubmitDataUI() {
        if (submitDataUI.isEnabled()) submitDataUI.disable(true);
        else submitDataUI.enable(true);
    }

    
    void setTabletUIMenus(UIManager ui) {
        
        //locomotion
        for (int x = 0; x < whiteboards.Length; x++) {
            locomotionUI[x] = whiteboards[x].UIMan.addAdditionalUI();
            locomotionUI[x].setContent(locomotionUIPrefab);
            locomotionUI[x].disable(true);
        }
        
        //textinput
        for (int x = 0; x < whiteboards.Length; x++) {
            textInputUI[x] = whiteboards[x].UIMan.addAdditionalUI();
            textInputUI[x].setContent(textInputUIPrefab);
            textInputUI[x].contentParent.GetChild(0).GetComponent<TextInputUI>().init(x);
            textInputUI[x].disable(true);
        }
        
        //room join
        roomJoinUI = ui.addAdditionalUI(); 
        roomJoinUI.setContent(roomJoinUIPrefab);
        roomJoinUI.disable(true);
        roomJoinUI.grabbable.setExactPos(0,-200);
        
        //video select
        videoSelectUI = ui.addAdditionalUI(); 
        videoSelectUI.setContent(videoSelectUIPrefab);
        videoSelectUI.contentParent.GetChild(0).GetComponent<VideoSelect>().ui = ui;
        videoSelectUI.disable(true);
        videoSelectUI.grabbable.setExactPos(0,-200);

        //quit ui
        submitDataUI = ui.addAdditionalUI(); 
        submitDataUI.setContent(submitDataUIPrefab);
        submitDataUI.disable(true);
        submitDataUI.grabbable.setExactPos(0,-200);

    }
    
    
    public void roomJoinUIToggle() {
        if (roomJoinUI.isEnabled()) roomJoinUI.disable(true);
        else roomJoinUI.enable(true);
    }
    
    public void videoSelectUIToggle() {
        if (videoSelectUI.isEnabled()) videoSelectUI.disable(true);
        else videoSelectUI.enable(true);
    }
    
    public void textInputUIToggle(int index) {
        if (textInputUI[index].isEnabled()) {
            textInputUI[index].disable(true);
        }
        else {
            if (index == 2 && videoControl.aCanvasIsActive) textInputUI[index].enable(true);
            else if (index != 2) textInputUI[index].enable(true);
        }
    }

    public void disableTextInputUI(int index) {
        textInputUI[index].disable(true);
    }
    

    #endregion

   
    
}
