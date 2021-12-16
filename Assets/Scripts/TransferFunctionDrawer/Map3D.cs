using com.jon_skoberne.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map3D : MonoBehaviour
{
    public struct SelectedHistValues
    {
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public float filter_dim;

        public SelectedHistValues(float pos_x, float pos_y, float pos_z, float filter_dim)
        {
            this.pos_x = pos_x;
            this.pos_y = pos_y;
            this.pos_z = pos_z;
            this.filter_dim = filter_dim;
        }
    }

    public delegate void OnSelectedMapRegion(SelectedHistValues s);
    public static OnSelectedMapRegion onSelectedMapRegion;


    public SliderLogic slider_pos_x;
    public SliderLogic slider_pos_y;
    public SliderLogic slider_pos_z;
    public SliderLogic slider_filter_dim;


    [SerializeField]
    int map_dim = 25;
    [SerializeField]
    ComputeShader compute_filter;
    [SerializeField]
    GameObject map_cube;

    RenderTexture map_rt;
    Texture3D map_t3d;
    float pos_x = 0.5f;
    float pos_y = 0.5f;
    float pos_z = 0.5f;
    float filter_dim = 0.5f;

    void Awake()
    {
        InitialMapSetup();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        RegisterSliderEvents();
    }

    void OnDisable()
    {
        UnregisterSliderEvents();
    }

    void OnDestroy()
    {
        ClearRenderTex();
    }

    void InitialMapSetup()
    {
        this.map_t3d = new Texture3D(this.map_dim, this.map_dim, this.map_dim, TextureFormat.RFloat, false);
        this.map_t3d.wrapMode = TextureWrapMode.Clamp;

        this.map_rt = new RenderTexture(this.map_dim, this.map_dim, 0, RenderTextureFormat.RFloat);
        this.map_rt.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        this.map_rt.volumeDepth = this.map_dim;
        this.map_rt.enableRandomWrite = true;
        this.map_rt.useMipMap = false;
        this.map_rt.wrapMode = TextureWrapMode.Clamp;
        this.map_rt.Create();

        this.map_cube.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", this.map_t3d);

        Draw3dMap();
    }

    void RegisterSliderEvents()
    {
        slider_pos_x.changedValue.AddListener(UpdateValues);
        slider_pos_y.changedValue.AddListener(UpdateValues);
        slider_pos_z.changedValue.AddListener(UpdateValues);
        slider_filter_dim.changedValue.AddListener(UpdateValues);
    }

    void UnregisterSliderEvents()
    {
        slider_pos_x.changedValue.RemoveAllListeners();
        slider_pos_y.changedValue.RemoveAllListeners();
        slider_pos_z.changedValue.RemoveAllListeners();
        slider_filter_dim.changedValue.RemoveAllListeners();
    }

    void ClearRenderTex()
    {
        this.map_rt.Release();
        this.map_rt = null;
    }

    void UpdateValues(SliderEventData _slv)
    {
        Debug.Log("Map: updating values");
        this.pos_x = slider_pos_x.CurrentValue();
        this.pos_y = slider_pos_y.CurrentValue();
        this.pos_z = slider_pos_z.CurrentValue();
        this.filter_dim = slider_filter_dim.CurrentValue();

        Draw3dMap();
    }

    void Draw3dMap()
    {
        Debug.Log("Map: Drawing in compute shader");
        Draw();
        onSelectedMapRegion?.Invoke(GetSelectedHistValues());
    }

    void Draw()
    {
        this.compute_filter.SetFloats("MapDims", this.map_dim, this.map_dim, this.map_dim);
        this.compute_filter.SetFloats("DimValues", this.pos_x, this.pos_y, this.pos_z, this.filter_dim);
        this.compute_filter.SetTexture(0, "Result", this.map_rt);
        this.compute_filter.Dispatch(0, this.map_rt.width / 8 + 1, this.map_rt.height / 8 + 1, this.map_rt.volumeDepth / 8 + 1);
        Graphics.CopyTexture(this.map_rt, this.map_t3d);
    }

    SelectedHistValues GetSelectedHistValues()
    {
        return new SelectedHistValues(this.pos_x, this.pos_y, this.pos_z, this.filter_dim);
    }
}
