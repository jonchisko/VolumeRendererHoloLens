using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace com.jon_skoberne.TransferFunctionDrawer
{

    public enum Mode
    {
        Lin,
        Log,
    }

    public class HistogramDrawer : MonoBehaviour
    {
        public MeshRenderer histogramPlane;
        public ComputeShader binCounter;
        
        private Material histogramMaterial;
        private Mode drawMode = Mode.Lin;
        private int textureDimension = 2048;

        private Dictionary<int, int> bucketDictionary;
        private bool computedFlag;
        private int[] bucketValues;
        private float delta = 0f;
        private Texture3D textureData;
        private RenderTexture renderTextureData;
        private Texture2D histTexture;
        private int yMax;
        private int amplifier = 1;

        private void Awake()
        {
            Debug.Log("Histogram Awake call");
            this.computedFlag = false;

            // how much "range" does each bucket cover
            this.textureData = Resources.Load<Texture3D>("VolumeData/" + VolumeAssetNames.data3d);
            this.renderTextureData = new RenderTexture(this.textureData.width, this.textureData.height, 0, RenderTextureFormat.RFloat);
            this.renderTextureData.volumeDepth = this.textureData.depth;
            this.renderTextureData.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            this.renderTextureData.enableRandomWrite = true;
            this.renderTextureData.Create();

            Graphics.CopyTexture(this.textureData, this.renderTextureData);

            this.bucketValues = new int[this.textureDimension];
            this.histTexture = new Texture2D(this.textureDimension, this.textureDimension);
            this.delta = 1.0f / this.textureDimension; // N buckets = texture dim 2 -> 0, 1 
            //this.bucketDictionary = new Dictionary<int, int>();
            this.histogramMaterial = histogramPlane.GetComponent<MeshRenderer>().sharedMaterial;

            Calculate();
        }

        public void ChangeToLin()
        {
            if (this.drawMode != Mode.Lin)
            {
                this.drawMode = Mode.Lin;
                Calculate();
            }
        }

        public void ChangeToLog()
        {
            if (this.drawMode != Mode.Log)
            {
                this.drawMode = Mode.Log;
                Calculate();
            }
        }

        public void ChangeAmplifier(SliderEventData data)
        {
            this.amplifier = Mathf.CeilToInt(data.NewValue);
            Calculate();
        }

        private void Calculate()
        {
            CalculateBucketValues();
            DrawHistTexture();
        }


        private void CalculateBucketValues()
        {
            if(!this.computedFlag && this.textureData != null)
            {
                Debug.Log("Expensive Histogram computation!");
                ComputeBuffer bucketsBuffer = new ComputeBuffer(this.bucketValues.Length, sizeof(int));
                bucketsBuffer.SetData(this.bucketValues);

                binCounter.SetBuffer(0, "_CountedBins", bucketsBuffer);
                binCounter.SetFloat("_Delta", this.delta);

                binCounter.SetTexture(0, "Data", this.renderTextureData);
                binCounter.Dispatch(0, this.renderTextureData.width / 8, this.renderTextureData.height / 8, this.renderTextureData.volumeDepth / 8);

                bucketsBuffer.GetData(this.bucketValues);

                bucketsBuffer.Release();
                bucketsBuffer = null;


                this.yMax = 0;
                
                foreach (var value in this.bucketValues)
                {
                    if (this.yMax < value) this.yMax = value;
                }

                this.computedFlag = true;
                /* ^^^^^ perhaps a better alternative would be to have per thread buffers and count there and then combine them in the shader, this would require some block syncs
                //CPU version:
                var pixelColors = this.textureData.GetPixels();
                float density;
                foreach (var color in pixelColors)
                {
                    density = color.r;
                    if (density == 0.0f) continue; // ignore empty space ...
                    int bucket = GetBucket(density);
                    if (this.bucketDictionary.ContainsKey(bucket))
                    {
                        this.bucketDictionary[bucket] += 1;
                    }
                    else
                    {
                        this.bucketDictionary[bucket] = 1;
                    }
                }

                this.yMax = 0;
                foreach (var value in this.bucketDictionary.Values)
                {
                    if (this.yMax < value) this.yMax = value;
                }*/
            }
        }

        private void DrawHistTexture()
        {
            // create new array and texture2d
            // fill this new array
            Color[] texValues = new Color[this.textureDimension * this.textureDimension];
            float yMaxLog = Mathf.CeilToInt(Mathf.Log(this.yMax, 2));

            for (int bucket = 0; bucket < this.textureDimension; bucket++)
            {
                // x + y * dimension
                int numberOfElements;
                if (drawMode == Mode.Lin)
                {
                    numberOfElements = TransformYRangeToTextureRange(this.bucketValues[bucket], this.yMax);
                } else
                {
                    // if (drawMode == Mode.Log)
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
            this.histogramMaterial.SetTexture("_TransferTexture", this.histTexture);
        }

        private int GetBucket(float density)
        {
            return (int)Mathf.Ceil(density / this.delta) - 1; // round up and minus 1 for the bucket -> otherwise you get 256 bucket id for density 1.0, and you should get 255 :) 
        }

        private int TransformYRangeToTextureRange(float value, float yMax)
        {
            /*
             * new min + (new max - new min) / (old max - old min) * value
             * textureXDimensions - 1 cuz, dim = 2, values go from 0, 1. so max is 1
             */
            return (int)(0 + (((this.textureDimension - 1) - 0) / (yMax - 0)) * value);
        }
    }
}

