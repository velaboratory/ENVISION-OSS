using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Debug = VRPen.Debug;
using Object = System.Object;

public class VideoNetwork : MonoBehaviour {

    
    
    public static VideoNetwork s_instance;
    public VideoController videoController;

    public GameManager gm;

    public VideoController video;

    private bool unlockedTimesCaughtup = false;
    private bool videoCaughtup = false;
    private bool pdfUnlocksCaughtup = false;
    
    private void Awake() {
        s_instance = this;
        
    }

    public void sendVideoLink(string link) {
        //make packet
        List<byte> packetList = new List<byte>();
        packetList.AddRange(BitConverter.GetBytes(link.Length));
        byte[] chars = Encoding.ASCII.GetBytes(link);
        for (int x = 0; x < link.Length; x++) {
            packetList.Add(chars[x]);
        }
        byte[] packet = packetList.ToArray();
        
        //send
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.videoLinkID, packet);
    }
    

    public void sendPdfUnlocksCatchup(byte[] unlocks, ulong catchupRecipient) {
        //make packet
        List<byte> packetList = new List<byte>();
        packetList.AddRange(BitConverter.GetBytes(unlocks.Length));
        for (int x = 0; x < unlocks.Length; x++) {
            packetList.Add(unlocks[x]);
        }
        byte[] packet = packetList.ToArray();
        
        
        //send
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.pdfUnlocksCatchupID, packet, true, catchupRecipient);
    }

    public void sendFlashSyncEvent() {
        
        //make packet
        byte[] packet = new byte[] { };
        
        //log
        VRDEOLogging.s_instance.logFlashSyncEvent(true);

        //send
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.flashSyncEventID, packet);
        
    }

    public void receiveFlashSynceEvent() {
        
        GameManager.s_instance.flashSync();
        
        //log
        VRDEOLogging.s_instance.logFlashSyncEvent(false);
    }
    
    public void receivePdfUnlocksCatchup(byte[] packet) {

        if (pdfUnlocksCaughtup) return;
        pdfUnlocksCaughtup = true;
        Debug.Log("Pdf unlocks caught up");
        
        List<byte> unlocks = new List<byte>();
        
        //unpack data
        int offset = 0;
        int unlocksLength = ReadInt(packet, ref offset);
        for (int x = 0; x < unlocksLength; x++) {
            unlocks.Add(ReadByte(packet, ref offset));
        }
        
        PdfManager.s_instance.unlockCatchup(unlocks);
    }
    
    public void sendVideoCatchupPacket(string link, float time, List<VideoController.Annotation> annotations, bool isPlaying, ulong catchupRecipient) {
        //make packet
        List<byte> packetList = new List<byte>();
        packetList.AddRange(BitConverter.GetBytes(time));
        packetList.AddRange(BitConverter.GetBytes(link.Length));
        byte[] chars = Encoding.ASCII.GetBytes(link);
        for (int x = 0; x < link.Length; x++) {
            packetList.Add(chars[x]);
        }
        packetList.AddRange(BitConverter.GetBytes(annotations.Count));
        for (int x = 0; x < annotations.Count; x++) {
            packetList.AddRange(BitConverter.GetBytes(annotations[x].getPercentTime()));
            packetList.Add(annotations[x].getCanvasId());
        }
        packetList.Add(isPlaying?(byte)1:(byte)0);
        
        byte[] packet = packetList.ToArray();
        
        //send
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.videoCatchupID, packet, true, catchupRecipient);
    }

    public void receiveVideoCatchupPacket(byte[] packet) {

        if (videoCaughtup) return;
        videoCaughtup = true;
        Debug.Log("Video caught up");
        
        //unpack data
        int offset = 0;
        float time = ReadFloat(packet, ref offset);
        int linkLength = ReadInt(packet, ref offset);
        string link = Encoding.ASCII.GetString(ReadByteArray(packet, ref offset, linkLength));
        
        //unpack annotations
        int annotationCount = ReadInt(packet, ref offset);
        List<VideoController.Annotation> annotationsToAdd = new List<VideoController.Annotation>();
        for (int x = 0; x < annotationCount; x++) {
            
            //add annotation
            VideoController.Annotation newAnnotation = new VideoController.Annotation(ReadFloat(packet, ref offset), ReadByte(packet, ref offset));
            annotationsToAdd.Add(newAnnotation);
        }
        bool isPlaying = ReadByte(packet, ref offset) == 1;
        
        //set
        videoController.catchup(link, time, annotationsToAdd, isPlaying);
        
    }
    
    public void sendVideoEvent(VideoController.EventType type, float time, byte canvasId = 0) {
        //make packet
        List<byte> packetList = new List<byte>();
        packetList.Add((byte)type);
        packetList.AddRange(BitConverter.GetBytes(time));
        packetList.Add(canvasId);
        byte[] packet = packetList.ToArray();
        Debug.Log(String.Format("sending: type = {0}, time = {1}, canvasId = {2}", type, time, canvasId));

        //send
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.videoEventID, packet);
    }
    
    public void receiveVideoEvent(byte[] packet) {
        //unpack data
        int offset = 0;
        VideoController.EventType type = (VideoController.EventType)ReadByte(packet, ref offset);
        float time = ReadFloat(packet, ref offset);
        byte canvasId = ReadByte(packet, ref offset);

        Debug.Log(String.Format("received: type = {0}, time = {1}, canvasId = {2}", type, time, canvasId));

        //apply
        if (type == VideoController.EventType.play) {
            video.play(false, time);
        }
        else if (type == VideoController.EventType.annotate) {
            video.annotate(false, time, canvasId);
        }
        
        
    }

    public void receiveVideoLink(byte[] packet) {
        //unpack data
        int offset = 0;
        int linkLength = ReadInt(packet, ref offset);
        string link = Encoding.ASCII.GetString(ReadByteArray(packet, ref offset, linkLength));
        
        //apply
        video.setLink(link, false);
    }

	public void sendAvatarState(int whiteboardId, ulong id) {
		//make packet
		List<byte> packetList = new List<byte>();
		packetList.AddRange(BitConverter.GetBytes(whiteboardId));
		packetList.AddRange(BitConverter.GetBytes(id));
		byte[] packet = packetList.ToArray();

		//send
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.avatarStatePacketID, packet);
	}

	public void receiveAvatarState(byte[] packet) {
		//unpack data
		int offset = 0;
		int whiteboardId = ReadInt(packet, ref offset);
		ulong id = ReadULong(packet, ref offset);

		//add/update avatar 
        GameManager.s_instance.set2DAvatarState(id, whiteboardId, false);
	}


    public void sendVideoUnlockedTimeCatchup(List<Tuple<string, float>> data, ulong catchupRecipient) {
        List<byte> packetList = new List<byte>();
        packetList.AddRange(BitConverter.GetBytes(data.Count));
        foreach (var video in data) {
            packetList.AddRange(BitConverter.GetBytes(video.Item1.Length));
            byte[] chars = Encoding.ASCII.GetBytes(video.Item1);
            for (int x = 0; x < video.Item1.Length; x++) {
                packetList.Add(chars[x]);
            }
            packetList.AddRange(BitConverter.GetBytes(video.Item2));
        }
        byte[] packet = packetList.ToArray();
        VelNetNetworkMan.s_instance.sendVideoPacket(VelNetNetworkMan.s_instance.videoLockedTimesCatchupID, packet, true, catchupRecipient);
    }
    
    public void receiveVideoUnlockedTimeCatchup(byte[] packet) {
        
        
        if (unlockedTimesCaughtup) return;
        unlockedTimesCaughtup = true;
        Debug.Log("Unlocked times caught up");
        
        //unpack data
        int offset = 0;
        int count = ReadInt(packet, ref offset);
        for (int x = 0; x < count; x++) {
            int urlLength = ReadInt(packet, ref offset);
            string url = Encoding.ASCII.GetString(ReadByteArray(packet, ref offset, urlLength));
            float time = ReadFloat(packet, ref offset);
            videoController.updateVideoUnlockedTime(url, time);
        }
        
    }
    

    #region Serialization

    void PackByte(byte b, byte[] buf, ref int offset) {
        buf[offset] = b;
        offset += sizeof(byte);
    }

    byte ReadByte(byte[] buf, ref int offset) {
        byte val = buf[offset];
        offset += sizeof(byte);
        return val;
    }

    void PackFloat(float f, byte[] buf, ref int offset) {
        Buffer.BlockCopy(BitConverter.GetBytes(f), 0, buf, offset, sizeof(float));
        offset += sizeof(float);
    }

    public static float ReadFloat(byte[] buf, ref int offset) {
        float val = BitConverter.ToSingle(buf, offset);
        offset += sizeof(float);
        return val;
    }

    short ReadShort(byte[] buf, ref int offset) {
        short val = BitConverter.ToInt16(buf, offset);
        offset += sizeof(short);
        return val;
    }


    void PackULong(ulong u, byte[] buf, ref int offset) {
        Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(ulong));
        offset += sizeof(ulong);
    }

    ulong ReadULong(byte[] buf, ref int offset) {
        ulong val = BitConverter.ToUInt64(buf, offset);
        offset += sizeof(ulong);
        return val;
    }

    long ReadLong(byte[] buf, ref int offset) {
        long val = BitConverter.ToInt64(buf, offset);
        offset += sizeof(long);
        return val;
    }

    void PackUInt32(UInt32 u, byte[] buf, ref int offset) {
        Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(UInt32));
        offset += sizeof(UInt32);
    }

    void PackInt32(Int32 u, byte[] buf, ref int offset) {
        Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(Int32));
        offset += sizeof(Int32);
    }

    UInt32 ReadUInt32(byte[] buf, ref int offset) {
        UInt32 val = BitConverter.ToUInt32(buf, offset);
        offset += sizeof(UInt32);
        return val;
    }

    int ReadInt(byte[] buf, ref int offset) {
        int val = BitConverter.ToInt32(buf, offset);
        offset += sizeof(Int32);
        return val;
    }

    Vector3 ReadVector3(byte[] buf, ref int offset) {
        Vector3 vec;
        vec.x = ReadFloat(buf, ref offset);
        vec.y = ReadFloat(buf, ref offset);
        vec.z = ReadFloat(buf, ref offset);
        return vec;
    }

    Quaternion ReadQuaternion(byte[] buf, ref int offset) {
        Quaternion quat;
        quat.x = ReadFloat(buf, ref offset);
        quat.y = ReadFloat(buf, ref offset);
        quat.z = ReadFloat(buf, ref offset);
        quat.w = ReadFloat(buf, ref offset);
        return quat;
    }

    byte[] ReadByteArray(byte[] buf, ref int offset, int length) {
        byte[] temp = new byte[length];
        for (int x = 0; x < length; x++) {
            temp[x] = buf[x + offset];
        }
        offset += length;
        return temp;
    }

    #endregion
    
}
