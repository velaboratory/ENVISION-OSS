using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRPen;
using Debug = UnityEngine.Debug;

public class PdfManager : MonoBehaviour {

    public static PdfManager s_instance;
   
    
    public Text pdfPageText;
    [System.NonSerialized] 
    public bool thisUserSpawnsThePdfCanvases = false;
    public VRPen.Display pdfWhiteboard;
    int canvasesToLoad = 0;
    [System.NonSerialized] 
    public bool pdfDownloaded = false;


    private byte landingPageCanvasId;
    class Video {
        public string name;
        public string url;
        public List<VideoPdf> videoPdfs;
    }
    class VideoPdf {
        public string name;
        public string url;
        public float unlockTime;
        public bool autoSwitch;
        public bool locked = true;
        public byte vrpenCanvasId;
        public PdfUIOption uiOption;
    }
    private List<Video> videoData = new List<Video>();
    private Video currentVideo = null;

    public List<PdfUIOption> pdfUIOptions;

    List<IEnumerator> threadPumpList = new List<IEnumerator>();

    private void Awake() {
        s_instance = this;
        
    }

    void Start() {
        if (JsonData.s_instance == null) return;
        //load pdfs when the data is downloaded
        JsonData.s_instance.dataLoadedEvent += loadPDF;
        
        //update ui based on canvas
        pdfWhiteboard.canvasChangeEvent += updatePdfOptionColors;
        pdfWhiteboard.canvasChangeEvent += logCanvasChange;
        
        //spawn pdf canvases
        StartCoroutine(spawnPdfCanvases());
        
    }

    private void Update() {

        while(threadPumpList.Count > 0)
        {
            StartCoroutine(threadPumpList[0]);
            threadPumpList.RemoveAt(0);
        }

        //update pdf page number (cant be done in pageturn event since thats not networked, i just depend on vrpen networking canvas change
        if (pdfWhiteboard.currentLocalCanvas!=null) pdfPageText.text = "Page: " + (pdfWhiteboard.currentLocalCanvas.canvasId+1)+"/"+(VectorDrawing.s_instance.canvasBackgrounds.Length);

        //set pdf
        checkForCanvasUnlock();

    }

    public void unlockCatchup(List<byte> unlocks) {
        foreach (var canvasId in unlocks) {
            foreach (var video in videoData) {
                foreach (var pdf in video.videoPdfs) {
                    if (pdf.vrpenCanvasId == canvasId) pdf.locked = false;
                }
            }
        }
    }

    public byte[] getUnlockedPdfs() {
        List<byte> unlocks = new List<byte>();
        foreach (var video in videoData) {
            foreach (var pdf in video.videoPdfs) {
                if (!pdf.locked) unlocks.Add(pdf.vrpenCanvasId);
            }
        }
        return unlocks.ToArray();
    }

    void updatePdfOptionColors(VectorCanvas prev, VectorCanvas next) {
        
        //make old ui option yellow
        if (currentVideo != null) {
            foreach (var video in videoData) {
                foreach (VideoPdf pdf in video.videoPdfs) {
                    if (prev != null && pdf.vrpenCanvasId == prev.canvasId) pdf.uiOption.setColor(false);
                    if (pdf.vrpenCanvasId == next.canvasId) pdf.uiOption.setColor(true);
                }
            }
        }

    }

    void logCanvasChange(VectorCanvas prev, VectorCanvas next) {
        
        //get pdf name
        string pdfName = "N/A";
        if (currentVideo != null) {
            foreach (VideoPdf pdf in currentVideo.videoPdfs) {
                if (pdf.vrpenCanvasId == next.canvasId) {
                    pdfName = pdf.name;
                    break;
                }
            }
        }
        
        //log
        VRDEOLogging.s_instance.logPDFCanvasSwitch(0, next.canvasId, pdfName);
    }

    void checkForCanvasUnlock() {
        
        //ignore if canvases havent been loaded
        if (VectorDrawing.s_instance != null && VectorDrawing.s_instance.canvases.Count == 0) return;
        
        //ignore if not in video
        if (currentVideo == null) return;

        float currentVideoTime = VideoController.s_instance.getActualTime();
        foreach (VideoPdf pdf in currentVideo.videoPdfs) {
            if (pdf.locked) {
                if (currentVideoTime >= pdf.unlockTime || currentVideo.url.Equals("no-video") || currentVideo.url.Equals("")) {
                    pdf.locked = false;
                    pdf.uiOption.unlockOption();
                    if (pdf.autoSwitch) {
                        changePdfPage(pdf.vrpenCanvasId);
                    }
                }
            }
        }

    }
    
