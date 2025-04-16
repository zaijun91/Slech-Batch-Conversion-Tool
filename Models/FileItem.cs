using MhtToPdfConverter.ViewModels; // For ViewModelBase if needed later, or just INotifyPropertyChanged
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO; // For Path operations
using System; // For DateTime


namespace MhtToPdfConverter.Models
{
    public class FileItem : INotifyPropertyChanged // Inherit for Status updates
    {
        // Keep original path read-only after construction
        public string SourcePath { get; }

        // Read-only property to display just the file name
        public string SourceFileName => Path.GetFileName(SourcePath);

        private string _outputFileName = string.Empty;
        public string OutputFileName
        {
            get => _outputFileName;
            // Ensure validation happens if set directly, though usually set via Save command
            set => SetProperty(ref _outputFileName, value);
        }

        private string _status = "待处理"; // Default status
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private string? _errorMessage; // Nullable string for error message
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }


        public FileItem(string sourcePath)
        {
            SourcePath = sourcePath;
            // Initialize OutputFileName with a default generated name
            _outputFileName = GenerateDefaultName(sourcePath); // Assign directly to backing field
        }

        // Public method to allow regeneration if needed (e.g., after validation fails)
        public string GenerateDefaultName(string path)
        {
            // Generates name like "OriginalFileName_YYYYMMDD"
            return $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.Now:yyyyMMdd}";
        }


        // --- INotifyPropertyChanged Implementation (remains the same) ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        // --- End INotifyPropertyChanged ---
    }
}
