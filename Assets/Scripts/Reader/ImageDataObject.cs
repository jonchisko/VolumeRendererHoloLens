using System;
using System.Threading.Tasks;
using itk.simple;
using UnityEngine;
using UnityEditor;
using System.IO;
using com.jon_skoberne.HelperServices;

namespace com.jon_skoberne.Reader 
{
    public class ImageDataObject : ScriptableObject
    {

        public delegate void ReadingEvent(ImageDataObject ido);
        public static event ReadingEvent OnReadingError;
        public static event ReadingEvent OnReadingSuccess;

        private float[] dataArray;
        public ItkReadFileSupport.ReadType readType;
        public ComputeShader csReading;
        public Texture3D tex3D;
        public Texture3D tex3Dgauss;
        public Texture3D tex3Dgradient;
        public Texture3D tex3DgradientGauss;
        public Texture3D tex3DgradientSobel;
        
        public float minValue, maxValue;
        public int dimX, dimY, dimZ;
        public string filePath;

        /*public ImageDataObject(ItkReadFileSupport.ReadType readType, ComputeShader csReading, string filePath)
        {
            this.readType = readType;
            this.csReading = csReading;
            InitializeValues(filePath);
        }*/

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
                case 0: return tex3D;
                case 1: return tex3Dgauss;
                default: throw new SystemException("Filter mode does not exist!");
            }
        }

        public Texture3D GetTexture3DGradient(int mode=0)
        {
            switch(mode)
            {
                case 0: return tex3Dgradient;
                case 1: return tex3DgradientGauss;
                case 2: return tex3DgradientSobel;
                default: throw new SystemException("Gradient mode does not exist!");
            }
        }

        #endregion

        #region Set methods
        
        public void SetComputeShader(ComputeShader cs)
        {
            this.csReading = cs;
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
                SetTextures();
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
                for (int i = 0; i < length; i++)
                {
                    imageFloatArray[i] = floatPointer[i];
                }
            }
            this.dataArray = imageFloatArray;
        }

        public void SetMaxMinValues()
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

        public void SetTextures()
        {
            Texture3D actualValues = ComputeTexture3D();
            Texture3D gradientValues = ComputeTexture3DGradient();
            AssetDatabase.CreateAsset(actualValues, "Assets/Resources/VolumeData/Texture3D/" + Path.GetFileNameWithoutExtension(GetFilePath()) + ".asset");
            AssetDatabase.CreateAsset(gradientValues, "Assets/Resources/VolumeData/Texture3DGrad/" + Path.GetFileNameWithoutExtension(GetFilePath()) + "_grad" + ".asset");
            tex3D = actualValues;
            tex3Dgradient = gradientValues;


            Texture3D t = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RFloat, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels(Filters.FilterGPUTest(actualValues.GetPixels(), GetWidth(), GetHeight(), GetDepth()));
            t.Apply();
            AssetDatabase.CreateAsset(t, "Assets/Resources/VolumeData/Texture3D/" + Path.GetFileNameWithoutExtension(GetFilePath()) + "_shader_test_" + ".asset");

            tex3Dgauss = GetFilteredTexture(actualValues.GetPixels(), TextureFormat.RFloat, 0);
            tex3DgradientGauss = GetFilteredTexture(gradientValues.GetPixels(), TextureFormat.RGBAFloat, 0);
            tex3DgradientSobel = GetFilteredTexture(actualValues.GetPixels(), TextureFormat.RGBAFloat, 1);

            AssetDatabase.CreateAsset(tex3Dgauss, "Assets/Resources/VolumeData/Texture3D/" + Path.GetFileNameWithoutExtension(GetFilePath()) + "_gauss" + ".asset");
            AssetDatabase.CreateAsset(tex3DgradientGauss, "Assets/Resources/VolumeData/Texture3DGrad/" + Path.GetFileNameWithoutExtension(GetFilePath()) + "_grad_gauss" + ".asset");
            AssetDatabase.CreateAsset(tex3DgradientSobel, "Assets/Resources/VolumeData/Texture3DGrad/" + Path.GetFileNameWithoutExtension(GetFilePath()) + "_grad_sobel" + ".asset");

        }

        public void SetDimensions(Image image)
        {
            this.dimX = (int)image.GetWidth();
            this.dimY = (int)image.GetHeight();
            this.dimZ = (int)image.GetDepth();
        }

        public RenderTexture GetRenderTexture(Image image) // TODO: convert everything to render texture and use compute shaders ??! ATM this is NOT used!
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
            /*
             * It's a terminology error, depth is not volumeDepth, so in 
             * RenderTexture constructor use two dimension and depth 0 (depth buffer, is not volumeDepth/slice)
             * url: https://answers.unity.com/questions/1611445/3d-rendertextures-with-depth-not-supported.html
             */
            RenderTexture tex3D = new RenderTexture((int)input.GetWidth(), (int)input.GetHeight(), 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = (int)input.GetDepth()
            };
            tex3D.Create();
            /* Access the underlying buffer in unsafe block - the compiler cannot 
             * perform full type checking and we can use pointers
             */
            unsafe 
            {
                float* bufferPtr = (float*)buffer.ToPointer();
                // linear indx
                Parallel.For(0, length, index => { imageFloatArray[index] = bufferPtr[index]; });
                int kernelHandle = csReading.FindKernel("CSWriteTexture");
                ComputeBuffer dataBuffer = new ComputeBuffer(length, 1 * sizeof(float));
                dataBuffer.SetData(imageFloatArray);
                csReading.SetBuffer(kernelHandle, "input", dataBuffer);
                csReading.SetTexture(kernelHandle, "outputTexture3D", tex3D);
                csReading.SetInt("_InputSize", length);
                csReading.SetVector("_Dimensions", new Vector4(size[0], size[1], size[2], 0));
                csReading.Dispatch(kernelHandle, Mathf.CeilToInt(length / 1024.0f), 1, 1); // 1024 num of threads on gpu in 1 block
                dataBuffer.Dispose();
            }
            return tex3D;
        }

        public Texture3D ComputeTexture3D()
        {
            Texture3D tex3D = new Texture3D(this.dimX, this.dimY, this.dimZ, TextureFormat.RFloat, false);
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

        public Texture3D ComputeTexture3DGradient()
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


        #endregion
    }
}

