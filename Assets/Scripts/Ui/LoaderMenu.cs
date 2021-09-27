using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using com.jon_skoberne.Reader;
using System.IO;
using System;

namespace com.jon_skoberne.UI
{
    public class LoaderMenu : MonoBehaviour
    {
        public TMP_InputField input;
        public TMP_Dropdown dropDown;
        public MainMenuPopup popup;

        private LinkedList<string> loadOptions = new LinkedList<string>();
        private const string FileConversionFailure = "Something went wrong when converting file to texture!";
        private const string FilePathFailure = "Something is wrong with file path!";
        private const string FileConversionSuccess = "File conversion was successful!";

        // Start is called before the first frame update
        void Start()
        {
            ImageDataObject.OnReadingError += OnReadingErrorInConversion;
            ImageDataObject.OnReadingSuccess += OnReadingSuccessInConversion;
        }

        private void OnDestroy()
        {
            ImageDataObject.OnReadingError -= OnReadingErrorInConversion;
            ImageDataObject.OnReadingSuccess -= OnReadingSuccessInConversion;
        }

        public void LoadData()
        {
            // load (convert) data, create image data object
            // serialize image data object (store it)
            // create assets
            ConvertSelectedFileToImageDataObject();
        }

        public void OnSelectedItemDropdown(string path)
        {
            // deserialize data into image data object
            // create assets
        }

        private void OnReadingErrorInConversion(ImageDataObject ido)
        {
            popup.OpenPopup("ERROR", FileConversionFailure + "\nObject:\n" + ido.GetFilePath());
        }

        private void OnReadingSuccessInConversion(ImageDataObject ido)
        {
            popup.OpenPopup("SUCCESS", FileConversionSuccess + "\nObject:\n" + ido.GetFilePath());
            //AssetDatabase.CreateAsset(ido, SaveFolderPath + "/" + Path.GetFileNameWithoutExtension(ido.GetFilePath()) + "_scriptableObject" + ".asset");
            //PopulateDropdownDinamically();
        }

        private void ConvertSelectedFileToImageDataObject()
        {
            string imagePath = input.text;
            if (File.Exists(imagePath))
            {
                try
                {
                    ImageDataObject ido = (ImageDataObject)ScriptableObject.CreateInstance(VolumeAssetNames.imageObjectTypeName);
                    string fileType = Path.GetExtension(imagePath).Trim('.');
                    Debug.Log(imagePath);
                    Debug.Log(fileType);
                    ItkReadFileSupport.ReadType readType = ItkReadFileSupport.GetReadTypeFromString(fileType);
                    ido.CreateImageDataObject(readType, imagePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                    popup.OpenPopup("Error", ex.Message);
                }
            }
            else
            {
                popup.OpenPopup("Error", FilePathFailure);
            }
        }


        /*private void PopulateDropdownDinamically()
        {
            dropDown.ClearOptions();
            dropDownImageDataObjectsAssetPaths.Clear();
            string[] guids = AssetDatabase.FindAssets("_scriptableObject", new string[] { SaveFolderPath });
            List<string> assetPaths = new List<string>();
            foreach (string guid in guids)
            {
                assetPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (var element in assetPaths)
            {
                dropDown.options.Add(new TMP_Dropdown.OptionData() { text = Path.GetFileName(element) });
                dropDownImageDataObjectsAssetPaths.Add(SaveFolderPath.Split('/')[2] + "/" + Path.GetFileNameWithoutExtension(element));
            }
            dropDown.value = -1;
            dropDown.Select();
            dropDown.RefreshShownValue();
        }

        private LinkedList<string> GetDataFiles()
        {

        }

        private void CreateAssets(ImageDataObject ido)
        {

        }

        private void SerializeImageDataObject(ImageDataObject ido)
        {

        }

        private ImageDataObject DeserializeImageDataObject(string path)
        {

        }*/
    }
}

