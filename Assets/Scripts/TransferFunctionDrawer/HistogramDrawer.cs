using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace com.jon_skoberne.TransferFunctionDrawer
{

    public enum Dim
    {
        D1,
        D2,
    }

    public enum Mode
    {
        Lin,
        Log,
    }

    public class HistogramDrawer : MonoBehaviour
    {
        public MeshRenderer histogramPlane;
        public ComputeShader binCounter;
        public Dim dimMode;

        private Material histogramMaterial;
        private Mode drawMode = Mode.Lin;
        private int textureDimension = 2048;

        private bool computedFlag;
        private int[] bucketValues;
        private float deltaX = 0f;
        private float deltaY = 0f;
        private Texture3D textureData;
        private RenderTexture renderTextureData;
        private RenderTexture renderTextureGradientData;
        private Texture2D histTexture;
        private int yMax;
        private int amplifier = 1;

        private void Start()
        {

        }

        private void OnDestroy()
        {
            ReleaseTmpRenderTex();
        }

        public void InitializeHistogramState(Texture3D data, Texture3D gradientData)
        {
            Debug.Log("Histogram Awake call");
            this.computedFlag = false;

            // how much "range" does each bucket cover
            this.textureData = data;
            switch (this.dimMode)
            {
                case Dim.D1:
                    {
                        this.renderTextureData = new RenderTexture(this.textureData.width, this.textureData.height, 0, RenderTextureFormat.RFloat);
                        this.renderTextureData.volumeDepth = this.textureData.depth;
                        this.renderTextureData.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                        this.renderTextureData.enableRandomWrite = true;
                        this.renderTextureData.Create();
                        Graphics.CopyTexture(this.textureData, this.renderTextureData);

                        this.bucketValues = new int[this.textureDimension];
                        break;
                    }
                case Dim.D2:
                    {
                        this.renderTextureData = new RenderTexture(this.textureData.width, this.textureData.height, 0, RenderTextureFormat.RFloat);
                        this.renderTextureData.volumeDepth = this.textureData.depth;
                        this.renderTextureData.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                        this.renderTextureData.enableRandomWrite = true;
                        this.renderTextureData.Create();
                        Graphics.CopyTexture(this.textureData, this.renderTextureData);

                        this.renderTextureGradientData = new RenderTexture(gradientData.width, gradientData.height, 0, RenderTextureFormat.ARGBFloat);
                        this.renderTextureGradientData.volumeDepth = gradientData.depth;
                        this.renderTextureGradientData.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                        this.renderTextureGradientData.enableRandomWrite = true;
                        this.renderTextureGradientData.Create();
                        Graphics.CopyTexture(gradientData, this.renderTextureGradientData);

                        this.bucketValues = new int[this.textureDimension * this.textureDimension];
                        break;
                    }

            }

            this.histTexture = new Texture2D(this.textureDimension, this.textureDimension);
            this.deltaX = 1.0f / this.textureDimension; // N buckets = texture dim 2 -> 0, 1 
            this.deltaY = 1.0f / this.textureDimension;
            this.histogramMaterial = histogramPlane.GetComponent<MeshRenderer>().sharedMaterial;

            CalculateBucketValues();
            Draw();
        }

        public void ChangeToLin()
        {
            if (this.drawMode != Mode.Lin)
            {
                this.drawMode = Mode.Lin;
                Draw();
            }
        }

        public void ChangeToLog()
        {
            if (this.drawMode != Mode.Log)
            {
                this.drawMode = Mode.Log;
                Draw();
            }
        }

        public void ChangeAmplifier(SliderEventData data)
        {
            this.amplifier = Mathf.CeilToInt(data.NewValue);
            Draw();
        }

        private void Draw()
        {
            switch(this.dimMode)
            {
                case Dim.D1:
                    {
                        Draw1DHistTexture();
                        break;
                    }
                case Dim.D2:
                    {
                        Draw2DHistTexture();
                        break;
                    }
            }
        }


        private void CalculateBucketValues()
        {
            Debug.Log("Histogram state: Computed = " + this.computedFlag + ", Tex is null: " + this.textureData == null);
            if(!this.computedFlag && this.textureData != null)
            {
                Debug.Log("Expensive Histogram computation!");

                switch(this.dimMode)
                {
                    case Dim.D1:
                        {
                            CalculateBuckets1D();
                            break;
                        }
                    case Dim.D2:
                        {
                            CalculateBuckets2D();
                            break;
                        }
                }

                this.yMax = 0;
                
                foreach (var value in this.bucketValues)
                {
                    if (this.yMax < value) this.yMax = value;
                }

                this.computedFlag = true;
                // ^^^^^ perhaps a better alternative would be to have per thread buffers and count there and then combine them in the shader, this would require some block syncs
            }

            ReleaseTmpRenderTex();
        }

        private void CalculateBuckets1D()
        {
            Debug.Log("Computing 1D buckets.");

            int kernel1D = binCounter.FindKernel("Main1D");
            ComputeBuffer bucketsBuffer = new ComputeBuffer(this.bucketValues.Length, sizeof(int));

            binCounter.SetBuffer(kernel1D, "_CountedBins", bucketsBuffer);
            binCounter.SetFloat("_DeltaX", this.deltaX);
            binCounter.SetInts("_DataDims", this.renderTextureData.width, this.renderTextureData.height, this.renderTextureData.volumeDepth);

            binCounter.SetTexture(kernel1D, "Data", this.renderTextureData);
            binCounter.Dispatch(kernel1D, this.renderTextureData.width / 8 + 1, this.renderTextureData.height / 8 + 1, this.renderTextureData.volumeDepth / 8 + 1);

            bucketsBuffer.GetData(this.bucketValues);

            bucketsBuffer.Release();
            bucketsBuffer = null;
        }

        private void CalculateBuckets2D()
        {
            Debug.Log("Computing 2D buckets.");

            int kernel2D = binCounter.FindKernel("Main2D");
            ComputeBuffer bucketsBuffer = new ComputeBuffer(this.bucketValues.Length, sizeof(int));
            
            binCounter.SetBuffer(kernel2D, "_CountedBins", bucketsBuffer);
            binCounter.SetFloat("_DeltaX", this.deltaX);
            binCounter.SetFloat("_DeltaY", this.deltaY);
            binCounter.SetInts("_DataDims", this.renderTextureGradientData.width, this.renderTextureGradientData.height, this.renderTextureGradientData.volumeDepth);
            binCounter.SetInts("_BinsDims", this.textureDimension, this.textureDimension);
            binCounter.SetInt("_MaxVal", this.renderTextureGradientData.width * this.renderTextureGradientData.height * this.renderTextureGradientData.volumeDepth);
            binCounter.SetFloat("_MaxNormGrad", Constants.maxNormalisedMagnitude);

            binCounter.SetTexture(kernel2D, "Data", this.renderTextureData);
            binCounter.SetTexture(kernel2D, "GradientData", this.renderTextureGradientData);
            binCounter.Dispatch(kernel2D, this.renderTextureGradientData.width / 8 + 1, this.renderTextureGradientData.height / 8 + 1, this.renderTextureGradientData.volumeDepth / 8 + 1);

            bucketsBuffer.GetData(this.bucketValues);

            bucketsBuffer.Release();
            bucketsBuffer = null;
        }

        private void Draw1DHistTexture()
        {
            // create new array and texture2d
            // fill this new array
            Color[] texValues = new Color[this.textureDimension * this.textureDimension];
            float yMaxLog = Mathf.CeilToInt(Mathf.Log(this.yMax, 2));

            Debug.Log("Draw 1D Hist");
            for (int bucket = 0; bucket < this.textureDimension; bucket++)
            {
                // x + y * dimension
                int numberOfElements = 0;
                if (drawMode == Mode.Lin)
                {
                    numberOfElements = TransformYRangeToTextureRange(this.bucketValues[bucket], this.yMax);
                }
                if (drawMode == Mode.Log)
                {
                    float numElements = Mathf.CeilToInt(Mathf.Log(this.bucketValues[bucket], 2));
                    numberOfElements = TransformYRangeToTextureRange(numElements, yMaxLog);
                }

                for (int yTexture = 0; yTexture < numberOfElements * this.amplifier; yTexture++)
                {
                    int linearId = bucket + yTexture * this.textureDimension;
                    if (linearId >= texValues.Length) break;
                    texValues[linearId] = new Color(1, 1, 1, 1);
                }
            }
            this.histTexture.SetPixels(texValues);
            this.histTexture.filterMode = FilterMode.Point;
            this.histTexture.Apply();
            // set texture as texture for histogram material
            this.histogramMaterial.SetTexture("_MainTex", this.histTexture);
        }

        private void Draw2DHistTexture()
        {
            Color[] texValues = new Color[this.textureDimension * this.textureDimension];

            Debug.Log("Draw 2D Hist");
            int maxCnt = -1;
            for (int bucket = 0; bucket < this.bucketValues.Length; bucket++)
            {
                if (this.bucketValues[bucket] > 0)
                {
                    texValues[bucket] = Color.white;
                    maxCnt = bucket / 2048;
                }
            }
            Debug.Log("Max value: " + maxCnt);
            this.histTexture.SetPixels(texValues);
            this.histTexture.filterMode = FilterMode.Point;
            this.histTexture.Apply();
            // set texture as texture for histogram material
            this.histogramMaterial.SetTexture("_MainTex", this.histTexture);
        }

        private int GetBucket(float density)
        {
            return (int)Mathf.Ceil(density / this.deltaX) - 1; // round up and minus 1 for the bucket -> otherwise you get 256 bucket id for density 1.0, and you should get 255 :) 
        }

        private int TransformYRangeToTextureRange(float value, float yMax)
        {
            /*
             * new min + (new max - new min) / (old max - old min) * value
             * textureXDimensions - 1 cuz, dim = 2, values go from 0, 1. so max is 1
             */
            return (int)(0 + (((this.textureDimension - 1) - 0) / (yMax - 0)) * value);
        }

        private void ReleaseTmpRenderTex()
        {
            this.renderTextureData?.Release();
            this.renderTextureGradientData?.Release();
        }
    }
}

