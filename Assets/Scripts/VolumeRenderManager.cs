using com.jon_skoberne.Reader;
using com.jon_skoberne.TransferFunctionDrawer;
using com.jon_skoberne.UI;
using UnityEngine;

public class VolumeRenderManager : MonoBehaviour
{
    public GameObject volumeCube;
    public ShaderMenu shaderMenuInstance;
    public HistogramDrawer histogram1d;
    public HistogramDrawer histogram2d;
    public ImageDataObject ido;
    public Material ctMaterial;
    //public Material tfMaterial;

    // Start is called before the first frame update
    void Start()
    {
        InitVolumeCube();
        InitHistogram();
        RegisterEvents();
    }

    private void OnDestroy()
    {
        DeregisterEvents();
    }

    private void RegisterEvents()
    {
        TransferFunctionController.OnEventRedrawTexture += SetTexture;
    }

    private void DeregisterEvents()
    {
        TransferFunctionController.OnEventRedrawTexture -= SetTexture;
    }

    private void SetTexture(Texture2D tex)
    {
        ctMaterial.SetTexture("_Transfer2D", tex);
    }

    private void InitVolumeCube()
    {
        shaderMenuInstance.InitializeCubeState(this.ido);
    }

    private void InitHistogram()
    {
        // We are using gaussed gradients for the histogram
        histogram1d.InitializeHistogramState(ido.GetTexture3D(), ido.GetTexture3DGradient(1)); 
        histogram2d.InitializeHistogramState(ido.GetTexture3D(), ido.GetTexture3DGradient(1));
    }

}
