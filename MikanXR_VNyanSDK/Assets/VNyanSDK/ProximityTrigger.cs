using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Proximity Trigger")]
public class ProximityTrigger : MonoBehaviour
{
    public GameObject[] activateTargets;
    public GameObject[] deactivateTargets;


    public float triggerDistance = 0.05f;

    public UnityEngine.HumanBodyBones sensorBone;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
