using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using VRPen;
using Debug = VRPen.Debug;
using Display = UnityEngine.Display;

public class VideoController : MonoBehaviour {

    public enum EventType {
        play,
        annotate,
    }

    public static VideoController s_instance;

    private Dictionary<string, float> unlockedTimes = new Dictionary<string, float>();

    public VideoPlayer video;
    public VideoControllerUI controlUI;
    public GameObject videoDisplayCanvasParent;
    public GameObject videoDisplayColliderParent;
    public VRPen.Display videoDisplay;
    public GameObject whiteBG;
    
    //the first annotation will have this id (since 0 and 1 are held by the other whiteboards
    [NonSerialized]
    public byte minAnnotationCanvasId;// = 2;
    public List<Annotation> annotations = new List<Annotation>();
    public float maxTimeDeltaAnnotationSnap;

    [NonSerialized]
    public bool aCanvasIsActive = false;
    
    
    public class Annotation {
        
        float percentTime;
        byte canvasId;
        public AnnotationTab tab;
        
        public Annotation(float percentTime, byte canvasId) {
            this.percentTime = percentTime;
            this.canvasId = canvasId;
            GameObject tabObj = GameObject.Instantiate(VideoController.s_instance.controlUI.AnnotationTabPrefab,
                VideoController.s_instance.controlUI.AnnotationTabParent);
            tab = tabObj.GetComponent<AnnotationTab>();
            tab.setPos(percentTime);
            tab.annotation = this;
        }

        public float getPercentTime() {
            return percentTime;
        }

        public byte getCanvasId() {
            return canvasId;
        }

        public void setPercentTime(float percentTime) {
            this.percentTime = percentTime;
            tab.setPos(percentTime);
        }

    }

    private void Awake() {
        s_instance = this;
    }

    private void Start() {
        //release the rendertexture (set to black so that it doesnt hold onto the last frame of the last execution)
        video.targetTexture.Release();
    }

    private void Update() {
        if (video.isPrepared) {
            updateVideoUnlockedTime(video.url, getPercentTime());
        }
    }

    public void initVideoUnlockedTimesList(List<string> urls) {
        foreach (string url in urls) {
            if(!unlockedTimes.ContainsKey(url)) unlockedTimes.Add(url, 0);
        }
    }

    public void updateVideoUnlockedTime(string url, float time) {
        if (!unlockedTimes.ContainsKey(url)) return;

        if (time > unlockedTimes[url]) unlockedTimes[url] = time;
    }

    public float getVideoUnlockedTime(string url) {
        if (!unlockedTimes.ContainsKey(url)) return 0f;

        return unlockedTimes[url];
    }
    
    public float getVideoUnlockedTime() {
        if (!unlockedTimes.ContainsKey(video.url)) return 0f;
        return unlockedTimes[video.url];
    }

    public List<Tuple<string, float>> getAllVideoUnlockedTimes() {
        List<Tuple<string, float>> data = new List<Tuple<string, float>>();
        foreach (var unlocked in unlockedTimes) {
            Tuple<string, float> entry = new Tuple<string, float>(unlocked.Key, unlocked.Value);
            data.Add(entry);
        }
        return data;
    }

    public void setLink(string link, bool localInput, bool keepAnnotations = false, bool pauseOnFirstFrame = true) {
        
        //ignore if already have link
        if (link.Equals(video.url) && !link.Equals("")) return;
        
        //log
        VRDEOLogging.s_instance.logVideoEvent("set_url", link, 0);
        
        //set url
        video.url = link;
        Debug.Log(string.Format("setting video.url= {0}", video.url));
        if (pauseOnFirstFrame) StartCoroutine(prepareAndLoadFirstFrame());
        
        //clear annotations
        if (!keepAnnotations && annotations.Count > 0) {
            Debug.Log("Clearing annotations");
            foreach (Annotation annotation in annotations) {
                annotation.tab.destroy();
            }
            annotations.Clear();
            setCanvas(false);
        }
        
        //set pdf
        PdfManager.s_instance.newVideoSet(link, localInput);
        
        //network
        if (localInput) VideoNetwork.s_instance.sendVideoLink(link);
    }

    IEnumerator prepareAndLoadFirstFrame() {
        
        //log
        VRDEOLogging.s_instance.logVideoEvent("preparing_video_pause_at_start", video.url, 0);
        
        //load and get to first (or second) frame
        video.Play();
        while (!video.isPlaying) {
            yield return null;
        }
        video.Pause();
        video.StepForward();
        video.StepForward();
        
        //set annotation
        pauseToNearbyAnnotation();
        
        //log
        VRDEOLogging.s_instance.logVideoEvent("[complete]preparing_video_pause_at_start", video.url, percentTimeToActualTime(getPercentTime()));
        
    }
    
    public void play(bool localInput, float percentTime) {
        setTimeLocal(percentTime);
        play(localInput);
    }
    
