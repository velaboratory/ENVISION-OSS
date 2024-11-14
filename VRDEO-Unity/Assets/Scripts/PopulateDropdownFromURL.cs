using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PopulateDropdownFromURL : MonoBehaviour
{
    public string url;
    public Dropdown dropdown;
    [SerializeField]
    private List<string> data;

    private void Awake()
    {
        StartCoroutine(fetchListItems());
    }

    // Start is called before the first frame update
    void Start()
    {
        dropdown = dropdown == null ? GetComponent<Dropdown>() : dropdown;
    }

    private void Update()
    {
        if (!Enumerable.SequenceEqual(data, dropdown.options.ConvertAll<string>((item) => item.text)))
        {
            Debug.Log("got a new list, refreshing dropdown");
            dropdown.ClearOptions();
            dropdown.AddOptions(data);
            //we have to call onValueChanged ourselves
            dropdown.onValueChanged.Invoke(0);
        }
    }

    IEnumerator fetchListItems()
    {
        Debug.Log(string.Format("fetching values from {0}", url));
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string str = www.downloadHandler.text;
                Thread thread = new Thread(() => {
                    data = JsonConvert.DeserializeObject<List<string>>(str);
                    Debug.Log(string.Format("received this list of strings: {0}",string.Join(",", data)));
                });
                thread.Start();

            }
        }
    }
}
