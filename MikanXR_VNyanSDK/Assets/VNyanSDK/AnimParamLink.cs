using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Anim Param Link")]
public class AnimParamLink : MonoBehaviour
{

    public string VNyanParameterName;
    public string AnimatorParameterName;
    public Animator animator;

    public UpdateMode updateMode = UpdateMode.EveryFrame;

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
