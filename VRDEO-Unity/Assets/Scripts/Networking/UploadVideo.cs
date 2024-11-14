using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FfmpegUnity;
using System.Threading.Tasks;
using System.Net;

public class UploadVideo : MonoBehaviour {


    public static UploadVideo s_instance;
    
    public string webLogURL;
    public string appName;
    public string webLogPassword;
    //public FfmpegCaptureCommand capture;
    public IOSRecording iosCapture;
    private int byteCountToRead = 1000 * 1000 * 25; //25 MB chunks should be good
    
    [System.NonSerialized]
    public bool uploading = false;


    public class WebRequestEdited {
        public UnityWebRequest www;
        public bool isDone = false;
        public WebRequestEdited(UnityWebRequest www) {
            this.www = www;
        }
    }
    private List<WebRequestEdited> webRequests = new List<WebRequestEdited>();
    
    private List<byte[]> previousVideos = new List<byte[]>();

    [System.NonSerialized]
    public bool lastUploadWasSuccessful = false;


    private void Awake() {
        s_instance = this;
    }

    public float getUploadPercent() {
        if (uploading) {
            if (webRequests.Count == 0) return 0;
            float sum = 0;
            foreach (WebRequestEdited wwwe in webRequests) {
                if (wwwe.isDone) sum += 1;
                else sum += wwwe.www.uploadProgress;
            }
            return sum / webRequests.Count;
        }
        else return -1;
    }

    bool uploadsDone() {
        foreach (var wwwe in webRequests) {
            if (!wwwe.isDone) return false;
        }

        return true;
    }
    
    public void upload() {
        
        //if uploading already, ignore
        if (uploading) return;
        
        //stop video and upload
        uploading = true;
        //capture.StopFfmpeg();
        iosCapture.stopRecording();
        StartCoroutine(nameof(uploadCo));
        
        
    }

    private IEnumerator uploadCo() {

        
        //clear videoupload percents
        webRequests.Clear();
        
        //wait a few frames for ffmpeg to finish up the recording
        yield return new WaitForSeconds(0.1f);
        
        //turns false if any of the videos error
        lastUploadWasSuccessful = true;
        
        //upload previous videos - doesn't do anything now
        for (int x = 0; x < previousVideos.Count; x++) {
            StartCoroutine(uploadToServer(previousVideos[x], x));
        }

        for (int i = 0; i < iosCapture.fragments; i++)
        {
            //get video data
            byte[] data = new byte[byteCountToRead];
            FileStream fs = File.OpenRead(iosCapture.getCachePathNoPrefix(i));
            string filename = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-ff") +
            "_" + i + "_" + SystemInfo.deviceUniqueIdentifier + ".mp4";


            while (fs.Length != fs.Position)
            {
                //chunk
                Task t = fs.ReadAsync(data, 0, byteCountToRead);
                while (!t.IsCompleted)
                {
                    yield return null;
                }
                //send chunk
                yield return uploadToServer(data, filename);
            }
            /*
            using (UnityWebRequest www = UnityWebRequest.Get(iosCapture.getCachePath(i)))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    VRPen.Debug.Log("downloaded data from cache");
                    data = www.downloadHandler.data;
                }
            }

            //add data from past recording to history
            previousVideos.Add(data);
            //upload current video
            StartCoroutine(uploadToServer(data, previousVideos.Count));*/
        }
        //string dir = Path.Combine(Application.persistentDataPath,"capture.mp4");
        //byte[] data = File.ReadAllBytes(dir);

        //restart ffmpeg now that we have the data
        //capture.StartFfmpeg();
        Debug.Log("start recording");
        iosCapture.startRecording();
        Debug.Log("done start recording");

        //wait for all coroutines to finish
        while (!uploadsDone()) {
            yield return null;
        } 
        VRPen.Debug.Log("after finished video");

        
        //set uploading to false
        uploading = false;
        
    }

    IEnumerator uploadToServer(byte[] data, int videoId)
    {
        return uploadToServer(data, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-ff") +
            "_" + videoId + "_" + SystemInfo.deviceUniqueIdentifier + ".mp4");
    }

    IEnumerator uploadToServer(byte[] data, string filename) {
        
        WWWForm form = new WWWForm();
        form.AddField("password", webLogPassword);
        form.AddField("app", appName);
        form.AddField("version", Application.version);
        form.AddBinaryData("upload", data, filename);
        
        using (UnityWebRequest www = UnityWebRequest.Post(webLogURL, form)) {
            WebRequestEdited wwwe = new WebRequestEdited(www);
            webRequests.Add(wwwe);
            yield return www.SendWebRequest();
            wwwe.isDone = true;
            if (www.result != UnityWebRequest.Result.Success) { 
                Debug.Log(www.error);
                lastUploadWasSuccessful = false;
            }
            else {
                Debug.Log("Uploaded video - " + filename);
                Debug.Log(www.downloadHandler.text);
            }
        }
        VRPen.Debug.Log("finished video");
    }
    
}