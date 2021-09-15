using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeCubeManipulator : MonoBehaviour
{
    [SerializeField]
    private Transform volumeCube;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnSliderUpdateRotX(SliderEventData data)
    {
        //volumeCube.rotation = Quaternion.identity;
        volumeCube.RotateAround(volumeCube.position, Vector3.right, data.NewValue * Time.deltaTime);
    }

    public void OnSliderUpdateRotY(SliderEventData data)
    {
        //volumeCube.rotation = Quaternion.identity;
        volumeCube.RotateAround(volumeCube.position, Vector3.up, data.NewValue * Time.deltaTime);
    }

    public void OnSliderUpdateRotZ(SliderEventData data)
    {
        //volumeCube.rotation = Quaternion.identity;
        volumeCube.RotateAround(volumeCube.position, Vector3.forward, data.NewValue * Time.deltaTime);
    }

    public void ResetRotation()
    {
        volumeCube.rotation = Quaternion.identity;
    }
}
