using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.jon_skoberne.Reader;
using Microsoft.MixedReality.Toolkit.UI;

namespace com.jon_skoberne.UI
{
    public class ShaderMenu : MonoBehaviour
    {

        public GameObject volumeCube;

        [Header("Light color sliders:")]
        public SliderLogic[] sliders_color;
        [Header("Light position sliders:")]
        public SliderLogic[] sliders_position;

        private const string LightPositionShaderName = "_LightPosition";
        private const string LightColorShaderName = "_LightColor";

        [SerializeField]
        private Material material;
        [SerializeField]
        private GameObject sliderCollection;

        private ImageDataObject ido;


        private void Awake()
        {
            InitCubeMaterial();
        }

        private void InitCubeMaterial()
        {
            ido = Resources.Load<ImageDataObject>("VolumeData/LoadedImageObject");
            Renderer mt = this.volumeCube.GetComponent<Renderer>();
            mt.sharedMaterial = this.material;
            SetRandomTextureInShader();

            SetInitialKeywords();
        }

        private void SetInitialKeywords()
        {
            EnableMip();
            EnableNormalData();
            EnableNormalGradient();
            DisableLighting();
        }

        private void SetRandomTextureInShader()
        {
            Texture3D randomValuesTex = CreateRandomTexture(this.ido.dimX, this.ido.dimY, this.ido.dimZ);
            this.material.SetTexture("_RandomTex", randomValuesTex);
        }

        private Texture3D CreateRandomTexture(int width, int height, int depth)
        {
            Texture3D tex3Drandoms = new Texture3D(width, height, depth, TextureFormat.RGBAFloat, false);
            tex3Drandoms.wrapMode = TextureWrapMode.Clamp;
            Color[] values = CreateRandomBuffer(width * height * depth);
            tex3Drandoms.SetPixels(values);
            tex3Drandoms.Apply();

            return tex3Drandoms;
        }


        private Color[] CreateRandomBuffer(int size)
        {
            Color[] buffer = new Color[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
            }
            return buffer;
        }


        #region RenderMode
        public void EnableMip()
        {
            Debug.Log("Enabling MIP mode!");
            this.material.EnableKeyword("MODE_MIP");
            this.material.DisableKeyword("MODE_DVR");
            this.material.DisableKeyword("MODE_SRF");
            this.material.DisableKeyword("MODE_CINEMA");
        }

        public void EnableDvr()
        {
            Debug.Log("Enabling DVR mode!");
            this.material.DisableKeyword("MODE_MIP");
            this.material.EnableKeyword("MODE_DVR");
            this.material.DisableKeyword("MODE_SRF");
            this.material.DisableKeyword("MODE_CINEMA");
        }

        public void EnableSur()
        {
            Debug.Log("Enabling SRF mode!");
            this.material.DisableKeyword("MODE_MIP");
            this.material.DisableKeyword("MODE_DVR");
            this.material.EnableKeyword("MODE_SRF");
            this.material.DisableKeyword("MODE_CINEMA");
        }

        public void EnableVol()
        {
            Debug.Log("Enabling CINEMA mode!");
            this.material.DisableKeyword("MODE_MIP");
            this.material.DisableKeyword("MODE_DVR");
            this.material.DisableKeyword("MODE_SRF");
            this.material.EnableKeyword("MODE_CINEMA");
        }
        #endregion

        #region FilteredData
        public void EnableNormalData()
        {
            Debug.Log("Setting normal data!");
            this.material.SetTexture("_CompTopTex", ido.GetTexture3D(0));
        }

        public void EnableGaussData()
        {
            Debug.Log("Setting gauss filtered data!");
            this.material.SetTexture("_CompTopTex", ido.GetTexture3D(1));
        }

        public void EnableNormalGradient()
        {
            Debug.Log("Setting normal gradient!");
            this.material.SetTexture("_GradientTex", ido.GetTexture3DGradient(0));
        }

        public void EnableGaussGradient()
        {
            Debug.Log("Setting gauss filtered gradient!");
            this.material.SetTexture("_GradientTex", ido.GetTexture3DGradient(1));
        }

        public void EnableSobelGradient()
        {
            Debug.Log("Setting sobel generated gradient!");
            this.material.SetTexture("_GradientTex", ido.GetTexture3DGradient(2));
        }
        #endregion

        #region LightingMode
        public void DisableLighting()
        {
            Debug.Log("Disable lighting!");
            this.material.DisableKeyword("LOCAL_LIGHTING_BP");
            this.material.DisableKeyword("LOCAL_LIGHTING_CT");
        }

        public void EnableBlinnPhong()
        {
            Debug.Log("Enable Blinn Phong lighting!");
            this.material.EnableKeyword("LOCAL_LIGHTING_BP");
            this.material.DisableKeyword("LOCAL_LIGHTING_CT");
        }

