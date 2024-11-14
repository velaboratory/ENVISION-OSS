using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = VRPen.Debug;

public class AnnotationTab : MonoBehaviour
{
    public int minXPos;
    public int maxXPos;
    public Image img;

    public VideoController.Annotation annotation;

    //pos is 0-1
    public void setPos(float pos) {
        float xPos = (maxXPos - minXPos) * pos + minXPos;
        transform.localPosition = new Vector3(xPos, 0, 0);
    }

    public void setColor(bool red) {
        img.color = red ? new Color(0.8117f,0.2117f,0.2117f) : Color.black;
    }

    public void destroy() {
        GameObject.Destroy(gameObject);
    }

    public void snapTo() {
        VideoController.s_instance.annotate(true, annotation.getPercentTime(), annotation.getCanvasId());
    }
}
