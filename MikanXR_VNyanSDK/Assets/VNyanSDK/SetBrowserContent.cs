using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Set Browser Content")]

public class SetBrowserContent : MonoBehaviour
{
    public string html;
    public UIBrowser browser;

    public UpdateMode updateMode;

    public enum UpdateMode
    {
        OnAwake = 0,
        OnEnable,
        EveryFrame
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
