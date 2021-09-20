using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


namespace com.jon_skoberne.TransferFunctionDrawer
{
    public class TransferFunctionSaver
    {

        public static LinkedList<string> ListAvailableTransferFunctions(string extension)
        {
            LinkedList<string> tfFiles = new LinkedList<string>();
            if (Directory.Exists(Application.persistentDataPath))
            {
                string worldsFolder = Application.persistentDataPath;

                DirectoryInfo d = new DirectoryInfo(worldsFolder);
                foreach (var file in d.GetFiles("*" + extension))
                {
                    tfFiles.AddLast(file.FullName);
                    Debug.Log(file);
                }
            }
            else
            {
                //File.Create(Application.persistentDataPath);
            }

            return tfFiles;
        }


        public static void SavePoints(LinkedList<TransferFunctionPoint> points, string fileName)
        {
            LinkedList<TransferFunctionSaveObject> pointsSave = new LinkedList<TransferFunctionSaveObject>();
            foreach (var p in points)
            {
                pointsSave.AddLast(p.GetSaveObject());
            }

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(Application.persistentDataPath + "\\" + fileName);
            bf.Serialize(file, pointsSave);
            file.Close();
        }

        public static LinkedList<TransferFunctionSaveObject> LoadPoints(string fileName)
        {
            string path = fileName;
            LinkedList<TransferFunctionSaveObject> tfSaveObject = null;
            if (File.Exists(path))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(path, FileMode.Open);
                tfSaveObject = (LinkedList<TransferFunctionSaveObject>)bf.Deserialize(file);
                file.Close();
            }
            else
            {
                Debug.LogError("File does not exist!");
            }

            return tfSaveObject;
        }


    }
}

