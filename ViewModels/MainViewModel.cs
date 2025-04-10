using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MhtToPdfConverter.Models; // Add this
using System.Collections.ObjectModel; // Add this
using System.ComponentModel; // For INotifyPropertyChanged if not using ViewModelBase directly
using System.Runtime.CompilerServices; // For CallerMemberName
using System.Windows.Input; // For ICommand
using Microsoft.Win32; // For OpenFileDialog
using System.IO; // For Path, Directory related operations
using System.Windows; // For MessageBox
using MhtToPdfConverter.Services; // Add this

namespace MhtToPdfConverter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // --- Properties ---

        private ObservableCollection<FileItem> _fileList = new();
        public ObservableCollection<FileItem> FileList
        {
            get => _fileList;
            set => SetProperty(ref _fileList, value);
        }

        private ObservableCollection<FileError> _errorList = new();
        public ObservableCollection<FileError> ErrorList
        {
            get => _errorList;
            set
            {
                // Need to handle setting the collection properly if needed,
                // but usually we just modify the existing one.
                // For simplicity, assume we only modify.
                if (_errorList != value) // Basic check
                {
                     _errorList = value;
                     OnPropertyChanged(); // Notify that the collection instance changed
                     UpdateErrorListHeader();
                     // Re-attach listener if the instance changes
                     if (_errorList != null)
                     {
                         _errorList.CollectionChanged += (s, e) => UpdateErrorListHeader();
                     }
                }
            }
        }

        private string _outputPath = string.Empty;
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string _currentStatus = "就绪";
        public string CurrentStatus
        {
            get => _currentStatus;
            set => SetProperty(ref _currentStatus, value);
        }

        private bool _overwrite;
        public bool Overwrite
        {
            get => _overwrite;
            set => SetProperty(ref _overwrite, value);
        }

        private string _errorListHeader = string.Empty;
        public string ErrorListHeader
        {
            get => _errorListHeader;
            private set => SetProperty(ref _errorListHeader, value); // Private setter for internal updates
        }

        private bool _isConverting;
        public bool IsConverting
        {
            get => _isConverting;
            private set
            {
                 if (SetProperty(ref _isConverting, value))
                 {
                     // Ensure commands re-evaluate their CanExecute status when IsConverting changes
                     Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
                 }
            }
        }


        // --- Constructor ---
        public MainViewModel()
        {
            // Initialize properties and commands
            // FileList and ErrorList are initialized inline

            // Set default output path (e.g., Desktop)
            try
            {
                 OutputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MHT_PDF_Output");
            }
            catch (Exception ex) // Handle potential errors getting Desktop path
            {
                 OutputPath = System.IO.Path.GetTempPath(); // Fallback to temp path
                 System.Diagnostics.Debug.WriteLine($"Error getting Desktop path: {ex.Message}");
            }

            Overwrite = false; // Default to not overwrite
            ErrorList.CollectionChanged += (s, e) => UpdateErrorListHeader(); // Listen for changes
            UpdateErrorListHeader(); // Initial update
            // Initialize Commands
            AddFilesCommand = new RelayCommand(AddFiles);
            AddFolderCommand = new RelayCommand(AddFolder);
            SelectOutputCommand = new RelayCommand(SelectOutput);
            ConvertCommand = new RelayCommand(Convert, CanConvert); // Add CanExecute logic
            ClearListCommand = new RelayCommand(ClearList, CanClearList); // Add CanExecute logic
            RetryCommand = new RelayCommand(RetryFile);
            EditFileNameCommand = new RelayCommand(EditFileName); // Initialize Edit command
            SaveFileNameCommand = new RelayCommand(SaveFileName); // Initialize Save command
        }


        // --- Helper Methods ---
        private void UpdateErrorListHeader()
        {
            ErrorListHeader = $"错误列表 ({ErrorList.Count})";
        }

        // --- Commands ---
        public ICommand AddFilesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand SelectOutputCommand { get; }
        public ICommand ConvertCommand { get; }
        public ICommand ClearListCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand EditFileNameCommand { get; private set; } // Added
        public ICommand SaveFileNameCommand { get; private set; } // Added


        // --- Command Methods (Implementations) ---

        private void AddFiles(object? parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MHT Files (*.mht;*.mhtml)|*.mht;*.mhtml|All files (*.*)|*.*",
                Multiselect = true,
                Title = "选择 MHT/MHTML 文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var sourcePath in openFileDialog.FileNames) // Renamed variable
                {
                    // Avoid adding duplicates using SourcePath
                    if (!FileList.Any(f => f.SourcePath.Equals(sourcePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        FileList.Add(new FileItem(sourcePath)); // Pass sourcePath to constructor
                    }
                }
            }
        }

        private void AddFolder(object? parameter)
        {
             // Note: Using Ookii.Dialogs.Wpf for a better folder browser dialog is recommended,
             // but requires adding a NuGet package. Using basic OpenFileDialog for now as a workaround,
             // asking user to select a file within the target folder. A bit clunky.
             // Or we can try the WindowsAPICodePack-Shell package if available.
             // Let's stick to a simpler approach first or ask user.
             // For now, let's use a placeholder message.
             MessageBox.Show("选择文件夹功能需要额外的库支持，暂未实现。请使用“添加文件”按钮。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

             // TODO: Implement proper folder selection later (e.g., using Ookii.Dialogs.Wpf or WindowsAPICodePack-Shell)
             /* Example using OpenFileDialog workaround:
             var dialog = new OpenFileDialog { ValidateNames = false, CheckFileExists = false, CheckPathExists = true, FileName = "选择文件夹" };
             if (dialog.ShowDialog() == true)
             {
                 string folderPath = Path.GetDirectoryName(dialog.FileName);
                 if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                 {
                     var mhtFiles = Directory.EnumerateFiles(folderPath, "*.mht", SearchOption.TopDirectoryOnly)
                                             .Concat(Directory.EnumerateFiles(folderPath, "*.mhtml", SearchOption.TopDirectoryOnly));
                     foreach (var filePath in mhtFiles)
                     {
                         if (!FileList.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                         {
                             FileList.Add(new FileItem(filePath));
                         }
                     }
                 }
             }
             */
        }

        private void SelectOutput(object? parameter)
        {
            // Similar issue as AddFolder. Using OpenFileDialog workaround.
            var dialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择输出文件夹" // Will show in the file name box
            };

            if (dialog.ShowDialog() == true)
            {
                string? selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                {
                    OutputPath = selectedPath;
                }
                else
                {
                     // If user selected a drive root or something invalid, fallback or keep old path
                     MessageBox.Show("选择的路径无效，请选择一个有效的文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool CanConvert(object? parameter)
        {
            // Only allow conversion if not already converting, files exist, and output path is set
            return !IsConverting && FileList.Any() && !string.IsNullOrWhiteSpace(OutputPath);
        }

        private async void Convert(object? parameter)
        {
            if (!CanConvert(null)) return; // Check should include IsConverting now

            IsConverting = true; // Disable buttons

            // Basic validation
            if (!Directory.Exists(OutputPath))
            {
                 MessageBox.Show($"输出路径不存在: {OutputPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                 return;
            }

            CurrentStatus = "开始转换...";
            Progress = 0;
            ErrorList.Clear(); // Clear previous errors

            int totalFiles = FileList.Count;
            int processedCount = 0;
            int successCount = 0;

            // Run the conversion loop on a background thread
            await Task.Run(async () =>
            {
                foreach (var fileItem in FileList.ToList()) // Use ToList() for safe iteration if list might change (e.g., retry adds)
                {
                    if (!IsConverting) break; // Allow cancellation (basic check)

                    // Use OutputFileName from the item, ensuring it has .pdf extension
                    string finalOutputFileName = fileItem.OutputFileName;
                    if (!finalOutputFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        finalOutputFileName += ".pdf";
                    }
                    // Validate again before combining, in case it was set directly
                    var invalidChars = Path.GetInvalidFileNameChars();
                    if (finalOutputFileName.IndexOfAny(invalidChars) >= 0)
                    {
                         // Handle invalid name during conversion - add error and skip
                         await Application.Current.Dispatcher.InvokeAsync(() =>
                         {
                             fileItem.Status = "错误";
                             ErrorList.Add(new FileError(fileItem.SourcePath, $"输出文件名 '{fileItem.OutputFileName}' 包含非法字符。"));
                         });
                         continue; // Skip this file
                    }

                    string pdfOutputPath = Path.Combine(OutputPath, finalOutputFileName);


                    try
                    {
                        // Update UI from background thread using Dispatcher
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            fileItem.Status = "转换中...";
                            CurrentStatus = $"正在转换: {Path.GetFileName(fileItem.SourcePath)}"; // Display original source file name
                        });

                        // Call the actual conversion service using SourcePath
                        bool success = await PdfConversionService.ConvertSingleFileAsync(fileItem.SourcePath, pdfOutputPath, Overwrite);

                        // Update UI based on result
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (success)
                            {
                                fileItem.Status = "成功";
                                successCount++;
                            }
                            else
                            {
                                fileItem.Status = "失败";
                                // Add error (assuming ConvertSingleFileAsync throws for specific errors, or returns false for general print failure)
                                ErrorList.Add(new FileError(fileItem.SourcePath, "转换失败 (PrintToPdfAsync 返回 false)"));
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // Update UI with error
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            fileItem.Status = "错误";
                            ErrorList.Add(new FileError(fileItem.SourcePath, ex.Message)); // Use SourcePath for error reporting
                        });
                        // Log the full exception details
                        System.Diagnostics.Debug.WriteLine($"Error converting {fileItem.SourcePath}: {ex}"); // Use SourcePath for logging
                    }
                    finally
                    {
                         // Update progress regardless of success/failure
                         processedCount++;
                         await Application.Current.Dispatcher.InvokeAsync(() =>
                         {
                             Progress = (int)Math.Round((double)processedCount * 100 / totalFiles);
                         });
                    }
                }
            });

            // Final UI updates after loop finishes
            CurrentStatus = $"转换完成 ({successCount} 成功, {ErrorList.Count} 失败)";
            IsConverting = false; // Re-enable buttons
            MessageBox.Show($"转换完成！\n成功: {successCount}\n失败: {ErrorList.Count}", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanClearList(object? parameter)
        {
             return !IsConverting && FileList.Any(); // Check IsConverting
        }

        private void ClearList(object? parameter)
        {
            FileList.Clear();
            ErrorList.Clear();
            Progress = 0;
            CurrentStatus = "列表已清空";
        }

        private void RetryFile(object? parameter)
        {
             if (parameter is FileError errorToRetry)
             {
                 // Find the original item if it still exists in the error list using SourcePath
                 var errorItem = ErrorList.FirstOrDefault(e => e.FilePath == errorToRetry.FilePath); // FileError still uses FilePath, which is the SourcePath
                 if (errorItem != null)
                 {
                     ErrorList.Remove(errorItem); // Remove from errors
                     // Find if it's already back in the main list using SourcePath
                     if (!FileList.Any(f => f.SourcePath == errorItem.FilePath))
                     {
                          var fileItem = new FileItem(errorItem.FilePath) { Status = "待重试" }; // Create new item with SourcePath
                          FileList.Add(fileItem); // Add back to main list for next conversion run
                     }
                     else
                     {
                          // Optionally update status if already in list using SourcePath
                          var existingItem = FileList.First(f => f.SourcePath == errorItem.FilePath);
                          existingItem.Status = "待重试";
                     }
                     CurrentStatus = $"文件 '{Path.GetFileName(errorItem.FilePath)}' 已准备重试"; // Use FilePath from FileError (which is SourcePath)
                 }
             }
             else
             {
                 MessageBox.Show("无法重试：未提供有效的错误项。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
             }
             // TODO: Consider triggering conversion immediately or just adding back to list
        }

        private void EditFileName(object? parameter)
        {
            if (parameter is FileItem item)
            {
                // Ensure only one item is in edit mode at a time (optional, but good practice)
                // foreach (var file in FileList.Where(f => f.IsEditing && f != item))
                // {
                //     file.IsEditing = false;
                //     // Optionally save or revert changes on other items here
                // }
                item.IsEditing = true;
            }
        }

        private void SaveFileName(object? parameter)
        {
            if (parameter is FileItem item)
            {
                item.IsEditing = false;
                ValidateFileName(item); // Validate and potentially correct the name
            }
        }

        private void ValidateFileName(FileItem item)
        {
            string originalName = item.OutputFileName;
            string sanitizedName = originalName;

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            sanitizedName = string.Concat(sanitizedName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Trim whitespace
            sanitizedName = sanitizedName.Trim();

            // Ensure name is not empty after sanitization
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                MessageBox.Show($"文件名 '{originalName}' 无效或只包含非法字符，已重置为默认名称。", "文件名无效", MessageBoxButton.OK, MessageBoxImage.Warning);
                item.OutputFileName = item.GenerateDefaultName(item.SourcePath); // Reset to default
                return; // Exit validation
            }

            // Check if name changed after sanitization
            if (sanitizedName != originalName)
            {
                 MessageBox.Show($"文件名 '{originalName}' 包含非法字符或前后空格，已自动修正为 '{sanitizedName}'。", "文件名修正", MessageBoxButton.OK, MessageBoxImage.Information);
                 item.OutputFileName = sanitizedName;
            }

            // Optional: Check for reserved names (CON, PRN, AUX, NUL, COM1-9, LPT1-9) - less common issue
            // Optional: Check for max path length issues if combining with OutputPath

            // Note: We don't add .pdf here; the Convert method handles that.
            // This keeps the editable part clean.
        }
    }
}
