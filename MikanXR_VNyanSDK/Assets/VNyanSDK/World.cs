using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan World")]
public class World : MonoBehaviour
{

    public Transform spawnPoint;
    public Material skyboxMaterial;
    public bool overrideVNyanAmbientLight = false;
    public bool skipVNyanDirectLight = false;
    public GameObject postProcessVolumeContainer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
