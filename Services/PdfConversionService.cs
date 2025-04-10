using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using System.Windows; // For Window
using System.Windows.Interop; // For WindowInteropHelper
using System.Windows.Threading; // For Dispatcher

namespace MhtToPdfConverter.Services
{
    public static class PdfConversionService
    {
        /// <summary>
        /// Converts a single MHT/MHTML file to PDF using WebView2.
        /// Runs the core logic on the main WPF Dispatcher thread.
        /// </summary>
        /// <param name="mhtFilePath">Full path to the input MHT file.</param>
        /// <param name="pdfOutputPath">Full path for the output PDF file.</param>
        /// <param name="overwrite">Whether to overwrite the output file if it exists.</param>
        /// <returns>True if conversion was successful, false otherwise.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the input MHT file does not exist.</exception>
        /// <exception cref="IOException">Thrown if the output directory cannot be created or if the output file exists and overwrite is false.</exception>
        /// <exception cref="InvalidOperationException">Thrown if WebView2 components fail to initialize.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors during conversion.</exception>
        public static async Task<bool> ConvertSingleFileAsync(string mhtFilePath, string pdfOutputPath, bool overwrite)
        {
            // --- Pre-checks ---
            if (!File.Exists(mhtFilePath))
            {
                throw new FileNotFoundException($"Input MHT file not found: {mhtFilePath}");
            }

            string? outputDir = Path.GetDirectoryName(pdfOutputPath);
            if (string.IsNullOrEmpty(outputDir))
            {
                 throw new ArgumentException("Invalid output path specified.", nameof(pdfOutputPath));
            }

            if (!Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                    System.Diagnostics.Debug.WriteLine($"Created output directory: {outputDir}");
                }
                catch (Exception ex)
                {
                    throw new IOException($"Error creating output directory '{outputDir}': {ex.Message}", ex);
                }
            }

            if (File.Exists(pdfOutputPath) && !overwrite)
            {
                throw new IOException($"Output file '{pdfOutputPath}' already exists and overwrite is set to false.");
            }
            // --- End Pre-checks ---


            // Use a TaskCompletionSource to bridge the event-based WebView2 completion
            var tcs = new TaskCompletionSource<bool>();
            Window? hiddenWindow = null;
            CoreWebView2Controller? controller = null;

            // WebView2 operations need to run on an STA thread with a dispatcher.
            // We assume this method is called from a background thread (via Task.Run in ViewModel),
            // so we need to marshal the call to the main UI thread's dispatcher.
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                CoreWebView2Environment? environment = null;
                try
                {
                    // Create hidden window for WebView2 controller
                    hiddenWindow = new Window { Width = 0, Height = 0, WindowStyle = WindowStyle.None, ShowInTaskbar = false, ShowActivated = false, AllowsTransparency = true, Opacity = 0 };
                    var windowInteropHelper = new WindowInteropHelper(hiddenWindow);
                    var windowHandle = windowInteropHelper.EnsureHandle();
                    if (windowHandle == IntPtr.Zero) throw new InvalidOperationException("Failed to create hidden window handle.");

                    // Create WebView2 Environment and Controller
                    string userDataFolder = Path.Combine(Path.GetTempPath(), $"MhtToPdfConverter_WebView2_{Guid.NewGuid()}"); // Unique folder per conversion potentially
                    environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    controller = await environment.CreateCoreWebView2ControllerAsync(windowHandle);
                    if (controller == null) throw new InvalidOperationException("Failed to create CoreWebView2Controller.");

                    // Navigate and Print
                    string fileUri = new Uri(mhtFilePath).AbsoluteUri;
                    System.Diagnostics.Debug.WriteLine($"Navigating to: {fileUri}");

                    controller.CoreWebView2.NavigationCompleted += async (sender, e) =>
                    {
                        if (e.IsSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine("Navigation successful.");
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"Attempting to print to PDF: {pdfOutputPath}");
                                var printSettings = environment.CreatePrintSettings(); // Use default settings for now
                                bool success = await controller.CoreWebView2.PrintToPdfAsync(pdfOutputPath, printSettings);
                                tcs.TrySetResult(success); // Signal completion status
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error during PDF printing: {ex}");
                                tcs.TrySetException(ex); // Signal error
                            }
                            finally
                            {
                                controller?.Close(); // Close controller after printing attempt
                                hiddenWindow?.Close(); // Close hidden window
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Navigation failed. Status: {e.WebErrorStatus}");
                            tcs.TrySetResult(false); // Signal navigation failure
                            controller?.Close();
                            hiddenWindow?.Close();
                        }
                    };

                    // Handle process failure (e.g., browser process crashes)
                    controller.CoreWebView2.ProcessFailed += (sender, e) =>
                    {
                         System.Diagnostics.Debug.WriteLine($"WebView2 Process Failed: {e.Reason}");
                         tcs.TrySetException(new Exception($"WebView2 process failed unexpectedly. Reason: {e.Reason}"));
                         controller?.Close();
                         hiddenWindow?.Close();
                    };

                    controller.CoreWebView2.Navigate(fileUri);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during WebView2 setup/navigation: {ex}");
                    tcs.TrySetException(ex); // Signal error if setup fails
                    controller?.Close(); // Attempt cleanup
                    hiddenWindow?.Close();
                }
            });

            // Wait for the conversion process (signaled by tcs) to complete
            return await tcs.Task;
        }
    }
}
