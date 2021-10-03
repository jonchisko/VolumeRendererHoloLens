using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using com.jon_skoberne.Reader;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace com.jon_skoberne.UI
{
    public class LoaderMenu : MonoBehaviour
    {
        public TMP_InputField input;
        public TMP_Dropdown dropDown;
        public MainMenuPopup popup;
        public ImageDataObject ido;

        private List<string> loadOptions = new List<string>();
        private const string FileConversionFailure = "Something went wrong when converting file to texture!";
        private const string FilePathFailure = "Something is wrong with file path!";
        private const string FileConversionSuccess = "File conversion was successful!";
        private const string LoadingMsg = "In progress ...";

        // Start is called before the first frame update
        void Start()
        {
            PopulateDropdownDinamically();


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

        public void OnSelectedItemDropdown(int selectedInd)
        {
            if(selectedInd >= 0)
            {
                string path = this.loadOptions[selectedInd];
                SaveImageDataObject imageObject = LoadImageObject(path);
                ido.DeserializeIntoImageDataObject(imageObject);
            }

        }

        private void OnReadingErrorInConversion(ImageDataObject ido)
        {
            popup.OpenPopup("ERROR", FileConversionFailure + "\nObject:\n" + ido.GetFilePath());
        }

        private void OnReadingSuccessInConversion(ImageDataObject ido)
        {
            popup.OpenPopup("SUCCESS", FileConversionSuccess + "\nObject:\n" + ido.GetFilePath());
            PopulateDropdownDinamically();
        }

        private void ConvertSelectedFileToImageDataObject()
        {
            string imagePath = input.text;
            if (File.Exists(imagePath))
            {
                try
                {
                    popup.OpenPopup("Working", LoadingMsg);
                    string fileType = Path.GetExtension(imagePath).Trim('.');
                    Debug.Log(imagePath);
                    Debug.Log(fileType);
                    ItkReadFileSupport.ReadType readType = ItkReadFileSupport.GetReadTypeFromString(fileType);
                    ido.CreateImageDataObject(readType, imagePath);
                    SaveImageDataObject();
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


        private void PopulateDropdownDinamically()
        {
            dropDown.ClearOptions();
            loadOptions.Clear();

            loadOptions = GetDataFiles();

            foreach (var element in loadOptions)
            {
                dropDown.options.Add(new TMP_Dropdown.OptionData() { text = Path.GetFileName(element) });
            }
            dropDown.value = -1;
            dropDown.Select();
            dropDown.RefreshShownValue();
        }

        private List<string> GetDataFiles()
        {
            List<string> files = new List<string>();
            if (Directory.Exists(Application.persistentDataPath))
            {
                string worldsFolder = Application.persistentDataPath;

                DirectoryInfo d = new DirectoryInfo(worldsFolder);
                foreach (var file in d.GetFiles("*" + VolumeAssetNames.savedImageObject))
                {
                    files.Add(file.FullName);
                    Debug.Log(file);
                }
            } else
            {
                //File.Create(Application.persistentDataPath);
            }

            return files;
        }

        private static SaveImageDataObject LoadImageObject(string path)
        {
            SaveImageDataObject imageObject = null;
            if (File.Exists(path))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(path, FileMode.Open);
                imageObject = (SaveImageDataObject)bf.Deserialize(file);
                file.Close();
            }
            else
            {
                Debug.LogError("File does not exist!");
            }

            return imageObject;
        }

        private void SaveImageDataObject()
        {
            Debug.Log("Saving Converted Image Data Object");
            SaveImageDataObject saveObject = ido.GetSerializableImageObject();
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(Application.persistentDataPath + "\\" + Path.GetFileNameWithoutExtension(saveObject.filePath));
            bf.Serialize(file, saveObject);
            file.Close();
        }

        private void CreateImageDataInstance()
        {
            // create image data instance
            // create asssets in folder so that they can be "serialized" into image
        }

        private void CreateAssets(ImageDataObject ido)
        {

        }

        private void SerializeImageDataObject(ImageDataObject ido)
        {

        }

        /*private ImageDataObject DeserializeImageDataObject(string path)
        {

        }

        
         *             string[] guids = AssetDatabase.FindAssets("_scriptableObject", new string[] { VolumeAssetNames.SaveFolderPath });
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
        */
    }
}

