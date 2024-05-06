using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

[AddComponentMenu("VNyan/VNyan Edible Item")]
public class EdibleItem : MonoBehaviour
{
    public GameObject eatParticle;

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

        this.gameObject.tag = "Edible";

    }

}
