
namespace com.jon_skoberne.Reader
{
    public class ItkReadFileSupport
    {

        public enum ReadType
        {
            MINC,
            NIFTI,
            NRRD,
        }

        public static string GetIoName(ReadType readType)
        {
            string result;
            switch (readType)
            {
                case ReadType.MINC: result = "MINCImageIO"; break;
                case ReadType.NIFTI: result = "NiftiImageIO"; break;
                case ReadType.NRRD: result = "NrrdImageIO"; break;
                default: throw new System.Exception("This read type does not exist");
            }
            return result;
        }

        public static ReadType GetReadTypeFromString(string stringType)
        {
            ReadType result;
            switch(stringType)
            {
                case "nrrd": result = ReadType.NRRD; break;
                case "minc": result = ReadType.MINC; break;
                case "nifti": result = ReadType.NIFTI; break;
                default: throw new System.Exception("This read type does not exist");
            }
            return result;
        }
    }
}

