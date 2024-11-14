using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VelUtils;
using VelNet;
using VRPen;
using Debug = UnityEngine.Debug;

public class VelNetNetworkMan : MonoBehaviour {
    
    public static VelNetNetworkMan s_instance;

    public string roomToJoin = "1";
    public NetworkInterface networkInterface;
    public List<VRPenInput> localInputDevicesToSetLocalID;
    public GameObject localWhiteboards;
    public NetworkObject whiteboardPrefab;

    
    [System.NonSerialized]
    public ulong localId;
    [System.NonSerialized]
    public bool localIdSet = false;
    
    
    [System.NonSerialized]
    public byte videoEventID = 100;
    [System.NonSerialized]
    public byte videoLinkID = 101;
    [System.NonSerialized]
    public byte avatarStatePacketID = 102;
    [System.NonSerialized]
    public byte videoCatchupID = 103;
    [System.NonSerialized]
    public byte videoLockedTimesCatchupID = 104;
    [System.NonSerialized]
    public byte pdfUnlocksCatchupID = 105;
    [System.NonSerialized]
    public byte flashSyncEventID = 106;

    public string roomChangeURL;

    private string autoJoinThisRoom = "";

    [SerializeField]
    public LoginInput loginInput;
    
    private void Awake() {
        s_instance = this;
    }

    void Start() {
        roomToJoin = loginInput != null && !string.IsNullOrEmpty(loginInput.roomId)? loginInput.roomId : roomToJoin;

        //dont connect if offline mode
        if (VRPen.VectorDrawing.OfflineMode) return;
        
        //callbacks
        setupCallbacks();

        //check for room change
        //InvokeRepeating(nameof(checkServerForRoomChange), 0.1f, 5f);
        StartCoroutine(checkServerForRoomChangeRoutine());
        InvokeRepeating(nameof(heartbeat), .1f, 5f);
    }

    void heartbeat() {
        VelNetManager.GetRooms();
    }

    void checkServerForRoomChange() {
        StartCoroutine(nameof(checkServerForRoomChangeRoutine));
    }

