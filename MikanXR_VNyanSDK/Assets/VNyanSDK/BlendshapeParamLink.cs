using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Blendshape Param Link")]
public class BlendshapeParamLink : MonoBehaviour
{

    [Tooltip("Decimal Parameter name in VNyan")]
    public string VNyanParameterName;
    [Tooltip("Blendshape name in Skinned Mesh Renderer")]
    public string BlendshapeName;
    [Tooltip("Parameter will be multiplied by this before being applied to blendshape")]
    public float Multiplier = 1.0f;
    [Tooltip("Skinned Mesh Renderer where to apply blendshapes")]
    public SkinnedMeshRenderer mesh;
    [Tooltip("When should the blendshape values be applied")]
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
