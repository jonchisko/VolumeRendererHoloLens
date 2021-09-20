using com.jon_skoberne.TransferFunctionDrawer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeRenderManager : MonoBehaviour
{
    public GameObject volumeCube;
    public Material ctMaterial;
    public Material tfMaterial;

    // Start is called before the first frame update
    void Start()
    {
        TransferFunctionController.OnEventRedrawTexture += SetTexture;
    }

    private void OnDestroy()
    {
        TransferFunctionController.OnEventRedrawTexture -= SetTexture;
    }

    private void SetTexture(Texture2D tex)
    {
        ctMaterial.SetTexture("_Transfer2D", tex);
    }
}
