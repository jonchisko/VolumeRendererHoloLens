
namespace com.jon_skoberne.Reader
{
    public class ItkReadFileSupport
    {

        public enum ReadType
        {
            MINC,
            NIFTI,
            NRRD,
            MRC,
        }

        public static string GetIoName(ReadType readType)
        {
            string result;
            switch (readType)
            {
                case ReadType.MINC: result = "MINCImageIO"; break;
                case ReadType.NIFTI: result = "NiftiImageIO"; break;
                case ReadType.NRRD: result = "NrrdImageIO"; break;
                case ReadType.MRC: result = "MRCImageIO"; break;
                default: throw new System.Exception("This read type does not exist");
            }
            return result;
        }

        public static ReadType GetReadTypeFromString(string stringType)
        {
            ReadType result;
            switch(stringType)
            {
                case "nhdr":
                case "nrrd": result = ReadType.NRRD; break;
                case ".MNC":
                case "minc": result = ReadType.MINC; break;
                case "nia":
                case "nii":
                case "hdr":
                case "img":
                case "nifti": result = ReadType.NIFTI; break;
                case "rec":
                case "mrc": result = ReadType.MRC; break;
                default: throw new System.Exception("This read type does not exist");
            }
            return result;
        }
    }
}

