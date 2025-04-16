using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MhtToPdfConverter.ViewModels; // Add this using statement
using System.IO; // For Directory/File checks
using System.Linq; // For LINQ operations

namespace MhtToPdfConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Set the DataContext to an instance of MainViewModel
            DataContext = new MainViewModel();
        }

        // Event handler for Drag and Drop onto the DataGrid
        private void FileDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
                var filesToAdd = new List<string>();

                foreach (var itemPath in droppedItems)
                {
                    if (Directory.Exists(itemPath)) // It's a folder
                    {
                        try
                        {
                            // Recursively find MHT/MHTML files (or just top-level based on requirements)
                            // Let's stick to top-level for now, matching AddFolder placeholder
                            var mhtFiles = Directory.EnumerateFiles(itemPath, "*.mht", SearchOption.TopDirectoryOnly)
                                                    .Concat(Directory.EnumerateFiles(itemPath, "*.mhtml", SearchOption.TopDirectoryOnly));
                            filesToAdd.AddRange(mhtFiles);
                        }
                        catch (Exception ex)
                        {
                             MessageBox.Show($"无法读取文件夹 '{itemPath}' 中的文件: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                             // Optionally log the error
                        }
                    }
                    else if (File.Exists(itemPath)) // It's a file
                    {
                        string extension = System.IO.Path.GetExtension(itemPath).ToLowerInvariant(); // Explicitly use System.IO.Path
                        if (extension == ".mht" || extension == ".mhtml")
                        {
                            filesToAdd.Add(itemPath);
                        }
                    }
                }

                // Add unique files to the ViewModel's list
                foreach (var sourcePath in filesToAdd.Distinct(StringComparer.OrdinalIgnoreCase)) // Renamed variable for clarity
                {
                     // Use SourcePath for comparison
                     if (!viewModel.FileList.Any(f => f.SourcePath.Equals(sourcePath, StringComparison.OrdinalIgnoreCase)))
                     {
                         viewModel.FileList.Add(new Models.FileItem(sourcePath)); // Pass sourcePath to constructor
                     } // Added missing closing brace
                 }
            }
        }

        // Event handler for Double Click on a DataGrid Row
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is Models.FileItem fileItem)
            {
                if (DataContext is MainViewModel viewModel && viewModel.EditFileNameCommand.CanExecute(fileItem))
                {
                    viewModel.EditFileNameCommand.Execute(fileItem);
                } // Added missing closing brace
            }
        }
    }
} // Removed extra closing brace
