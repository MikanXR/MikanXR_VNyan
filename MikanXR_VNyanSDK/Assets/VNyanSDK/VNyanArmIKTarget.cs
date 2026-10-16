using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Arm IK Target")]
public class VNyanArmIKTarget : MonoBehaviour
{
    public enum IKTargetType
    {
        LeftHand = 0,
        RightHand,
        LeftElbow,
        RightElbow
    }

    [Header("Values")]

    [Tooltip("Which target is this bound to")]
    public IKTargetType targetType = IKTargetType.LeftHand;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

}
