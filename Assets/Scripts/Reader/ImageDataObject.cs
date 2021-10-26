﻿using System;
using System.Threading.Tasks;
using itk.simple;
using UnityEngine;
using UnityEditor;
using com.jon_skoberne.HelperServices;

namespace com.jon_skoberne.Reader 
{
    [System.Serializable]
    public class SaveImageDataObject
    {
        public ItkReadFileSupport.ReadType readType;
        public float[] dataArray;
        public float[] tex3D;
        public float[] tex3Dgauss;
        public float[] tex3Dgradient;
        public float[] tex3DgradientGauss;
        public float[] tex3DgradientSobel;

        public float minValue, maxValue;
        public int dimX, dimY, dimZ;
        public string filePath;

        public SaveImageDataObject(ItkReadFileSupport.ReadType readType, float[] dataArray, float minValue, float maxValue, int dimX, int dimY, int dimZ, string filePath)
        {
            this.readType = readType;
            this.dataArray = dataArray;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.dimX = dimX;
            this.dimY = dimY;
            this.dimZ = dimZ;
            this.filePath = filePath;

            this.tex3D = new float[this.dimX * this.dimY * this.dimZ];
            this.tex3Dgauss = new float[this.dimX * this.dimY * this.dimZ];

            this.tex3Dgradient = new float[this.dimX * this.dimY * this.dimZ * 4];
            this.tex3DgradientGauss = new float[this.dimX * this.dimY * this.dimZ * 4];
            this.tex3DgradientSobel = new float[this.dimX * this.dimY * this.dimZ * 4];
        }

