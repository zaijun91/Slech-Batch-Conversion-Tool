namespace MhtToPdfConverter.Models
{
    public class FileError
    {
        public string FilePath { get; }
        public string ErrorMessage { get; }

        public FileError(string filePath, string errorMessage)
        {
            FilePath = filePath;
            // Simplify long error messages if needed
            ErrorMessage = errorMessage.Length > 200 ? errorMessage.Substring(0, 200) + "..." : errorMessage;
        }
    }
}
