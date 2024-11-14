using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using VideoCreator;

public class IOSRecording : MonoBehaviour {

    public int fragments = 0;
    public bool recordOnStart;
    public RenderTexture renderTexture;
    [SerializeField]
    private AudioSource audioSource;
    
    private long startTimeOffset = 0;
    private float startTime;
    private long amountAudioFrame = 0;

    public static IOSRecording s_instance;
    private string cachePath;

    private string micname = null;

    private void Awake() {
        s_instance = this;
        cachePath = "file://" + Application.temporaryCachePath;
    }

    private void Start() {

        #if UNITY_IOS && !UNITY_EDITOR

        if (recordOnStart) {
            //wait a second since it crashes if it starts right away (idk why)
            Invoke(nameof(startRecording), 1f);
        }
        
        InvokeRepeating(nameof(updatechange), 0f, 1/30f);

        #endif
        
    }

    public string getCachePath(int i)
    {
        return cachePath + "/tmp-" + i + ".mov";
    }

    public string getCachePathNoPrefix(int i)
    {
        return Application.temporaryCachePath + "/tmp-" + i + ".mov";
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        //Debug.Log("reading audio");
        WriteAudio(data, channels);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }
    }

    private void WriteAudio(float[] data, int channels)
    {
        if (!MediaCreator.IsRecording()) return;

        long time = (amountAudioFrame * 1_000_000 / 48_000) + startTimeOffset;
        //Debug.Log($"write audio: {time}");

        MediaCreator.WriteAudio(data, time);

        amountAudioFrame += data.Length;
    }

    public void startRecording() {
        Debug.Log("start microphone");

        var clip = Microphone.Start(micname, true, 1, 48_000);
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
        Debug.Log("made microphone wait for activate");
        while (Microphone.GetPosition(micname) < 0) { }

        Debug.Log("start rec mov with audio!");

        var fragmentedCachePath = getCachePath(fragments++);
        Debug.Log($"cachePath: {fragmentedCachePath}, {renderTexture.width}x{renderTexture.height}");
        
        MediaCreator.InitAsMovWithAudio(fragmentedCachePath, "h264", renderTexture.width, renderTexture.height,1,48_000);
        MediaCreator.Start(startTimeOffset);

        startTime = Time.time;
        amountAudioFrame = 0;
    }
    
    void updatechange()
    {
        if (!MediaCreator.IsRecording()) return;

        long time = (long)((Time.time - startTime) * 1_000_000) + startTimeOffset;

        //Debug.Log($"write texture: {time}");

        MediaCreator.WriteVideo(renderTexture, time);

    }

    public void stopRecording() {

        Debug.Log("finish recording");
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
        Microphone.End(micname);
        if (!MediaCreator.IsRecording()) return;

        MediaCreator.FinishSync();
        
        
    }

    public void OnApplicationPause(bool pause)
    {
        if(pause)
        {
            stopRecording();
        } else
        {
            startRecording();
        }
    }
}
