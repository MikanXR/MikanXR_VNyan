using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Custom camera for VNyan
/// VNyan's main camera will be moved to this object's transform either instantly
/// or based on CameraSpeed
/// </summary>
[AddComponentMenu("VNyan/VNyan Camera")]
public class VNyanCamera : MonoBehaviour
{
    [Tooltip("Seconds to transition to this camera from any camera")]
    public float TransitionToSeconds = 0.0f;
    [Tooltip("Seconds to transition from this camera to VNyan controls")]
    public float TransitionFromSeconds = 0.0f;
    [Tooltip("Camera's transition speed. 0 = not in use")]
    [Range(0.0f, 50.0f)]
    public float CameraSpeed = 0.0f;
    [Tooltip("Camera's smoothing. Overrides CameraSpeed. 0 = not in use")]
    [Range(0.0f, 1.0f)]
    public float CameraSmoothing = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
