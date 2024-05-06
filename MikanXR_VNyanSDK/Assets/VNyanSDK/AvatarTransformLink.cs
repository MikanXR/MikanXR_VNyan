using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Link avatar to this object's transform.
/// This may override avatar tracking to some degree so it's not suitable
/// for turning avatar to face another direction in a room etc.
/// </summary>
[AddComponentMenu("VNyan/VNyan Avatar Transform Link")]
public class AvatarTransformLink : MonoBehaviour
{
    [Tooltip("Link Avatar to this gameobject's world position")]
    public bool LinkPosition;
    [Tooltip("Link Avatar to this gameobject's world rotation")]
    public bool LinkRotation;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
