using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyanNet Avatar Position")]
public class VNyanNetAvatarPosition : MonoBehaviour
{
    [Tooltip("Avatar Position Slot. Avatar will be linked to this component's transform if the number set in VNyan is the same as this number")]
    public int avatarPositionSlot;

    // Start is called before the first frame update
    void Start()
    {
        
    }

}
