using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Net;
using Debug = VRPen.Debug;
using System.Linq;

public class JsonData : MonoBehaviour {

    public static JsonData s_instance;
    
    //json file structure
    public class Data {
        public string landingPdfLink;
        public VideoFolders[] videoFolders;

        public override bool Equals(object obj)
        {
            if (!(obj is Data)) return false;
            Data othercast = obj as Data;
            return landingPdfLink.Equals(othercast.landingPdfLink) && videoFolders.All(e => othercast.videoFolders.Contains(e));
        }
    }
    public class VideoFolders {
        public string folderName;
        public Video[] videos;

        public override bool Equals(object obj)
        {
            if (!(obj is VideoFolders)) return false;
            VideoFolders othercast = obj as VideoFolders;
            return folderName.Equals(othercast.folderName) && videos.All(e => othercast.videos.Contains(e));
        }
    }
    public class Video {
        public string name;
        public string url;
        public VideoPdf[] videoPdfs;

        public override bool Equals(object obj)
        {
            if (!(obj is Video)) return false;
            Video othercast = obj as Video;
            return name.Equals(othercast.name) && url.Equals(othercast.url) && videoPdfs.All(e => othercast.videoPdfs.Contains(e));
        }
    }
    public class VideoPdf {
        public string name;
        public string url;
        public float unlockTime;
        public bool autoSwitch;

        public override bool Equals(object obj)
        {
            if (!(obj is VideoPdf)) return false;
            VideoPdf othercast = obj as VideoPdf;
            return name.Equals(othercast.name) && url.Equals(othercast.url) && unlockTime.Equals(othercast.unlockTime) && autoSwitch.Equals(othercast.autoSwitch);
        }
    }
    
    //data
    private static Data data;
    
    //loading vars
    private Thread dataLoadingThread;
    private Coroutine dataCheckingCoroutine;
    [System.NonSerialized] 
    public bool dataLoaded = false; 
    public delegate void DataLoadedEvent();
    //public event DataLoadedEvent dataLoadedEvent;
    public Action dataLoadedEvent;
    
    //public vars
    public string jsonUrl;
    
    //awake
    private void Awake() {
        s_instance = this;
    }

    //start
    private void Start() {
        //download default pdfs
        //loadData(false);

        //dataCheckingCoroutine = StartCoroutine(checkLoadingThread());
    }
    
    //loading thread
    void loadData(bool overrideData,string str = null) {
        
        //ignore if data already there
        if (data != null && !overrideData) return;
        
        dataLoadingThread = new Thread(()=>loadDataThread(overrideData, str));
        dataLoadingThread.Start();
        
    }

    void loadDataThread(bool overrideData, string str) {
        string json;
        if (overrideData) json = str;
        else json = new WebClient().DownloadString(jsonUrl);
        data = JsonConvert.DeserializeObject<Data>(json);
    }
    
    IEnumerator checkLoadingThread() {
        //wait
        while (dataLoadingThread != null && dataLoadingThread.IsAlive) yield return null;
        dataLoaded = true;
        dataLoadedEvent?.Invoke();
        VideoController.s_instance.initVideoUnlockedTimesList(getVideoUrlList());
        dataCheckingCoroutine = null;
    }

    public void overrideData(Data d) {
        data = d;
        dataLoaded = true;
        dataLoadedEvent?.Invoke();
        VideoController.s_instance.initVideoUnlockedTimesList(getVideoUrlList());
    }

    public List<Tuple<string, string, List<Tuple<string, string, float, bool>>>> getAllVideoPdfData() {
        
        //error
        if (!dataLoaded) {
            Debug.LogError("Cannot retrieve pdf urls since the data has not been loaded yet");
            return null;
        }
        
        //get pdf urls
        List<Tuple<string, string,  List<Tuple<string, string, float, bool>>>> allVideoPdfData =
            new List<Tuple<string, string, List<Tuple<string, string, float, bool>>>>();
        foreach (VideoFolders folder in data.videoFolders) {
            foreach (Video video in folder.videos) {
                List<Tuple<string, string, float, bool>> videoPdfs = new List<Tuple<string, string, float, bool>>();
                foreach (VideoPdf pdf in video.videoPdfs) {
                    videoPdfs.Add(new Tuple<string, string, float, bool>(pdf. name, pdf.url, pdf.unlockTime, pdf.autoSwitch)); 
                }
                Tuple<string, string, List<Tuple<string, string, float, bool>>> videoPdfData = new Tuple<string, string, List<Tuple<string, string, float, bool>>>(video.name, video.url, videoPdfs);
                allVideoPdfData.Add(videoPdfData);
            }
        }
        return allVideoPdfData;
    }

    public string getLandingPdfData() {
        //error
        if (!dataLoaded) {
            Debug.LogError("Cannot retrieve pdf urls since the data has not been loaded yet");
            return null;
        }

        return data.landingPdfLink;
    }

    public int getFolderCount() {
        return data.videoFolders.Length;
    }

    public int getVideoCountInFolder(int folderIndex) {
        return data.videoFolders[folderIndex].videos.Length;
    }

    public string getFolderName(int folderIndex) {
        return data.videoFolders[folderIndex].folderName;
    }
    public string getVideoName(int folderIndex, int videoIndex) {
        return data.videoFolders[folderIndex].videos[videoIndex].name;
    }
    
    public string getVideoUrl(int folderIndex, int videoIndex) {
        return data.videoFolders[folderIndex].videos[videoIndex].url;
    }

    public List<string> getVideoUrlList() {
        List<string> urls = new List<string>();
        foreach (var folder in data.videoFolders) {
            foreach (var video in folder.videos) {
                urls.Add(video.url);
            }
        }
        return urls;
    }
    
}