    public void newVideoSet(string videoUrl, bool localInput) {

        foreach (var option in pdfUIOptions) {
            option.disable();
        }
        
        foreach (Video video in videoData) {
            if (video.url.Equals(videoUrl)) {
                currentVideo = video;
                for (int x = 0; x < video.videoPdfs.Count; x++) {
                    video.videoPdfs[x].uiOption = pdfUIOptions[x];
                    video.videoPdfs[x].uiOption.enable(video.videoPdfs[x].name, video.videoPdfs[x].unlockTime);
                    if (!video.videoPdfs[x].locked) video.videoPdfs[x].uiOption.unlockOption();
                }
                break;
            }
        }
        
        if (localInput) changePdfPage(landingPageCanvasId);
    }
    

    void loadPDF() {
        
        //return
        if (!JsonData.s_instance.dataLoaded) {
            Debug.LogError("could not load pdf data due to json data not being loaded");
            return;
        }
        
        //landing pdf
        string landingPageUrl = JsonData.s_instance.getLandingPdfData();
        threadPumpList.Add(LoadPdfPage(canvasesToLoad, landingPageUrl));
        canvasesToLoad++;
        landingPageCanvasId = 0;
        
        //video pdfs
        List<Tuple<string, string, List<Tuple<string, string, float, bool>>>> videoPdfData = JsonData.s_instance.getAllVideoPdfData();
        foreach (var videoData in videoPdfData) {
            Video video = new Video();
            video.name = videoData.Item1;
            video.url = videoData.Item2;
            video.videoPdfs = new List<VideoPdf>();
            foreach (var pdfData in videoData.Item3) {
                threadPumpList.Add(LoadPdfPage(canvasesToLoad, pdfData.Item2));
                VideoPdf pdf = new VideoPdf();
                pdf.name = pdfData.Item1;
                pdf.url = pdfData.Item2;
                pdf.unlockTime = pdfData.Item3;
                pdf.autoSwitch = pdfData.Item4;
                pdf.vrpenCanvasId = (byte)canvasesToLoad;
                canvasesToLoad++;
                video.videoPdfs.Add(pdf);
            }
            this.videoData.Add(video);
        }
        
        //set background length
        VectorDrawing.s_instance.canvasBackgrounds = new Texture[canvasesToLoad];
        
    }

    public void optionSelected(PdfUIOption option) {
        if (currentVideo == null) {
            Debug.LogError("cannot switch to pdf option page when no video");
        }
        foreach (VideoPdf pdf in currentVideo.videoPdfs) {
            if (pdf.uiOption == option) changePdfPage(pdf.vrpenCanvasId);
        }
        
    }
    
    IEnumerator LoadPdfPage(int canvasId, string url) {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
        else {
            Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            VectorDrawing.s_instance.canvasBackgrounds[canvasId] = myTexture;
            canvasesToLoad--;
            if (canvasesToLoad == 0) pdfDownloaded = true;
        }
    }
    
    IEnumerator spawnPdfCanvases() {
        while (VectorDrawing.s_instance.canvases.Count == 0) yield return null;
        if (!thisUserSpawnsThePdfCanvases) yield break; //break if not first user
        for (int x = 1; x < VectorDrawing.s_instance.canvasBackgrounds.Length; x++) {
            pdfWhiteboard.addCanvasPassthrough();
        }
        changePdfPage(0);//return to first page
        
    }

    void changePdfPage(int pageNum) {

        //ignore if already on page
        if (pageNum == pdfWhiteboard.currentLocalCanvas.canvasId) return;
        
        //switch
        pdfWhiteboard.swapCurrentCanvas((byte)(pageNum), true);
        
    }

    public void turnPdfPage(bool right) {
        //only turn pdf page if there is a page to turn to
        if (right && pdfWhiteboard.currentLocalCanvas.canvasId + 1 <
            VectorDrawing.s_instance.canvasBackgrounds.Length) {
            changePdfPage(pdfWhiteboard.currentLocalCanvas.canvasId + 1);
        }
        else if (!right && pdfWhiteboard.currentLocalCanvas.canvasId -1 >= 0) {
            changePdfPage(pdfWhiteboard.currentLocalCanvas.canvasId - 1);
        }
    }
}
