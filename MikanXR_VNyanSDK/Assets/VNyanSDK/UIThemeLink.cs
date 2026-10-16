using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VNyan/VNyan UI Theme Link")]
public enum TargetComponent
{
    Component,
    Panel,
    Text,
    Icon,
    Borders,
    PanelComponent,
    PanelComponentText,
    Button,
    ButtonText,
    InnerBorder,
    Workspace,
    CameraBackground
}

public class UIThemeLink : MonoBehaviour
{
    public TargetComponent themeComponent;

    void Awake()
    {
        
    }

    void OnDestroy()
    {

    }
}
