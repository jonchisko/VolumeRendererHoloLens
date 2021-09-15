using UnityEngine;


namespace com.jon_skoberne.HelperServices
{
    public class PerlinNoise
    {
        // public
        static public Texture2D GetTexture(int width, int height, float scale=1.0f, float originX=0, float originY=0)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[tex.width * tex.height];
            
            ComputePixels(pixels, tex.width, tex.height, scale, originX, originY);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // private

        static private void ComputePixels(Color[] pixels, int width, int height, float scale, float originX, float originY)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xPos = originX + (float)x / width * scale;
                    float yPos = originY + (float)y / height * scale;
                    float value = Mathf.PerlinNoise(xPos, yPos);
                    pixels[y * width + x] = new Color(value, value, value);
                }
            }
        }
    }
}


