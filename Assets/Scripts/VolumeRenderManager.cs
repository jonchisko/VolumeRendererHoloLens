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

    public delegate void OnChangeTfDim(Texture3D volumeData, Texture3D gradientData, Texture2D transferTexture, TransferFunctionDims tfDim);
    public static OnChangeTfDim onEventChangeTfDim;

    private Texture2D currentTransferTexture;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(this.name + ": Starting up");
        InitVolumeCube();
        InitHistogram();
        RegisterEvents();
        onEventChangeTfDim?.Invoke(ido.tex3D, ido.tex3Dgradient, new Texture2D(0, 0), TransferFunctionDims.None);
    }

    private void OnDestroy()
    {
        DeregisterEvents();
    }

    private void RegisterEvents()
    {
        TransferFunctionController.OnEventRedrawTexture += SetTexture;
        ShaderMenu.onEventChangeTfDim += SetTfDim;
    }

    private void DeregisterEvents()
    {
        TransferFunctionController.OnEventRedrawTexture -= SetTexture;
        ShaderMenu.onEventChangeTfDim -= SetTfDim;
    }

    private void SetTexture(Texture2D tex)
    {
        Debug.Log(this.name + ": Setting texture");
        this.currentTransferTexture = tex;
        ctMaterial.SetTexture("_Transfer2D", this.currentTransferTexture);
    }

    private void SetTfDim(TransferFunctionDims tfDim)
    {
        onEventChangeTfDim?.Invoke(this.ido.tex3D, this.ido.tex3Dgradient, this.currentTransferTexture, tfDim);
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
