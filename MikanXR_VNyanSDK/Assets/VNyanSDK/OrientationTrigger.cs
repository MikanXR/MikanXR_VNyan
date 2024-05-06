using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("VNyan/VNyan Orientation Trigger")]
public class OrientationTrigger : MonoBehaviour
{
    [Header("Values")]

    [Tooltip("Which vector of the transform to do dot product with")]
    public OrientationVector transformVector;

    public enum OrientationVector
    {
        Up = 0,
        Forward,
        Right
    }

    [Tooltip("World orientation vector to dot product this transform's vector against")]
    public Vector3 worldOrientationVector;

    [Tooltip("Dot Product value. Higher values trigger on and less triggers off.")]
    public float dotProductValue;

    [Header("Actions")]
    public UnityEvent onAction;
    public UnityEvent offAction;

}
