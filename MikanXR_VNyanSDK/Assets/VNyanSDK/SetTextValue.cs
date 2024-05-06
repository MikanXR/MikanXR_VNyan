using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("VNyan/VNyan Set Text Value")]
public class SetTextValue : MonoBehaviour
{
    public Text textComponent;
    public TMPro.TextMeshPro tmproComponent;
    public TextMesh textMeshComponent;

    public string textValue;

    public UpdateMode updateMode;

    public enum UpdateMode
    {
        OnAwake = 0,
        OnEnable,
        EveryFrame
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
