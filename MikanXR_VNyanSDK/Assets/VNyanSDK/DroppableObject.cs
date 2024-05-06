using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan Droppable Object")]
public class DroppableObject : MonoBehaviour
{

    private AudioSource collisionSound = null;

    public GameObject collisionParticlePrefab;
    public bool destroyOnCollision = false;
    public bool objectIsSticky = false;

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

        this.gameObject.tag = "Droppable";   
    }
}
