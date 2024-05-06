using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Set Shader Float Param")]
public class SetShaderFloatFromParam : MonoBehaviour
{
    [Tooltip("Material which receives the parameter")]
    public Material targetMaterial;

    [Tooltip("Shader parameter to set")]
    public string shaderParameter = "ShaderParameter";

    [Tooltip("VNyan Paramater")]
    public string vnyanParameter = "VNyanParameter";

    [Tooltip("When to set the Parameter")]
    public UpdateMode updateMode;

    public enum UpdateMode
    {
        OnAwake = 0,
        OnEnable,
        EveryFrame
    }
}
