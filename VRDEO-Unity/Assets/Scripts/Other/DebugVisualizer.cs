using UnityEngine;

public class DebugVisualizer : MonoBehaviour {
    
    
    static string myLog = "";
    private string output;
    private string stack;

    bool show = false;

    private int frameCount = 0;
    private float lastFrameCheckTime = 0;
    private const float frameCheckDelta = 1f;
    private int fps = -1;

    void OnEnable() {
        Application.logMessageReceived += Log;
    }

    void OnDisable() {
        Application.logMessageReceived -= Log;
    }

    private void Update() {
        //fps stuff
        frameCount++;
        if (Time.time > lastFrameCheckTime + frameCheckDelta) {
            fps = (int) (frameCount / frameCheckDelta);
            frameCount = 0;
            lastFrameCheckTime = Time.time;
        }
    }

    public void Log(string logString, string stackTrace, LogType type) {
        output = logString;
        stack = stackTrace;
        myLog = output + " - " + stack + "\n" + myLog;
        if (myLog.Length > 5000) {
            myLog = myLog.Substring(0, 4000);
        }
    }

    void OnGUI() {
        {
            if (show) {
                myLog = GUI.TextArea(new Rect(5, 5, Screen.width - 10, Screen.height / 2), myLog);
                GUI.TextArea(new Rect(5, Screen.height / 2 + 10, 65, 20), "FPS: " + fps);
            }
        }
    }

    public void toggle() {
        show = !show;
    }
}