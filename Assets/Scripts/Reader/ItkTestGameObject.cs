using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.jon_skoberne.Reader
{
    public class ItkTestGameObject : MonoBehaviour
    {
        public ComputeShader cs;
        public Transform volumeBox;
        private string imageFilePath = "C:\\Users\\jonch\\Downloads\\Pat1\\Pat1_CT.nrrd";
        private ImageDataObject ido;

        // Start is called before the first frame update
        void Start()
        {
            //ido = new ImageDataObject(ItkReadFileSupport.ReadType.NRRD, cs, imageFilePath);
            ido = (ImageDataObject) ScriptableObject.CreateInstance("ImageDataObject");
            ido.CreateImageDataObject(ItkReadFileSupport.ReadType.NRRD, imageFilePath);
            ido.GetTexture3D();
            ido.GetTexture3DGradient();
            AssetDatabase.CreateAsset(ido, "Assets/VolumeData/" + "test" + ".asset"); // save

            Texture3D dataTex = ido.GetTexture3D();
            AssetDatabase.CreateAsset(dataTex, "Assets/VolumeData/" + "test_3d_tex" + ".asset");
            Renderer renderer = volumeBox.GetComponent<Renderer>();
            renderer.sharedMaterial.SetTexture("_CompTopTex", dataTex);

        }

        // Update is called once per frame
        void Update()
        {
            EditorUtility.OpenFilePanel("Overwrite with png", "", "png");
        }
    }
}

