using com.jon_skoberne.TransferFunctionDrawer;
using UnityEngine;

public class BounderBuilder : MonoBehaviour
{
    public delegate void activeGrid(Texture3D activeGrid);
    public static activeGrid OnFinishedActiveGrid;

    public ComputeShader activeGridCalculator;
    public Texture3D activeGridTexture;
    
    private int celDim = 8; // number of voxels in one cell, cell's dim: 8 x 8 x 8
    private RenderTexture volumeData = null;
    private RenderTexture volumeGradientData = null;
    private RenderTexture transferTexture = null;

    void Awake()
    {
        if (activeGridCalculator == null)
        {
            Debug.LogError("activeGridCalculator compute shader is NULL in " + this.name);
        }
        RegisterToEvents();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void OnDestroy()
    {
        UnregisterFromEvents();
    }

    void RegisterToEvents()
    {
        VolumeRenderManager.onEventChangeTfDim += OnTfEvent;
    }

    void UnregisterFromEvents()
    {
        VolumeRenderManager.onEventChangeTfDim -= OnTfEvent;
    }

    public void OnTfEvent(Texture3D volData, Texture3D gradientData, Texture2D tfTex, TransferFunctionDims tfDim)
    {
        switch(tfDim)
        {
            case TransferFunctionDims.None: OnNoTfUsage(volData); break;
            case TransferFunctionDims.Dim1: OnTf1dUsage(volData, tfTex); break;
            case TransferFunctionDims.Dim2: OnTf2dUsage(volData, gradientData, tfTex); break;
            default: Debug.LogError("Tf dim not of any value in " + this.name); break;
        }
    }

    public void OnNoTfUsage(Texture3D volData)
    {
        CloneVolTex(volData);
        CalculateActiveGridBlocks(TransferFunctionDims.None);
        ReleaseTextures();
    }

    public void OnTf1dUsage(Texture3D volData, Texture2D transferTextureData)
    {
        CloneVolTex(volData);
        CloneTransferTex(transferTextureData);
        CalculateActiveGridBlocks(TransferFunctionDims.Dim1);
        ReleaseTextures();
    }

    public void OnTf2dUsage(Texture3D volData, Texture3D gradientData, Texture2D transferTextureData)
    {
        CloneVolTex(volData);
        CloneGradTex(gradientData);
        CloneTransferTex(transferTextureData);
        CalculateActiveGridBlocks(TransferFunctionDims.Dim2);
        ReleaseTextures();
    }

    void CloneTexture3DToRenderTexture(Texture3D volumeTexture, out RenderTexture tex, RenderTextureFormat rtFormat)
    {
        tex = new RenderTexture(volumeTexture.width, volumeTexture.height, 0, rtFormat);
        tex.volumeDepth = volumeTexture.depth;
        tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        tex.enableRandomWrite = true;
        tex.Create();
        Graphics.CopyTexture(volumeTexture, tex);
    }

    void CloneTexture2DToRenderTexture(Texture2D transferTexture, out RenderTexture tex, RenderTextureFormat rtFormat)
    {
        tex = new RenderTexture(transferTexture.width, transferTexture.height, 0, rtFormat);
        tex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        tex.enableRandomWrite = true;
        tex.Create();
        Graphics.CopyTexture(transferTexture, tex);
    }

    void CloneVolTex(Texture3D volumeData)
    {
        CloneTexture3DToRenderTexture(volumeData, out this.volumeData, RenderTextureFormat.RFloat);
    }

    void CloneGradTex(Texture3D gradientData)
    {
        CloneTexture3DToRenderTexture(gradientData, out this.volumeGradientData, RenderTextureFormat.ARGBFloat);
    }

    void CloneTransferTex(Texture2D transferTextureData)
    {
        CloneTexture2DToRenderTexture(transferTextureData, out this.transferTexture, RenderTextureFormat.ARGBFloat);
    }

    void ReleaseTextures()
    {
        Debug.Log("Releasing render textures in " + this.name);
        this.volumeData?.Release();
        this.volumeData = null;

        this.volumeGradientData?.Release();
        this.volumeGradientData = null;

        this.transferTexture?.Release();
        this.transferTexture = null;
    }

    void CalculateActiveGridBlocks(TransferFunctionDims tfcase)
    {
        // run compute shader to get maximum opacity values and store them into this grid block
        // a) -> density -> get opacity from the transfer function
        // b) -> density, derivative -> get opacity from the 2d transfer function
        Debug.Log("Calculating active grid blocks in " + this.name + ", tf dims: " + tfcase);
        Debug.Log("Size of active grid: " + ((this.volumeData.width / this.celDim) * (this.volumeData.height / this.celDim) * (this.volumeData.volumeDepth / this.celDim)));
        this.activeGridTexture = new Texture3D(this.volumeData.width/this.celDim, this.volumeData.height/this.celDim, this.volumeData.volumeDepth / this.celDim, TextureFormat.RFloat, false);
        RenderTexture activeGridRenderTexture = new RenderTexture(this.activeGridTexture.width, this.activeGridTexture.height, 0, RenderTextureFormat.RFloat);
        activeGridRenderTexture.volumeDepth = this.activeGridTexture.depth;
        activeGridRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        activeGridRenderTexture.enableRandomWrite = true;
        activeGridRenderTexture.Create();

        int kernelId = activeGridCalculator.FindKernel("NoneTfActiveGrid");
        activeGridCalculator.SetInts("volumeDims", this.volumeData.width, this.volumeData.height, this.volumeData.volumeDepth);
        activeGridCalculator.SetInts("gridDims", activeGridRenderTexture.width, activeGridRenderTexture.height, activeGridRenderTexture.volumeDepth);
        activeGridCalculator.SetInts("gridCellSize", this.celDim, this.celDim, this.celDim);

        switch(tfcase)
        {
            case TransferFunctionDims.None:
                {
                    kernelId = activeGridCalculator.FindKernel("NoneTfActiveGrid");
                    break;
                }
            case TransferFunctionDims.Dim1:
                {
                    kernelId = activeGridCalculator.FindKernel("Tf1dActiveGrid");
                    activeGridCalculator.SetInts("tfDims", this.transferTexture.width, this.transferTexture.height);
                    activeGridCalculator.SetTexture(kernelId, "TransferFunctionData", this.transferTexture);
                    break;
                }
            case TransferFunctionDims.Dim2:
                {
                    kernelId = activeGridCalculator.FindKernel("Tf2dActiveGrid");
                    activeGridCalculator.SetInts("tfDims", this.transferTexture.width, this.transferTexture.height);
                    activeGridCalculator.SetTexture(kernelId, "TransferFunctionData", this.transferTexture);
                    activeGridCalculator.SetTexture(kernelId, "VolumeGradientData", this.volumeGradientData);
                    break;
                }
            default: Debug.LogError("Incorrect usage of TfCase in " + this.name); break;
        }
        activeGridCalculator.SetTexture(kernelId, "VolumeData", this.volumeData);
        activeGridCalculator.SetTexture(kernelId, "ActiveGridData", activeGridRenderTexture);
        
        activeGridCalculator.Dispatch(kernelId, activeGridRenderTexture.width / this.celDim + 1, activeGridRenderTexture.height / this.celDim + 1, activeGridRenderTexture.volumeDepth / this.celDim + 1);

        Graphics.CopyTexture(activeGridRenderTexture, this.activeGridTexture);
        activeGridRenderTexture.Release();
        activeGridRenderTexture = null;


        OnFinishedActiveGrid?.Invoke(this.activeGridTexture);
    }
}
