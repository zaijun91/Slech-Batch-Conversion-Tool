using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace MhtToPdfConverter.Converters
{
    /// <summary>
    /// Converts a full path string into a shorter representation for display purposes.
    /// Example: "C:\Users\Username\Documents\Very Long Folder Name\Subfolder\file.txt"
    /// might become "C:\Users\...\Subfolder\file.txt"
    /// </summary>
    public class ShortPathConverter : IValueConverter
    {
        public int MaxPrefixLength { get; set; } = 3; // Number of directory parts to show at the beginning
        public int MaxSuffixLength { get; set; } = 2; // Number of directory parts (including filename) to show at the end
        public string Ellipsis { get; set; } = "..."; // String to use for omitted parts

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string fullPath || string.IsNullOrEmpty(fullPath))
            {
                return string.Empty; // Return empty for null or empty input
            }

            try
            {
                // Split the path into components
                string[] parts = fullPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                // If the path is already short enough, return it as is
                if (parts.Length <= MaxPrefixLength + MaxSuffixLength)
                {
                    return fullPath;
                }

                // Construct the shortened path
                string prefix = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(MaxPrefixLength));
                string suffix = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(parts.Length - MaxSuffixLength));

                // Handle drive letter if present (Windows specific)
                string? driveLetter = Path.GetPathRoot(fullPath);
                if (!string.IsNullOrEmpty(driveLetter) && !prefix.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure the drive letter is included if it wasn't part of the prefix split
                    // This usually happens if MaxPrefixLength is small (e.g., 1)
                    if (MaxPrefixLength > 0 && parts.Length > 0) // Check if parts exist
                    {
                         // Rebuild prefix to include drive if necessary
                         prefix = driveLetter + string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(MaxPrefixLength));
                    }
                    else
                    {
                         prefix = driveLetter + prefix; // Simple prepend if MaxPrefixLength was 0
                    }

                }
                 // Special case: If the prefix itself is just the drive letter, avoid "C:\...".
                 if (prefix.Length <= 3 && prefix.EndsWith(Path.DirectorySeparatorChar.ToString())) // e.g., "C:\"
                 {
                      // Take one more part for the prefix if available
                      if (parts.Length > MaxPrefixLength)
                      {
                           prefix = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(MaxPrefixLength + 1));
                      }
                 }


                return $"{prefix}{Path.DirectorySeparatorChar}{Ellipsis}{Path.DirectorySeparatorChar}{suffix}";
            }
            catch (Exception ex)
            {
                // Log error or handle gracefully
                System.Diagnostics.Debug.WriteLine($"Error shortening path '{fullPath}': {ex.Message}");
                return fullPath; // Return original path on error
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for one-way binding
            throw new NotImplementedException();
        }
    }
}
