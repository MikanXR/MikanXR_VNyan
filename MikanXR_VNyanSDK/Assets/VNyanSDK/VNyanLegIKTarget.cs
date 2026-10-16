using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Leg IK Target")]
public class VNyanLegIKTarget : MonoBehaviour
{
    public enum IKTargetType
    {
        LeftFoot = 0,
        RightFoot,
        LeftKnee,
        RightKnee
    }

    [Header("Values")]

    [Tooltip("Which target is this bound to")]
    public IKTargetType targetType = IKTargetType.LeftFoot;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

}
