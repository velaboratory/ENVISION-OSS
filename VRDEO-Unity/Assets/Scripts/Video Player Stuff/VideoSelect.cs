using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using VRPen;
using Debug = VRPen.Debug;
using Display = VRPen.Display;
using VelNet;

public class VideoSelect : MonoBehaviour {

    public Transform parent;
    public GameObject selectOptionVideoPrefab;
    public GameObject selectOptionFolderPrefab;
    public GameObject pagerParent;
    public Button leftPager;
    public Button leftPagerGhost;
    public Button rightPager;
    public Button rightPagerGhost;
    public Button backPager;
    public Text pageText;
    public UIManager ui;

    
    private void Start() {
        
        StartCoroutine(init());
        
    }


    IEnumerator init() {

        //wait
        while (!JsonData.s_instance.dataLoaded) yield return null;
        
        //backpage
        backPager.onClick.AddListener(delegate { setList(false, 0, 0); });
        
        setList(false, 1,0);
    }

    void setList(bool inFolder, int folderIndex, int page) {

        //remove previous options
        for (int x = 0; x < parent.childCount-1; x++) {
            Destroy(parent.GetChild(x).gameObject);
        }

        
        
        //get indexes
        int minIndex = page * 4;   //inclusize
        int length;
        int maxIndex;
        if (inFolder) length = JsonData.s_instance.getVideoCountInFolder(folderIndex); //exclusive
        else length = JsonData.s_instance.getFolderCount();
        maxIndex = length;
        if (maxIndex - minIndex > 4) maxIndex = minIndex + 4;
        
        //add buttons
        for (int x = minIndex; x < maxIndex; x++) {

            if (inFolder) {
                //add option
                GameObject obj = GameObject.Instantiate(selectOptionVideoPrefab, parent);
                VideoSelectOption option = obj.GetComponent<VideoSelectOption>();
                option.title.text = JsonData.s_instance.getVideoName(folderIndex, x);
                string link = JsonData.s_instance.getVideoUrl(folderIndex, x);
                option.link.onClick.AddListener(delegate {
                    GameManager.s_instance.videoControl.setLink(link, true);
                    //GameManager.s_instance.videoControl.play(true);
                    setList(false, 0, 0);    //reset ui
                    GameManager.s_instance.videoSelectUIToggle();  //close ui
                });
            }
            else {
                //add option
                GameObject obj = GameObject.Instantiate(selectOptionFolderPrefab, parent);
                Button btn = obj.GetComponent<Button>();
                obj.transform.GetChild(0).GetComponent<Text>().text = JsonData.s_instance.getFolderName(x);
                int newFolder = x;
                btn.onClick.AddListener(delegate { setList(true, newFolder, 0); });
            }
        }

        //set pager stuff
        leftPager.gameObject.SetActive(page > 0);
        rightPager.gameObject.SetActive(maxIndex < length);
        backPager.gameObject.SetActive(inFolder);
        bool bottomBarActive = leftPager.gameObject.activeInHierarchy ||
                               rightPager.gameObject.activeInHierarchy ||
                               backPager.gameObject.activeInHierarchy;
        if (!leftPager.gameObject.activeInHierarchy && rightPager.gameObject.activeInHierarchy &&  bottomBarActive) leftPagerGhost.gameObject.SetActive(true);
        else leftPagerGhost.gameObject.SetActive(false);
        if (!rightPager.gameObject.activeInHierarchy && leftPager.gameObject.activeInHierarchy && bottomBarActive) rightPagerGhost.gameObject.SetActive(true);
        else rightPagerGhost.gameObject.SetActive(false);
        
        
        //edit : static height windwo
        float uiHeight = 4 * 35; //static 4 high
        //add blank options to keep the next and prev page buttons at the bottom
        for (int x = maxIndex - minIndex; x < 4; x++) {
            GameObject obj = new GameObject();
            RectTransform t = obj.AddComponent<RectTransform>();
            t.transform.SetParent(parent);
            t.sizeDelta = new Vector2(35, 35);
        }
        
        //set height of obj
        //float uiHeight = (maxIndex - minIndex) * 35; //dont do this cause static
        if (bottomBarActive) uiHeight += 35;
        GetComponent<RectTransform>().sizeDelta = new Vector2(350, uiHeight);
        transform.parent.parent.parent.GetComponent<AdditionalUIWindow>().computeWindowSize();
        
        //page 
        if (length > 4) {
            pageText.text = "Page: " + page;
            pageText.gameObject.SetActive(true);
        }
        else {
            pageText.gameObject.SetActive(false);
        }
        
        //set pager button listeners
        pagerParent.transform.SetAsLastSibling();
        leftPager.onClick.RemoveAllListeners();
        if (page > 0) {
            leftPager.onClick.AddListener(delegate { setList(inFolder, folderIndex, page - 1);});
            leftPager.onClick.AddListener(delegate { ui.highlightTimer(0.2f);});
        }
        rightPager.onClick.RemoveAllListeners();
        if (maxIndex < length) {
            rightPager.onClick.AddListener(delegate { setList(inFolder, folderIndex, page + 1);});
            rightPager.onClick.AddListener(delegate { ui.highlightTimer(0.2f);});
        }
        
        
    }
}
