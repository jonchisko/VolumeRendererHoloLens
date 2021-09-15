using itk.simple;
using UnityEngine;

// obsolete
namespace com.jon_skoberne.Reader
{
    public class ItkWrapper
    {
        /*
         * This class is used as a wrapper to itk.
         * It's primary function is opening and reading medical image files and providing the data
         * to the callers, which is needed as rendering information.
         */

        // TODO: if no reader will need special treatment all reading can be refactored into on readFile method
        public static void ReadFile(ItkReadFileSupport.ReadType readType, string filePathSource, ImageDataObject objectToWrite)
        {
            switch(readType)
            {
                case ItkReadFileSupport.ReadType.MINC:
                    ReadFile(filePathSource, objectToWrite);
                    break;
                case ItkReadFileSupport.ReadType.NIFTI:
                    break;
                case ItkReadFileSupport.ReadType.NRRD:
                    ReadFile(filePathSource, objectToWrite);
                    break;
                default:
                    throw new System.Exception("Given readType is not a valid readType.");
            }
        }

        private static void ReadFile(string filePathSource, ImageDataObject objectToWrite)
        {
            try
            {
                ImageFileReader reader = new ImageFileReader();
                reader.SetImageIO(ItkReadFileSupport.GetIoName(objectToWrite.GetReadType()));
                reader.SetFileName(filePathSource);
                Image image = reader.Execute();
                Debug.Log("Image dimensions: " + image.GetDimension());
                Debug.Log("Image height: " + image.GetHeight() + ", width: " + image.GetWidth());
                //objectToWrite.SetImageData(image);
            } 
            catch (System.Exception ex)
            {
                Debug.LogError("Exception occured: " + ex.Message);
            }
        }


    }
}

