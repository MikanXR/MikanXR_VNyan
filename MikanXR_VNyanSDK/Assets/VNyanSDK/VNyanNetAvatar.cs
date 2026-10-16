using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyanNet Avatar")]
public class VNyanNetAvatar : MonoBehaviour
{
    [Tooltip("List of objects that will be synced over VNyanNet. Max supported amount is decided by server and will affect performance so recommended to keep this low")]
    public GameObject[] syncedObjects;

}