    public void play(bool localInput) {
        
        if (!video.isPlaying) {
            
            video.Play();
            //Debug.Log("bluepfbe " +  video.frame);
        }
        
        //log
        VRDEOLogging.s_instance.logVideoEvent("play", video.url, percentTimeToActualTime(getPercentTime()));
        
        //disable any annotation canvas
        setCanvas(false);

        Debug.Log(string.Format("Sending Play with percent time: {0}", getPercentTime()));

        //network
        if (localInput) VideoNetwork.s_instance.sendVideoEvent(EventType.play, getPercentTime());
    }

    void setTimeLocal(float percentTime) {
        
        //settime
        long frame = (long)(video.frameCount * percentTime);
        //todo: figure out why i need this bandaid or switch to vlc media player (probibally the latter)
        if (frame < 2) frame = 2; //for some reason, the video player doesnt buffer the video properlly if the frame is set too low. Dont really understand why 
        video.frame = frame;

        Debug.Log(String.Format("setTimeLocal: percentTime = {0}, video.frameCount = {1}, frame = {2}, video.frame = {3}",
            percentTime, video.frameCount, (long)(video.frameCount * percentTime), video.frame));

        //log
        VRDEOLogging.s_instance.logVideoEvent("set_time", video.url, percentTimeToActualTime(percentTime));
        
    }
    
    public void pause() {

        
        //Debug.Log("fala " +  video.frame);
        
        //pause
        video.Pause();
        
        //log
        VRDEOLogging.s_instance.logVideoEvent("pause", video.url, percentTimeToActualTime(getPercentTime()));
    }

    
    public void pause(float percentTime) {
        setTimeLocal(percentTime);
        pause();
    }

    public void pauseToNearbyAnnotation() {

        pauseToNearbyAnnotation(getPercentTime());
       
    }
    
    //only a local thing
    public void pauseToNearbyAnnotation(float percentTime) {
        
        //find closest annotation
        Annotation closest = getNearbyAnnotation(percentTime);

        //snap if close enough to closest annotation, or make a new annotation
        if (closest != null && Mathf.Abs(percentTimeToActualTime(percentTime) - percentTimeToActualTime(closest.getPercentTime())) <=
            maxTimeDeltaAnnotationSnap) {
            annotate(true, closest.getPercentTime(), closest.getCanvasId());
        }
        //make new annotation if none nearby
        else {
            addAnnotationLocal(percentTime);
        }
    }

    Annotation getNearbyAnnotation(float percentTime) {
        
        //find closest annotation
        Annotation closest = null;
        foreach (var annotation in annotations) {
            if (closest == null) closest = annotation;
            else if (Mathf.Abs(annotation.getPercentTime() - percentTime) < Mathf.Abs(closest.getPercentTime() - percentTime)) closest = annotation;
        }

        return closest;
        
    }

    public void pauseToNextAnnotation(bool right) {
        
        //find closest annotation to left or right side
        Annotation next = null;
        foreach (Annotation annotation in annotations) {

            //times
            float currentTime = percentTimeToActualTime(getPercentTime());
            float annotationTime = percentTimeToActualTime(annotation.getPercentTime());
            
            //skip if current canvas
            if (right) {
                if (annotationTime > currentTime + maxTimeDeltaAnnotationSnap) {
                    if (next == null || percentTimeToActualTime(next.getPercentTime()) > annotationTime) next = annotation;
                }
            }
            else {
                if (annotationTime < currentTime - maxTimeDeltaAnnotationSnap) {
                    if (next == null || percentTimeToActualTime(next.getPercentTime()) < annotationTime) next = annotation;
                }
            }
        }
        
        //snap to the annotation if found
        if (next != null) {
            annotate(true, next.getPercentTime(), next.getCanvasId());
        }
    }
    
    public void annotate(bool localInput, float time, byte canvasId) {
        
        //if new annotation
        if (canvasId >= annotations.Count + minAnnotationCanvasId) {
            //if local
            if (localInput) {
                //create new vrpen canvas (this will be synced through vrpen
                createNewVRPenCanvas((byte)(annotations.Count + minAnnotationCanvasId));
            }
            
            //add annotation
            Annotation newAnnotation = new Annotation(time, canvasId);
            annotations.Add(newAnnotation);
            
        }
        
        //get annotation
        Annotation curr = annotations.Find(x => x.getCanvasId() == canvasId);
        
        //if the annotation needs to be repurposed
        if (curr.getPercentTime() != time) {
            repurposeAnnotationCanvas(curr, time, localInput);
        }
        
        //set background to be clear
        VectorCanvas currCanvas = VectorDrawing.s_instance.getCanvas(canvasId);
        if (currCanvas.bgColor.r != 0 
            && currCanvas.bgColor.g != 0 
            && currCanvas.bgColor.b != 0 
            && currCanvas.bgColor.a != 0) {
            currCanvas.bgColor = new Color32(0, 0, 0, 0);
            StartCoroutine(currCanvas.rerenderCanvas());
        }

        //go to annotation
        pause(time);
        setCanvas(true, canvasId, percentTimeToActualTime(time));
        
        //network
        if (localInput) VideoNetwork.s_instance.sendVideoEvent(EventType.annotate, time, canvasId);
        
    }
    
    
    

