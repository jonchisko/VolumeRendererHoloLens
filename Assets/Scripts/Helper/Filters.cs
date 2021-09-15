using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace com.jon_skoberne.HelperServices
{
    public class Filters
    {
        // 3D

        // gauss filter
        static private readonly float[,,] gaussFilterX = { { { 0.25f * 1 } }, { { 0.25f * 2 } }, { { 0.25f * 1 } } };
        static private readonly float[,,] gaussFilterY = { { { 0.25f * 1 }, { 0.25f * 2 }, { 0.25f * 1 } } };
        static private readonly float[,,] gaussFilterZ = { { { 0.25f * 1, 0.25f * 2, 0.25f * 1 } } };

        // sobel filter
        // x direction
        static private readonly float[,,] sobelXFilterX = { { { 1 / 2.0f * -1 } }, { { 0 } }, { { 1 / 2.0f * 1 } } };
        static private readonly float[,,] sobelXFilterY = { { { 1 / 4.0f * 1 }, { 1 / 4.0f * 2 }, { 1 / 4.0f * 1 } } };
        static private readonly float[,,] sobelXFilterZ = { { { 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1 } } };
        // y direction
        static private readonly float[,,] sobelYFilterX = { { { 1 / 4.0f * 1 } }, { { 1 / 4.0f * 2 } }, { { 1 / 4.0f * 1 } } };
        static private readonly float[,,] sobelYFilterY = { { { 1 / 2.0f * -1 }, { 0 }, { 1 / 2.0f * 1 } } };
        static private readonly float[,,] sobelYFilterZ = { { { 1 / 4.0f * 1, 1 / 4.0f * 2, 1 / 4.0f * 1 } } };
        // z direction
        static private readonly float[,,] sobelZFilterX = { { { 1 / 4.0f * 1 } }, { { 1 / 4.0f * 2 } }, { { 1 / 4.0f * 1 } } };
        static private readonly float[,,] sobelZFilterY = { { { 1 / 4.0f * 1 }, { 1 / 4.0f * 2 }, { 1 / 4.0f * 1 } } };
        static private readonly float[,,] sobelZFilterZ = { { { 1 / 2.0f * -1, 0, 1 / 2.0f * 1 } } };



        static public Color[] FilterWithGauss(Color[] floatData, int width, int height, int depth)
        {
            Color[] result = new Color[width * height * depth];
            result = ComputeConv(floatData, width, height, depth, gaussFilterX);
            result = ComputeConv(result, width, height, depth, gaussFilterY);
            result = ComputeConv(result, width, height, depth, gaussFilterZ);
            return result;
        }

        static public Color[] FilterWithSobel(Color[] floatData, int width, int height, int depth)
        {
            Color[] xDerResult = new Color[width * height * depth];
            Color[] yDerResult = new Color[width * height * depth];
            Color[] zDerResult = new Color[width * height * depth];

            Color[] sobelGrad = new Color[width * height * depth];
            xDerResult = ComputeConv(floatData, width, height, depth, sobelXFilterX);
            xDerResult = ComputeConv(xDerResult, width, height, depth, sobelXFilterY);
            xDerResult = ComputeConv(xDerResult, width, height, depth, sobelXFilterZ);

            yDerResult = ComputeConv(floatData, width, height, depth, sobelYFilterX);
            yDerResult = ComputeConv(yDerResult, width, height, depth, sobelYFilterY);
            yDerResult = ComputeConv(yDerResult, width, height, depth, sobelYFilterZ);

            zDerResult = ComputeConv(floatData, width, height, depth, sobelZFilterX);
            zDerResult = ComputeConv(zDerResult, width, height, depth, sobelZFilterY);
            zDerResult = ComputeConv(zDerResult, width, height, depth, sobelZFilterZ);

            for (int i = 0; i < sobelGrad.Length; i++)
            {
                sobelGrad[i] = new Color(xDerResult[i].r, yDerResult[i].r, zDerResult[i].r, floatData[i].r);
            }

            return sobelGrad;
        }

        static public Color[] FilterGPUTest(Color[] floatData, int width, int height, int depth)
        {
            Color[] result = new Color[width * height * depth];
            result = ComputeConvGPU(floatData, width, height, depth, gaussFilterX);
            return result;
        }

        static private Color[] ComputeConv(Color[] floatData, int width, int height, int depth, float[,,] filter3D)
        {
            Color[] tmpResult = new Color[width * height * depth];

            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {

                        int offsetX = filter3D.GetLength(0) / 2;
                        int offsetY = filter3D.GetLength(1) / 2;
                        int offsetZ = filter3D.GetLength(2) / 2;

                        int startIndX = x - offsetX;
                        int startIndY = y - offsetY;
                        int startIndZ = z - offsetZ;

                        int endIndX = x + offsetX;
                        int endIndY = y + offsetY;
                        int endIndZ = z + offsetZ;

                        Color pixelResult = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        for (int zFilter = startIndZ; zFilter <= endIndZ; zFilter++)
                        {
                            for (int yFilter = startIndY; yFilter <= endIndY; yFilter++)
                            {
                                for (int xFilter = startIndX; xFilter <= endIndX; xFilter++)
                                {
                                    if (zFilter < 0 || yFilter < 0 || xFilter < 0 || zFilter >= depth || yFilter >= height || xFilter >= width) continue;
                                    int globalInd = xFilter + yFilter * width + (width * height) * zFilter;
                                    pixelResult += floatData[globalInd] * filter3D[(filter3D.GetLength(0) - 1) - (xFilter - startIndX), (filter3D.GetLength(1) - 1) - (yFilter - startIndY), (filter3D.GetLength(2) - 1) - (zFilter - startIndZ)];
                                }
                            }
                        }
                        tmpResult[x + y * width + (width * height) * z] = pixelResult;
                    }
                }
            }
            return tmpResult;
        }

        static private Color[] ComputeConvGPU(Color[] floatData, int width, int height, int depth, float[,,] filter3D)
        {

            ComputeShader computeShader = (ComputeShader)Resources.Load("FilterShader");
            if (computeShader == null)
            {
                Debug.LogError("Filter Shader is null");
                throw new System.Exception("FilterShader in Filter.cs is null!");
            }

            Color[] tmpResult = new Color[width * height * depth];

            ComputeBuffer inputColorData = new ComputeBuffer(width * height * depth, 4 * sizeof(float));
            inputColorData.SetData(floatData);
            ComputeBuffer outputColorData = new ComputeBuffer(width * height * depth, 4 * sizeof(float));
            outputColorData.SetData(floatData);
            
            computeShader.SetBuffer(0, "input", inputColorData);
            computeShader.SetBuffer(0, "output", outputColorData);
            computeShader.SetFloat("width", width);
            computeShader.SetFloat("height", height);
            computeShader.SetFloat("depth", depth);

            computeShader.Dispatch(0, width / 1024, height / 1, depth / 1);

            outputColorData.GetData(tmpResult);
            outputColorData.Dispose();
            inputColorData.Dispose();

            return tmpResult;
        }

    }
}