        public void EnableCookTorrance()
        {
            Debug.Log("Enable CookTorrance lighting!");
            this.material.DisableKeyword("LOCAL_LIGHTING_BP");
            this.material.EnableKeyword("LOCAL_LIGHTING_CT");
        }
        #endregion

        #region TransferFunction Sampling
        public void EnableTfNoSample()
        {
            Debug.Log("Disable TF sampling!");
            this.material.DisableKeyword("TF1D_MODE");
            this.material.DisableKeyword("TF2D_MODE");
        }

        public void EnableTf1dSample()
        {
            Debug.Log("Enable TF 1D sampling!");
            this.material.EnableKeyword("TF1D_MODE");
            this.material.DisableKeyword("TF2D_MODE");
        }

        public void EnableTf2dSample()
        {
            Debug.Log("Enable TF 2D sampling!");
            this.material.DisableKeyword("TF1D_MODE");
            this.material.EnableKeyword("TF2D_MODE");
        }
        #endregion

        #region Setting Material Values

        public void SetMinValue(SliderEventData data)
        {
            Debug.Log("Setting min val: " + data.NewValue);
            this.material.SetFloat("_MinVal", data.NewValue);
        }

        public void SetMaxValue(SliderEventData data)
        {
            Debug.Log("Setting max val: " + data.NewValue);
            this.material.SetFloat("_MaxVal", data.NewValue);
        }

        public void SetXRange(SliderEventData data)
        {
            Debug.Log("Setting xrange val: " + data.NewValue);
            this.material.SetFloat("_XRange", data.NewValue);
        }

        public void SetYRange(SliderEventData data)
        {
            Debug.Log("Setting yrange val: " + data.NewValue);
            this.material.SetFloat("_YRange", data.NewValue);
        }

        public void SetZRange(SliderEventData data)
        {
            Debug.Log("Setting zrange val: " + data.NewValue);
            this.material.SetFloat("_ZRange", data.NewValue);
        }

        public void SetShineLight(SliderEventData data)
        {
            Debug.Log("Setting light shine val: " + data.NewValue);
            this.material.SetFloat("_Shininess", data.NewValue);
        }
        
        public void SetPowerLight(SliderEventData data)
        {
            Debug.Log("Setting light power val: " + data.NewValue);
            this.material.SetFloat("_LightPower", data.NewValue);
        }

        public void SetNumSteps(SliderEventData data)
        {
            Debug.Log("Setting number of steps in raymarch: " + data.NewValue);
            this.material.SetInt("_NumSteps", Mathf.RoundToInt(data.NewValue));
        }

        public void SetSuperSampleScaleSteps(SliderEventData data)
        {
            Debug.Log("Setting SS scale: " + data.NewValue);
            this.material.SetInt("_ScaleSuperSample", Mathf.RoundToInt(data.NewValue));
        }

        public void SetG(SliderEventData data)
        {
            Debug.Log("Setting G for CINEMA mode: " + data.NewValue);
            this.material.SetFloat("_G", data.NewValue);
        }

        public void SetSigmaT(SliderEventData data)
        {
            Debug.Log("Setting SigmaT for CINEMA mode: " + data.NewValue);
            this.material.SetFloat("_SigmaT", data.NewValue);
        }

        public void SetCinemaRayNumber(SliderEventData data)
        {
            Debug.Log("Setting RayNumber for CINEMA mode: " + data.NewValue);
            this.material.SetFloat("_RayNumber", data.NewValue);
        }

        public void SetCinemaRayBounces(SliderEventData data)
        {
            Debug.Log("Setting RayBounces for CINEMA mode: " + data.NewValue);
            this.material.SetFloat("_RayBounces", data.NewValue);
        }

        public void SetLightColor()
        {
            VectorSlidersColor();
        }

        public void SetLightPosition()
        {
            VectorSlidersPosition();
        }

        public void ToggleSliderCollection()
        {
            sliderCollection.SetActive(!sliderCollection.activeSelf);
        }

        private void VectorSlidersColor()
        {
            Vector4 value = Vector4.zero;
            Color col = Color.HSVToRGB(sliders_color[0].CurrentValue(), sliders_color[1].CurrentValue(), sliders_color[2].CurrentValue());
            value[0] = col.r;
            value[1] = col.g;
            value[2] = col.b;
            value[3] = sliders_color[3].CurrentValue();
            Debug.Log("Setting Light Color: " + value);
            this.material.SetVector(LightColorShaderName, value);
        }

        private void VectorSlidersPosition()
        {
            Vector4 value = Vector4.one;
            for (int i = 0; i < sliders_position.Length; i++)
            {
                value[i] = sliders_position[i].CurrentValue();
            }
            Debug.Log("Setting Light Position: " + value);
            this.material.SetVector(LightPositionShaderName, value);
        }

        #endregion

    }
}

