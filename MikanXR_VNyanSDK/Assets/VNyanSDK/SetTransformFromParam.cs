using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Set Transform From Param")]
public class SetTransformFromParam : MonoBehaviour
{
    public bool SetPosition;
    public string ParamPosX;
    public string ParamPosY;
    public string ParamPosZ;

    public bool SetRotation;
    public string ParamRotX;
    public string ParamRotY;
    public string ParamRotZ;

    public bool SetScale;
    public string ParamScaleX;
    public string ParamScaleY;
    public string ParamScaleZ;

    public bool UseWorldSpace;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
