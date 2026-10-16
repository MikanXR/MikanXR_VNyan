using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Avatar Bone Follower")]
public class AvatarBoneFollower : MonoBehaviour
{
    public HumanBodyBones linkBone;
    public bool linkScale = false;
    public bool linkRotation = true;
    public bool useLocalRotation = false;
    public bool linkPosition = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
