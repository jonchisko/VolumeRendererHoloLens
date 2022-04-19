using com.jon_skoberne.Reader;
using com.jon_skoberne.TransferFunctionDrawer;
using com.jon_skoberne.UI;
using UnityEngine;

public class VolumeRenderManager : MonoBehaviour
{
    public GameObject volumeCube;
    public ShaderMenu shaderMenuInstance;
    //TEMPORARY
    public MeshCreator meshCreatorInstance;
    public HistogramDrawer histogram1d;
    public HistogramDrawer histogram2d;
    public ImageDataObject ido;
    public Material ctMaterial;
    //public Material tfMaterial;

    public delegate void OnChangeTfDim(Texture3D volumeData, Texture3D gradientData, Texture2D transferTexture, TransferFunctionDims tfDim);
    public static OnChangeTfDim onEventChangeTfDim;

    private Texture2D currentTransferTexture;
    private TransferFunctionDims tfDim;

    void Awake()
    {
        this.currentTransferTexture = null;
        this.tfDim = TransferFunctionDims.None;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(this.name + ": Starting up");
        InitVolumeCube();
        InitHistogram();
        RegisterEvents();
        NotifyTfChange();
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
        NotifyTfChange();
    }

    private void SetTfDim(TransferFunctionDims tfDim)
    {
        Debug.Log(this.name + ": Setting tfDim");
        this.tfDim = tfDim;

        // only notify of tf change if there was any texture to begin with
        if(this.currentTransferTexture != null) NotifyTfChange();
    }

    private void NotifyTfChange()
    {
        Debug.Log(this.name + ": tf values:");
        Debug.Log(this.currentTransferTexture + "\n" + this.tfDim.ToString());
        onEventChangeTfDim?.Invoke(this.ido.tex3D, this.ido.tex3Dgradient, this.currentTransferTexture, this.tfDim);
    }

    private void InitVolumeCube()
    {
        shaderMenuInstance.InitializeCubeState(this.ido);
        //TEMPORARY
        meshCreatorInstance.SetIdo(this.ido);
    }

    private void InitHistogram()
    {
        // We are using gaussed gradients for the histogram
        histogram1d.InitializeHistogramState(ido.GetTexture3D(), ido.GetTexture3DGradient(1)); 
        histogram2d.InitializeHistogramState(ido.GetTexture3D(), ido.GetTexture3DGradient(1));
    }

}
