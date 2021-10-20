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

        //private List<string> loadOptions = new List<string>();
        private const string FileConversionFailure = "Something went wrong when converting file to texture!";
        private const string FilePathFailure = "Something is wrong with file path!";
        private const string FileConversionSuccess = "File conversion was successful!";
        private const string LoadingMsg = "In progress ...";

        private const string noneOption = "None";

        // Start is called before the first frame update
        void Start()
        {
            PopulateDropdownDinamically();


            RegisterEvents();
        }

        private void OnDestroy()
        {
            DeregisterEvents();
        }

        public bool IsImageLoaded()
        {
            return ido.GetTexture3D() != null;
        }

        public void ShowNotLoadedPopup()
        {
            popup.OpenPopup("Image not loaded!", "Load image to continue.");
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
                Debug.Log("Selected index: " + selectedInd);
                string element = this.dropDown.options[selectedInd].text;
                if(element != noneOption)
                {
                    //string path = this.loadOptions[selectedInd];
                    string path = Application.persistentDataPath + "\\" + element;
                    SaveImageDataObject imageObject = LoadImageObject(path);
                    ido.DeserializeIntoImageDataObject(imageObject);
                }
            }

        }

        private void RegisterEvents()
        {
            ImageDataObject.OnReadingError += OnReadingErrorInConversion;
            ImageDataObject.OnReadingSuccess += OnReadingSuccessInConversion;
            dropDown.onValueChanged.AddListener(OnSelectedItemDropdown);
        }

        private void DeregisterEvents()
        {
            ImageDataObject.OnReadingError -= OnReadingErrorInConversion;
            ImageDataObject.OnReadingSuccess -= OnReadingSuccessInConversion;
            dropDown.onValueChanged.RemoveListener(OnSelectedItemDropdown);
        }

        private void OnReadingErrorInConversion(ImageDataObject ido)
        {
            popup.OpenPopup("ERROR", FileConversionFailure + "\nObject:\n" + ido.GetFilePath());
        }

        private void OnReadingSuccessInConversion(ImageDataObject ido)
        {
            popup.OpenPopup("SUCCESS", FileConversionSuccess + "\nObject:\n" + ido.GetFilePath());
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
                    PopulateDropdownDinamically();
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
            Debug.Log("Populating dropdown.");
            dropDown.ClearOptions();
            //loadOptions.Clear();
            List<string> loadOptions = new List<string>();

            loadOptions = GetDataFiles();
            loadOptions.Add(noneOption);
            dropDown.options.Add(new TMP_Dropdown.OptionData() { text = noneOption });
            foreach (var element in loadOptions)
            {
                if(element != noneOption)
                {
                    dropDown.options.Add(new TMP_Dropdown.OptionData() { text = Path.GetFileName(element) });
                }
            }
            dropDown.value = 0;
            dropDown.Select();
            dropDown.RefreshShownValue();
        }

        private List<string> GetDataFiles()
        {
            List<string> files = new List<string>();
            if (Directory.Exists(Application.persistentDataPath))
            {
                Debug.Log("Get data files. Path: " + Application.persistentDataPath);
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
            Debug.Log("Loading image: " + path);
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
            FileStream file = File.Create(Application.persistentDataPath + "\\" + Path.GetFileNameWithoutExtension(saveObject.filePath) + VolumeAssetNames.savedImageObject);
            bf.Serialize(file, saveObject);
            file.Close();

            ido.ResetSerializableImageObject(); // "releases mem" for GC
        }
    }
}

