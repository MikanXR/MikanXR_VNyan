using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/*
 * Will be automatically added to World on export if the scene contains lightmaps
 */
public class WorldLightmapSettings : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    public List<MeshLightmapIndex> lightmapIndex;
    public List<Texture2D> lightMap;
    public List<Texture2D> lightMapDir;
    public List<Texture2D> lightMapShadow;
    public LightmapsMode lightmapsMode;

    public AmbientMode ambientMode = AmbientMode.Flat;
    public Color ambientColor;
    public Material ambientSkybox;
    public Color ambientEquator;
    public Color ambientGround;
    public Color ambientSkyColor;
    public SphericalHarmonicsL2 ambientProbe;
    public float ambientIntensity;
    public bool fog = false;
    public Color fogColor;
    public float fogDensity;
    public float fogStartDistance;
    public float fogEndDistance;
    public FogMode fogMode;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[Serializable]
public class MeshLightmapIndex
{
    public MeshRenderer renderer;
    public int lightmapIndex;
    public Vector4 offsetScale;
    public bool stitchSeams;
}