    public void setCanvas(bool showCanvas, byte canvasId = 0, float time = -1) {
        
        aCanvasIsActive = showCanvas;
        if (showCanvas) {
            videoDisplayCanvasParent.SetActive(true);
            videoDisplayColliderParent.SetActive(true);
        
            //swap canvas
            videoDisplay.swapCurrentCanvas(canvasId, true);

            //set ui canvas text
            controlUI.setCanvasText(true, canvasId);
            
            //set annotation tab color
            controlUI.setAnnotationTabColor(canvasId);
            
            //toggle white bg
            //whiteBG.SetActive(true);

            //log
            VRDEOLogging.s_instance.logAnnotationCanvasSwitch(videoDisplay.uniqueIdentifier, canvasId, time);
        }
        else {
            videoDisplayCanvasParent.SetActive(false);
            videoDisplayColliderParent.SetActive(false);
            
            //set ui canvas text
            controlUI.setCanvasText(false);
            
            //set annotation tab color
            controlUI.setAnnotationTabColor(0, true);
            
            //turn off text input if it was active
            GameManager.s_instance.disableTextInputUI(1);
            if (GameManager.s_instance.whiteboards[1].currentStamp != null) GameManager.s_instance.whiteboards[2].currentStamp.close();
            
            //toggle white bg
            //whiteBG.SetActive(false);
            
            //log
            VRDEOLogging.s_instance.logAnnotationCanvasSwitch(videoDisplay.uniqueIdentifier, -1);
        }
    }
    
    public void addAnnotationLocal(float percentTime) {
        
        //ignore if no video playing
        if (video.url.Equals("")) return;
        
        //check to see if we can reporpose one of the already existing canvases
        if (annotations.Count > 0) {
            //serach for empty canvas
            foreach (Annotation annotation in annotations) {
                
                if (VectorDrawing.s_instance.getCanvas(annotation.getCanvasId()).graphics.Count == 0) {
                    
                    //found empty canvas
                    annotate(true, percentTime, annotation.getCanvasId());
                    return;
                    
                }
            }
        }

        //annotate
        annotate(true, getPercentTime(), (byte)(annotations.Count + minAnnotationCanvasId));
    }


    void repurposeAnnotationCanvas(Annotation annotation, float newTime, bool localInput) {
        
        //Debug.Log("Repurposed annotation #" +annotation.canvasId + " - Now at percent time: " + newTime);
        
        //set new time
        annotation.setPercentTime(newTime);
        
        //if isLocal then clear
        if (localInput) {
            createNewVRPenCanvas(annotation.getCanvasId());
        }
        
    }
    
    void createNewVRPenCanvas(byte newCanvasId) {

        //get canvasid
        //make new canvas
        if (newCanvasId == VectorDrawing.s_instance.canvases.Count) {
            UnityEngine.Debug.Log("New annotation canvas created");
            
            //add canvas
            videoDisplay.addCanvasPassthrough();
            
        }
        //reset canvas
        else if (newCanvasId < VectorDrawing.s_instance.canvases.Count) {
            UnityEngine.Debug.Log("New annotation canvas re-created from old annotation canvas");
            
            //swap to canvas and clear
            videoDisplay.swapCurrentCanvas(newCanvasId, true);
            videoDisplay.clearCanvas();
            
        }
        else {
            Debug.LogError("Could not determine how to create canvas for annotation  (with id: " +newCanvasId+")");
            
        }
    }
    
    public float getPercentTime() {
        return (float)((float)video.frame / video.frameCount);
    }

    public float getPercentTime(float timeSeconds) {
        return (float)(timeSeconds/video.length);
    }

    float percentTimeToActualTime(float percent) {
        return (float)video.length* percent;
    }

    public float getActualTime() {
        return percentTimeToActualTime(getPercentTime());
    }
    
    public void catchup(string url, float time, List<Annotation> annotationsToAdd, bool isPlaying) {
        
        StartCoroutine(catchupRoutine(url, time, annotationsToAdd, isPlaying));
    }

    IEnumerator catchupRoutine(string url, float time, List<Annotation> annotationsToAdd, bool isPlaying) {
        
        //track time for loading to offset pause
        float loadTime = Time.time;
        
        //set link
        setLink(url, false, true, false);
        
        //need to start the play so that it load properlly
        //play(false);
        video.Play();
        
        //wait for link to load before setting time and pause
        while (!video.isPrepared) {
            yield return null;
        }
        
        //add annotations
        for (int x = 0; x < annotationsToAdd.Count; x++) {
            
            //add annotation
            annotations.Add(annotationsToAdd[x]);
        }
        
        //set the time and continue playing or pause
        if (isPlaying) {
            float timeSet = time + getPercentTime(Time.time-loadTime);
            if (timeSet > 1) timeSet = 1;
            setTimeLocal(timeSet);
                Debug.Log("time set " + timeSet);
        }
        else {
            Annotation closest = getNearbyAnnotation(time);
            if (closest == null) {
                Debug.LogError("Video catchup error: paused but no annotation");
                pause(time);
            }
            else {
                annotate(false, closest.getPercentTime(), closest.getCanvasId());
            }
        }
        
        
    }
    
}