        public SaveImageDataObject(ItkReadFileSupport.ReadType readType, float[] dataArray, float[] tex3D, float[] tex3Dgauss, float[] tex3Dgradient, float[] tex3DgradientGauss,
            float[] tex3DgradientSobel, float minValue, float maxValue, int dimX, int dimY, int dimZ, string filePath)
        {
            this.readType = readType;
            this.dataArray = dataArray;
            this.tex3D = tex3D;
            this.tex3Dgauss = tex3Dgauss;
            this.tex3Dgradient = tex3Dgradient;
            this.tex3DgradientGauss = tex3DgradientGauss;
            this.tex3DgradientSobel = tex3DgradientSobel;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.dimX = dimX;
            this.dimY = dimY;
            this.dimZ = dimZ;
            this.filePath = filePath;
        }
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "LoadedImageObject", menuName = "ImageData")]
    public class ImageDataObject : ScriptableObject
    {

        public delegate void ReadingEvent(ImageDataObject ido);
        public static event ReadingEvent OnReadingError;
        public static event ReadingEvent OnReadingSuccess;
        public static event ReadingEvent OnLoadingSuccess;


        public ComputeShader imgCalculator;
        public ComputeShader imgFilter;
        public ComputeShader imgCopy;

        private SaveImageDataObject serializableImageObject;

        private ItkReadFileSupport.ReadType readType;
        public Texture3D tex3D;
        public Texture3D tex3Dgauss;
        public Texture3D tex3Dgradient;
        public Texture3D tex3DgradientGauss;
        public Texture3D tex3DgradientSobel;

        private float minValue, maxValue;
        private int dimX, dimY, dimZ;
        private string filePath;

        private float[] dataArray;
        private RenderTexture rt1;
        private RenderTexture rt2;
        private RenderTexture rtNormData;
        private RenderTexture rtNormTmp;
        private RenderTexture rtSobel;

        public void CreateImageDataObject(ItkReadFileSupport.ReadType readType, string filePath)
        {
            this.readType = readType;
            this.filePath = filePath;
            InitializeValues(filePath);
        }

        #region Get methods

        public string GetFilePath()
        {
            return filePath;
        }

        public int GetHeight()
        {
            return dimY;
        }

        public int GetWidth()
        {
            return dimX;
        }

        public int GetDepth()
        {
            return dimZ;
        }

        public ItkReadFileSupport.ReadType GetReadType()
        {
            return readType;
        }

        public Texture3D GetTexture3D(int filterMode=0)
        {
            switch(filterMode)
            {
                case 0: return this.tex3D;
                case 1: return this.tex3Dgauss;
                default: throw new SystemException("Filter mode does not exist!");
            }
        }

        public Texture3D GetTexture3DGradient(int mode=0)
        {
            switch(mode)
            {
                case 0: return this.tex3Dgradient;
                case 1: return this.tex3DgradientGauss;
                case 2: return this.tex3DgradientSobel;
                default: throw new SystemException("Gradient mode does not exist!");
            }
        }

        #endregion

        #region Reader methods

        private void InitializeValues(string filePathSource)
        {

            try
            {
                ImageFileReader reader = new ImageFileReader();
                reader.SetImageIO(ItkReadFileSupport.GetIoName(readType));
                reader.SetFileName(filePathSource);
                Image image = reader.Execute();
                Debug.Log("Image dimensions: " + image.GetDimension());
                Debug.Log("Image width: " + image.GetWidth() + ", height: " + image.GetHeight() + ", depth: " + image.GetDepth());
                ConvertPointerToArray(image);
                SetDimensions(image);
                SetMaxMinValues();
                this.serializableImageObject = new SaveImageDataObject(this.readType, this.dataArray, this.minValue, this.maxValue, this.dimX, this.dimY, this.dimZ, this.filePath);
                //SetTextures();
                //gpu
                CreateRenderTextures();
                CreateTextures3D();
                Compute3DtexGPU();
                //
                OnReadingSuccess?.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception occured in ReadFile: " + ex.Message + "\nStack trace: " + ex.StackTrace);
                OnReadingError?.Invoke(this);
            }
        }

        #endregion

        #region Converting to 3D textures methods 
        public void ConvertPointerToArray(Image image)
        {
            var input = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);
            var size = input.GetSize(); // example [256, 256]
            int length = 1;
            for (int dim = 0; dim < input.GetDimension(); dim++) // above example, dimension=2
            {
                length *= (int)size[dim];
            }
            // length is now dimension1 * dimension2 => 256*256 =aprox= 64k for above example
            IntPtr buffer = input.GetBufferAsFloat();
            float[] imageFloatArray = new float[length];
            unsafe
            {
                float* floatPointer = (float*)buffer.ToPointer();
                Parallel.For(0, length, index => { imageFloatArray[index] = floatPointer[index]; });
                /*for (int i = 0; i < length; i++)
                {
                    imageFloatArray[i] = floatPointer[i];
                }*/
            }
            this.dataArray = imageFloatArray;
        }

        private void SetMaxMinValues()
        {
            this.minValue = int.MaxValue;
            this.maxValue = int.MinValue;
            for (int i = 0; i < this.dataArray.Length; i++)
            {
                var value = dataArray[i];
                this.minValue = Mathf.Min(this.minValue, value);
                this.maxValue = Mathf.Max(this.maxValue, value);
            }
        }

        private void SetTextures()
        {
            this.tex3D = ComputeTexture3D();
            this.tex3Dgradient = ComputeTexture3DGradient();

            this.tex3Dgauss = GetFilteredTexture(tex3D.GetPixels(), TextureFormat.RGBAFloat, 0);
            this.tex3DgradientGauss = GetFilteredTexture(tex3Dgradient.GetPixels(), TextureFormat.RGBAFloat, 0);
            this.tex3DgradientSobel = GetFilteredTexture(tex3D.GetPixels(), TextureFormat.RGBAFloat, 1);

            //CreateAssetsFromTextures();
        }

        private void CreateAssetsFromTextures()
        {
            CreateTextureAsset(this.tex3D, VolumeAssetNames.data3d);
            CreateTextureAsset(this.tex3Dgradient, VolumeAssetNames.data3dGradient);
            CreateTextureAsset(this.tex3Dgauss, VolumeAssetNames.data3dGauss);
            CreateTextureAsset(this.tex3DgradientGauss, VolumeAssetNames.data3dGradientGauss);
            CreateTextureAsset(this.tex3DgradientSobel, VolumeAssetNames.data3dGradientSobel);
        }

        private void ReleaseTempRenderTextures()
        {
            Debug.Log("Releasing render textures!");
            this.rt1.Release();
            this.rt2.Release();
            this.rtSobel.Release();
            this.rtNormData.Release();
            this.rtNormTmp.Release();
        }

        private void CreateRenderTextures()
        {
            Debug.Log("Creating render textures!");
            this.rt1 = new RenderTexture(this.dimX, this.dimY, 0, RenderTextureFormat.ARGBFloat);
            this.rt1.volumeDepth = this.dimZ;
            this.rt1.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            this.rt1.enableRandomWrite = true;
            this.rt1.Create();

            this.rt2 = new RenderTexture(this.dimX, this.dimY, 0, RenderTextureFormat.ARGBFloat);
            this.rt2.volumeDepth = this.dimZ;
            this.rt2.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            this.rt2.enableRandomWrite = true;
            this.rt2.Create();

            this.rtSobel = new RenderTexture(this.dimX, this.dimY, 0, RenderTextureFormat.ARGBFloat);
            this.rtSobel.volumeDepth = this.dimZ;
            this.rtSobel.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            this.rtSobel.enableRandomWrite = true;
            this.rtSobel.Create();

            this.rtNormData = new RenderTexture(this.dimX, this.dimY, 0, RenderTextureFormat.RFloat);
            this.rtNormData.volumeDepth = this.dimZ;
            this.rtNormData.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            this.rtNormData.enableRandomWrite = true;
            this.rtNormData.Create();

            this.rtNormTmp = new RenderTexture(this.dimX, this.dimY, 0, RenderTextureFormat.RFloat);
            this.rtNormTmp.volumeDepth = this.dimZ;
            this.rtNormTmp.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            this.rtNormTmp.enableRandomWrite = true;
            this.rtNormTmp.Create();
        }

        private void CreateTextures3D()
        {
            if(this.tex3D == null) // if one is null, all should be null
            {
                this.tex3D = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RFloat, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                this.tex3Dgauss = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RFloat, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                this.tex3Dgradient = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RGBAFloat, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                this.tex3DgradientGauss = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RGBAFloat, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                this.tex3DgradientSobel = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RGBAFloat, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        private void Compute3DtexGPU()
        {
            Debug.Log("Computing on gpu!");
            GetNormalizedDataGpu(this.tex3D);
            CopyTexToFloats(this.rtNormData, this.serializableImageObject.tex3D, true);

            GetCentralDiffGradientGpu(this.tex3Dgradient);
            CopyTexToFloats(this.rt2, this.serializableImageObject.tex3Dgradient, false);

            GetGaussFilterGpu(this.rt2, this.rtSobel, this.rt1, this.tex3DgradientGauss);
            CopyTexToFloats(this.rtSobel, this.serializableImageObject.tex3DgradientGauss, false);

            GetGaussFilterGpu(this.rtNormData, this.rtNormTmp, this.rt2, this.tex3Dgauss);
            CopyTexToFloats(this.rtNormTmp, this.serializableImageObject.tex3Dgauss, true);

            GetSobelFilterGpu(this.rt1, this.rt2, this.rtSobel, this.tex3DgradientSobel);
            CopyTexToFloats(this.rtSobel, this.serializableImageObject.tex3DgradientSobel, false);
            //CreateAssetsFromTextures();
            ReleaseTempRenderTextures();
        }

        private void SetDimensions(Image image)
        {
            this.dimX = (int)image.GetWidth();
            this.dimY = (int)image.GetHeight();
            this.dimZ = (int)image.GetDepth();
        }

        private Texture3D ComputeTexture3D()
        {
            Texture3D tex3D = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RGBAFloat, false);
            tex3D.wrapMode = TextureWrapMode.Clamp;

            Color[] cols = new Color[this.dataArray.Length];
            float range = this.maxValue - this.minValue;
            for (int i = 0; i < this.dataArray.Length; i++)
            {
                cols[i] = new Color((this.dataArray[i] - this.minValue)/range, 0f, 0f, 0f);
            }
            tex3D.SetPixels(cols);
            tex3D.Apply();
            return tex3D;
        }

        private Texture3D ComputeTexture3DGradient()
        {
            Texture3D tex3DGradient = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RGBAFloat, false);
            tex3DGradient.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[this.dataArray.Length];
            float range = this.maxValue - this.minValue;
            for (int x = 0; x < this.dimX; x++)
            {
                for (int y = 0; y < this.dimY; y++)
                {
                    for (int z = 0; z < this.dimZ; z++)
                    {
                        int i = x + y * this.dimX + z * (this.dimX * this.dimY);
                        cols[i] = GetGradientCentralDifferences(x, y, z, range);
                    }
                }
            }
            tex3DGradient.SetPixels(cols);
            tex3DGradient.Apply();

            return tex3DGradient;
        }

        public SaveImageDataObject GetSerializableImageObject()
        {
            return this.serializableImageObject;
        }

        public void ResetSerializableImageObject()
        {
            this.serializableImageObject.tex3D = null;
            this.serializableImageObject.tex3Dgauss = null;

            this.serializableImageObject.tex3Dgradient = null;
            this.serializableImageObject.tex3DgradientGauss = null;
            this.serializableImageObject.tex3DgradientSobel = null;
        }

        public void DeserializeIntoImageDataObject(SaveImageDataObject saveObject)
        {
            this.readType = saveObject.readType;
            this.dataArray = saveObject.dataArray;
            this.minValue = saveObject.minValue;
            this.maxValue = saveObject.maxValue;
            this.dimX = saveObject.dimX;
            this.dimY = saveObject.dimY;
            this.dimZ = saveObject.dimZ;
            this.filePath = saveObject.filePath;

            Debug.Log("Deserialized save into ido: " + "X: " + this.dimX + ", Y: " + this.dimY + ", Z: " + this.dimZ);

            CreateRenderTextures();
            CreateTextures3D();

            CopyFloatsToTex(saveObject.tex3D, this.rtNormTmp, this.tex3D, true);
            CopyFloatsToTex(saveObject.tex3Dgauss, this.rtNormTmp, this.tex3Dgauss, true);
            CopyFloatsToTex(saveObject.tex3Dgradient, this.rt1, this.tex3Dgradient, false);
            CopyFloatsToTex(saveObject.tex3DgradientGauss, this.rt1, this.tex3DgradientGauss, false);
            CopyFloatsToTex(saveObject.tex3DgradientSobel, this.rt1, this.tex3DgradientSobel, false);

            //CreateAssetsFromTextures();
            ReleaseTempRenderTextures();

            OnLoadingSuccess?.Invoke(this);
        }

        private void CopyFloatsToTex(float[] data, RenderTexture destTex, Texture3D tex, bool isOnlyRedChannel)
        {
            // copy data to render tex with compute shader
            ComputeBuffer floatsBuffer = new ComputeBuffer(data.Length, 1 * sizeof(float));
            floatsBuffer.SetData(data);

            int kernelid = imgCopy.FindKernel("TransferToTex");

            imgCopy.SetBuffer(kernelid, "floatsBuffer", floatsBuffer);
            imgCopy.SetInts("dims", this.dimX, this.dimY, this.dimZ);
            imgCopy.SetBool("onlyRedChannel", isOnlyRedChannel);
            imgCopy.SetTexture(kernelid, "tex", destTex);

            imgCopy.Dispatch(kernelid, destTex.width / 8, destTex.height / 8, destTex.volumeDepth / 8);

            floatsBuffer.Release();
            floatsBuffer = null;

            // copy render tex to tex3d with graphics blit
            Graphics.CopyTexture(destTex, tex);
        }

        private void CopyTexToFloats(RenderTexture tex, float[] data, bool isOnlyRedChannel)
        {
            ComputeBuffer floatsBuffer = new ComputeBuffer(data.Length, 1 * sizeof(float));
            floatsBuffer.SetData(data);

            int kernelid = imgCopy.FindKernel("TransferToFloats");

            imgCopy.SetBuffer(kernelid, "floatsBuffer", floatsBuffer);
            imgCopy.SetInts("dims", this.dimX, this.dimY, this.dimZ);
            imgCopy.SetBool("onlyRedChannel", isOnlyRedChannel);
            imgCopy.SetTexture(kernelid, "tex", tex);

            imgCopy.Dispatch(kernelid, tex.width / 8, tex.height / 8, tex.volumeDepth / 8);

            floatsBuffer.GetData(data);

            floatsBuffer.Release();
            floatsBuffer = null;
        }

        private void CreateTextureAsset(Texture3D tex, string assetName)
        {
            AssetDatabase.CreateAsset(tex, VolumeAssetNames.assetFolderPath + assetName + ".asset");
        }

        private Color GetGradientCentralDifferences(int x, int y, int z, float range)
        {
            int wholeSliceOffset = this.dimX * this.dimY;
            var currentValue = (this.dataArray[x + y * this.dimX + z * wholeSliceOffset] - this.minValue)/range;
            var datax1 = (this.dataArray[Math.Max(x-1, 0) + y * this.dimX + z * wholeSliceOffset] - this.minValue)/range;
            var datax2 = (this.dataArray[Math.Min(x + 1, this.dimX-1) + y * this.dimX + z * wholeSliceOffset] - this.minValue)/range;
            var datay1 = (this.dataArray[x + Math.Max(y-1, 0) * this.dimX + z * wholeSliceOffset] - this.minValue)/range;
            var datay2 = (this.dataArray[x + Math.Min(y + 1, this.dimY-1) * this.dimX + z * wholeSliceOffset] - this.minValue)/range;
            var dataz1 = (this.dataArray[x + y * this.dimX + Math.Max(z-1, 0) * wholeSliceOffset] - this.minValue)/range;
            var dataz2 = (this.dataArray[x + y * this.dimX + Math.Min(z+1, this.dimZ-1) * wholeSliceOffset] - this.minValue)/range;
            return new Color((datax1 - datax2) / 2, (datay1 - datay2) / 2, (dataz1 - dataz2) / 2, currentValue); // maybe different order of subtraction?
        }

        private Texture3D GetFilteredTexture(Color[] pixels, TextureFormat format, int filterMode=0)
        {
            Texture3D tex3D = new Texture3D(this.dimX, this.dimY, this.dimZ, format, false);
            tex3D.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[this.dataArray.Length];

            switch (filterMode)
            {
                case 0: cols = Filters.FilterWithGauss(pixels, this.dimX, this.dimY, this.dimZ); break;
                case 1: cols = Filters.FilterWithSobel(pixels, this.dimX, this.dimY, this.dimZ); break;
            }
            tex3D.SetPixels(cols);
            tex3D.Apply();
            return tex3D;
        }

        private void GetNormalizedDataGpu(Texture3D result)
        {
            ComputeBuffer floatdataBuffer = new ComputeBuffer(this.dataArray.Length, 1 * sizeof(float));
            floatdataBuffer.SetData(this.dataArray);

            int kernelid = imgCalculator.FindKernel("Normalize");

            imgCalculator.SetBuffer(kernelid, "floatData", floatdataBuffer);

            imgCalculator.SetFloat("minValue", this.minValue);
            imgCalculator.SetFloat("range", this.maxValue - this.minValue);
            imgCalculator.SetInts("imgDims", this.dimX, this.dimY, this.dimZ);

            imgCalculator.SetTexture(kernelid, "OutputImage", this.rtNormData);
            imgCalculator.Dispatch(kernelid, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            Graphics.CopyTexture(this.rtNormData, result);

            floatdataBuffer.Release();
            floatdataBuffer = null;
        }

        private void GetCentralDiffGradientGpu(Texture3D result)
        {
            int kernelid = imgCalculator.FindKernel("CalcGradient");


            imgCalculator.SetFloat("minValue", this.minValue);
            imgCalculator.SetFloat("range", this.maxValue - this.minValue);
            imgCalculator.SetInts("imgDims", this.dimX, this.dimY, this.dimZ);

            imgCalculator.SetTexture(kernelid, "InputImage", this.rtNormData);
            imgCalculator.SetTexture(kernelid, "OutputImage", this.rt2);
            imgCalculator.Dispatch(kernelid, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            Graphics.CopyTexture(this.rt2, result);
        }

        private void GetGaussFilterGpu(RenderTexture srcData, RenderTexture firstTex, RenderTexture secondTex, Texture3D result)
        {
            Debug.Log("Running gauss with group x " + (this.dimX / 8 + 1) + ", group y " + (this.dimY / 8 + 1) + ", group z " + (this.dimZ / 8 + 1));
            int kernelidX = imgFilter.FindKernel("KernelXFilter");
            int kernelidY = imgFilter.FindKernel("KernelYFilter");
            int kernelidZ = imgFilter.FindKernel("KernelZFilter");

            imgFilter.SetInts("imgDims", this.dimX, this.dimY, this.dimZ);
            imgFilter.SetFloats("coefficients", 0.25f * 1, 0.25f * 2, 0.25f * 1, 0.0f);

            imgFilter.SetTexture(kernelidX, "InputImage", srcData); // first call will use this.rt2 from the gradient result, second will use normalized data
            imgFilter.SetTexture(kernelidX, "OutputImage", firstTex);
            imgFilter.Dispatch(kernelidX, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetTexture(kernelidY, "InputImage", firstTex);
            imgFilter.SetTexture(kernelidY, "OutputImage", secondTex);
            imgFilter.Dispatch(kernelidY, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);
            
            imgFilter.SetTexture(kernelidZ, "InputImage", secondTex);
            imgFilter.SetTexture(kernelidZ, "OutputImage", firstTex);
            imgFilter.Dispatch(kernelidZ, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            Graphics.CopyTexture(firstTex, result);
        }

        private void GetSobelFilterGpu(RenderTexture firstTex, RenderTexture secondTex, RenderTexture sobelSpecial, Texture3D result)
        {
            int kernelidX = imgFilter.FindKernel("KernelXFilter");
            int kernelidY = imgFilter.FindKernel("KernelYFilter");
            int kernelidZ = imgFilter.FindKernel("KernelZSobelFilter");

            imgFilter.SetInts("imgDims", this.dimX, this.dimY, this.dimZ);
            // first round
            imgFilter.SetInt("resultSobelInd", 0);
            imgFilter.SetFloats("coefficients", 1 / 2.0f * -1, 0, 1 / 2.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidX, "InputImage", this.rtNormData);
            imgFilter.SetTexture(kernelidX, "OutputImage", secondTex);
            imgFilter.Dispatch(kernelidX, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetFloats("coefficients", 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidY, "InputImage", secondTex);
            imgFilter.SetTexture(kernelidY, "OutputImage", firstTex);
            imgFilter.Dispatch(kernelidY, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetFloats("coefficients", 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidZ, "InputImage", firstTex);
            imgFilter.SetTexture(kernelidZ, "OutputImage", sobelSpecial);
            imgFilter.Dispatch(kernelidZ, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);


            // second round
            imgFilter.SetInt("resultSobelInd", 1);
            imgFilter.SetFloats("coefficients", 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1, 0.0f);
            imgFilter.SetTexture(kernelidX, "InputImage", this.rtNormData);
            imgFilter.SetTexture(kernelidX, "OutputImage", secondTex);
            imgFilter.Dispatch(kernelidX, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetFloats("coefficients", 1 / 2.0f * -1, 0, 1 / 2.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidY, "InputImage", secondTex);
            imgFilter.SetTexture(kernelidY, "OutputImage", firstTex);
            imgFilter.Dispatch(kernelidY, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetFloats("coefficients", 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidZ, "InputImage", firstTex);
            imgFilter.SetTexture(kernelidZ, "OutputImage", sobelSpecial);
            imgFilter.Dispatch(kernelidZ, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            // third round
            imgFilter.SetInt("resultSobelInd", 2);
            imgFilter.SetFloats("coefficients", 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1, 0.0f);
            imgFilter.SetTexture(kernelidX, "InputImage", this.rtNormData);
            imgFilter.SetTexture(kernelidX, "OutputImage", secondTex);
            imgFilter.Dispatch(kernelidX, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetFloats("coefficients", 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidY, "InputImage", secondTex);
            imgFilter.SetTexture(kernelidY, "OutputImage", firstTex);
            imgFilter.Dispatch(kernelidY, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            imgFilter.SetFloats("coefficients", 1 / 2.0f * -1, 0, 1 / 2.0f * 1, 0.0f);

            imgFilter.SetTexture(kernelidZ, "InputImage", firstTex);
            imgFilter.SetTexture(kernelidZ, "OutputImage", sobelSpecial);
            imgFilter.Dispatch(kernelidZ, this.dimX / 8 + 1, this.dimY / 8 + 1, this.dimZ / 8 + 1);

            Graphics.CopyTexture(sobelSpecial, result);
        }

        #endregion
    }
}