    IEnumerator checkServerForRoomChangeRoutine() {
        // WWWForm form = new WWWForm();
        // form.AddField("hardware_id", "test");
        // form.AddField("current_room", "test");
        // form.AddField("device_info", "test");
        /*
        string configURL = string.Format("api/config?hardware_id={0}&current_room={1}&device_info={2}",
                                         loginInput != null && !string.IsNullOrEmpty(loginInput.contentId) ? loginInput.contentId : "nandana",
                                         roomToJoin,
                                         string.Format("{0}|{1}", loginInput != null && !string.IsNullOrEmpty(loginInput.userId) ? loginInput.userId : "anon", SystemInfo.deviceUniqueIdentifier)
                                        );*/
        string configURL = string.Format("api/config/{0}", loginInput != null && !string.IsNullOrEmpty(loginInput.contentId) ? loginInput.contentId : "nandana");
        Debug.Log(string.Format("connecting to VRDEO Online @ {0}", configURL));
        using (UnityWebRequest www = UnityWebRequest.Get(roomChangeURL + configURL)) {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) { 
                Debug.Log(www.error);
            }
            else {
                String str = www.downloadHandler.text;
                RoomChangeData data = null;
                Thread thread = new Thread(()=> {
                    data = JsonConvert.DeserializeObject<RoomChangeData>(str);
                    Debug.Log("received new room data from server");
                    JsonData.s_instance.overrideData(data.info);
                    if (!string.IsNullOrEmpty(data.room) && data.room.Equals(VelNetManager.Room)) {
                        autoJoinThisRoom = data.room;
                    }
                });
                thread.Start();
                
            }
        }
    }

    private void Update() {
        if (autoJoinThisRoom.Length != 0) RoomSelect.s_instance.go(autoJoinThisRoom);
    }

    // public class RoomChangeData {
    //     public string id;
    //     public string type;
    //     public string name;
    //     public RoomChangeDataConfig config;
    //     public string room;
    //     public string last_update;
    // }

    public class RoomChangeData {
        public string room;
        public JsonData.Data info;
    }
     
    void setupCallbacks() {

        VelNetManager.OnConnectedToServer += () => {
            Debug.Log("Connected to VelNet");
            login();
        };
        
        VelNetManager.OnLoggedIn += () => {
            Debug.Log("Logged into VelNet");
            StartCoroutine(joinRoomAfterPdfDownload());
        };
        
        VelNetManager.OnPlayerJoined += (player, _) => {
            Debug.Log("Other VelNet player joined: " + player.userid);
            GameManager.s_instance.otherUserJoined((ulong)player.userid);
        };

        VelNetManager.OnPlayerLeft += player => {
            Debug.Log("Other VelNet player left: " + player.userid);
            GameManager.s_instance.removeAvatar((ulong)player.userid);
        };
        
        VelNetManager.OnJoinedRoom += roomName => {
            Debug.Log("VelNet room joined: " + roomName);
            VelNetManager.GetRoomData(roomName);
            //create my networked whiteboard for the other users to see
            var whiteboards = VelNetManager.InstantiateNetworkObject(whiteboardPrefab.name);
            whiteboards.GetComponent<CopyTransform>().target = localWhiteboards.transform;
            whiteboards.GetComponent<CopyTransform>().followPosition = true;
            whiteboards.GetComponent<CopyTransform>().followRotation = true;
            var children = new List<GameObject>();
            //disable just about everything on that whiteboard locally- the whiteboard that we start the scene with is the one that does 99% of the networking
            whiteboards.gameObject.GetChildGameObjects(children);
            children.ForEach(child => child.SetActive(false));
        };
        
        VelNetManager.RoomDataReceived += roomData => {
            Debug.Log("VelNet room data received: " + roomData.members.Count + " users");
            foreach (var m in roomData.members) {
                Debug.Log("VelNet room data received, user: " + m.Item2);
            }

            joinVRPen(roomData);
        };
        
        VelNetManager.CustomMessageReceived += (senderId, dataWithCategory) => {
            customPacketReceived(senderId, dataWithCategory);
        };
        
    }

    void login() {
        VelNetManager.Login("user_name", "vrpen");
    }
    
    IEnumerator joinRoomAfterPdfDownload() {

        //wait
        while (!PdfManager.s_instance.pdfDownloaded) yield return null;
		
        //also tell video ui where to start its canvas index
        VideoController.s_instance.minAnnotationCanvasId = (byte)VectorDrawing.s_instance.canvasBackgrounds.Length;
		
        //join
        joinRoom();
    }

    void joinRoom() {
        VelNetManager.Join(roomToJoin);
    }

    void joinVRPen(VelNetManager.RoomDataMessage roomData) {
        
        //first in room?
        bool isAloneInRoom = roomData.members.Count == 1;

        //Connect to room, request cache if not first person in room
        if (isAloneInRoom) {
            networkInterface.connectedToServer((ulong)VelNetManager.LocalPlayer.userid, false);
        }
        else {
            ulong randomOtherPlayerInRoom = 0;
            foreach ((int, string) player in roomData.members) {
                if (player.Item1 != VelNetManager.LocalPlayer.userid) {
                    randomOtherPlayerInRoom = (ulong)player.Item1;
                    break;
                }
            }
            Debug.Log("Requesting VRPen cache from user ID: " + randomOtherPlayerInRoom);
            networkInterface.connectedToServer((ulong)VelNetManager.LocalPlayer.userid, true, randomOtherPlayerInRoom);
        }
		
        //set id on chosen inputs
        foreach (var device in localInputDevicesToSetLocalID) {
            device.ownerID = (ulong)VelNetManager.LocalPlayer.userid;
        }
        
        //set id
        localIdSet = true;
        localId = (ulong)VelNetManager.LocalPlayer.userid;

        //if the user is first in the room, it should spawn the pdf canvases
        PdfManager.s_instance.thisUserSpawnsThePdfCanvases = isAloneInRoom;
        
        //set tablet id
        GameManager.s_instance.tabletInput.ownerID = (ulong)VelNetManager.LocalPlayer.userid;

        //set avatar
        GameManager.s_instance.set2DAvatarState((ulong)VelNetManager.LocalPlayer.userid, GameManager.s_instance.currentWhiteboardId, true);
        
    }

    void customPacketReceived(int senderId, byte[] dataWithCategory) {
        
        //vrpen packet
        if (VRPen.VectorDrawing.OfflineMode) return;
        
        
        //get data
        byte header = dataWithCategory[0];
        byte[] data = new byte[dataWithCategory.Length - 1];
        for (int x = 0; x < data.Length; x++) {
            data[x] = dataWithCategory[x + 1];
        }
        ulong id = (ulong)senderId;
        
        //pipe the data
        distributeCustomPacket(header, data, id);
        
    }

    void distributeCustomPacket(byte header, byte[] data, ulong id) {
        
        //return if in offline mode
        if (VRPen.VectorDrawing.OfflineMode) return;

        //video data
        if (header == videoEventID) {
            VideoNetwork.s_instance.receiveVideoEvent(data);
        }
        else if (header == videoLinkID) {
            VideoNetwork.s_instance.receiveVideoLink(data);
        }
        else if (header == avatarStatePacketID) {
            VideoNetwork.s_instance.receiveAvatarState(data);
        }
        else if (header == videoCatchupID) {
            VideoNetwork.s_instance.receiveVideoCatchupPacket(data);
        }
        else if (header == videoLockedTimesCatchupID) {
            VideoNetwork.s_instance.receiveVideoUnlockedTimeCatchup(data);
        }
        else if (header == pdfUnlocksCatchupID) {
            VideoNetwork.s_instance.receivePdfUnlocksCatchup(data);
        }
        else if (header == flashSyncEventID) {
            VideoNetwork.s_instance.receiveFlashSynceEvent();
        }
        
        //drawing data
        else {
			
            //packet data
            NetworkInterface.PacketCategory packetCategory = (NetworkInterface.PacketCategory)header;

            if (data == null) {
                Debug.LogError("Empty packet (likely not a vrpen packet) " + packetCategory.ToString());
                return;
            }
			
            //pipe the data
            networkInterface.receivePacket(packetCategory, data, id);
			
            //logging
            VRDEOLogging.s_instance.logDrawPacket(packetCategory.ToString(), id, data);
			
        }
    }
    
    
    public void sendVideoPacket(byte type, byte[] packet, bool sendToUser = false, ulong receiverID = 0) {
		
        //offline
        if (VRPen.VectorDrawing.OfflineMode) return;
        
        //add cat
        byte[] packetWithCategory = new byte[packet.Length + 1];
        packetWithCategory[0] = type;
        for (int x = 0; x < packet.Length; x++) {
            packetWithCategory[x + 1] = packet[x];
        }
        
        //send to others
        if (sendToUser) sendToIndividual(packetWithCategory, receiverID);
        else sendToOthers(packetWithCategory);

    }

    public void sendToOthers(byte[] data) {
        VelNetManager.SendCustomMessage(data, false, true, false); 
    }

    public void sendToIndividual(byte[] data, ulong receiverID) {
        //setup velnet group to send to if it doesnt already exist
        int receiverIDSingle = (int)receiverID;
        if (!VelNetManager.instance.groups.ContainsKey(receiverIDSingle.ToString())) {
            List<int> group = new List<int>{ receiverIDSingle }; 
            VelNetManager.SetupMessageGroup(receiverIDSingle.ToString(), group);
            Debug.Log("Setting up VelNet group with ID: " + receiverIDSingle);
        }
        
        //send
        VelNetManager.SendCustomMessageToGroup(receiverIDSingle.ToString(), data, true);
    }

    public void OnDestroy()
    {
        VelNetManager.Leave();
    }

}
