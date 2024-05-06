using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Blendshape Param Link")]
public class BlendshapeParamLink : MonoBehaviour
{
    public string VNyanParameterName;
    public string BlendshapeName;
    public SkinnedMeshRenderer mesh;

    public UpdateMode updateMode = UpdateMode.EveryFrame;

    public enum UpdateMode
    {
        OnAwake = 0,
        OnEnable,
        EveryFrame
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
