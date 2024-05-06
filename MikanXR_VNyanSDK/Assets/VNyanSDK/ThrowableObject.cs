using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Throwable Object")]
public class ThrowableObject : MonoBehaviour
{

    public GameObject collisionParticlePrefab;
    public bool destroyOnCollision = false;
    public bool objectIsSticky = false;
    public bool lookAtTargetAtSpawn = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Awake()
    {
        this.gameObject.tag = "Throwable";   
    }
}
