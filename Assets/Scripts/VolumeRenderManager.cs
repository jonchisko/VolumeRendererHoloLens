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

    public delegate void OnSetTexture(Texture3D volumeData, Texture3D gradientData, Texture2D transferTexture, TransferFunctionDims tfDim);
    public static OnSetTexture onEventSetTexture;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(this.name + ": Starting up");
        InitVolumeCube();
        InitHistogram();
        RegisterEvents();
        onEventSetTexture?.Invoke(ido.tex3D, ido.tex3Dgradient, new Texture2D(0, 0), TransferFunctionDims.None);
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

    private void SetTexture(Texture2D tex, TransferFunctionDims tfDim)
    {
        Debug.Log(this.name + ": Setting texture");
        ctMaterial.SetTexture("_Transfer2D", tex);
        onEventSetTexture?.Invoke(ido.tex3D, ido.tex3Dgradient, tex, tfDim);
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
