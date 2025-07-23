using FileStorage.ConsoleApp.Services;
using Microsoft.Extensions.Logging;

namespace FileStorage.ConsoleApp;

public class ConsoleRunner
{
    private readonly IFileProcessor _fileProcessor;
    private readonly ILogger<ConsoleRunner> _logger;

    public ConsoleRunner(
        ILogger<ConsoleRunner> logger,
        IFileProcessor fileProcessor)
    {
        _logger = logger;
        _fileProcessor = fileProcessor;
    }

    public async Task Run()
    {
        Console.WriteLine("=== File Storage System ===");
        Console.WriteLine();

        while (true)
        {
            DisplayMenu();
            var choice = Console.ReadLine();
            try
            {
                switch (choice)
                {
                    case "1":
                        await ProcessSingleFileAsync();
                        break;
                    case "2":
                        await ProcessMultipleFilesAsync();
                        break;
                    case "3":
                        await RestoreFileAsync();
                        break;
                    case "4":
                        await ListAllFilesAsync();
                        break;
                    case "5":
                        await DeleteFileAsync();
                        break;
                    case "6":
                        await ShowFileDetailsAsync();
                        break;
                    case "0":
                        Console.WriteLine("Exiting application...");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                _logger.LogError(ex, "Error in console application");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private void DisplayMenu()
    {
        Console.WriteLine("Select an option:");
        Console.WriteLine("1. Process Single File");
        Console.WriteLine("2. Process Multiple Files");
        Console.WriteLine("3. Restore File");
        Console.WriteLine("4. List All Files");
        Console.WriteLine("5. Delete File");
        Console.WriteLine("6. Show File Details");
        Console.WriteLine("0. Exit");
        Console.Write("\nEnter your choice: ");
    }

    private async Task ProcessSingleFileAsync()
    {
        Console.Write("Enter file path: ");
        var filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("File not found or invalid path.");
            return;
        }

        Console.WriteLine($"Processing file: {filePath}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var fileId = await _fileProcessor.ProcessFile(filePath);

        stopwatch.Stop();
        Console.WriteLine($"File processed successfully!");
        Console.WriteLine($"File ID: {fileId}");
        Console.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task ProcessMultipleFilesAsync()
    {
        Console.Write("Enter folder path: ");
        var directoryPath = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            Console.WriteLine("Directory not found or invalid path.");
            return;
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var processedGuids = await _fileProcessor.ProcessFolder(directoryPath);
        
        stopwatch.Stop();
       
        Console.WriteLine($"\nProcessing completed:");
        Console.WriteLine(string.Join(", ", processedGuids));
        Console.WriteLine($"Total processing time: {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task RestoreFileAsync()
    {
        Console.Write("Enter File ID: ");
        var fileIdInput = Console.ReadLine();
        
        if (!Guid.TryParse(fileIdInput, out var fileId))
        {
            Console.WriteLine("Invalid File ID format.");
            return;
        }
        
        Console.Write("Enter output directory: ");
        var outputDirectory = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(outputDirectory))
            outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        
        Console.WriteLine($"Restoring file: {fileId}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var restoredPath = await _fileProcessor.RestoreFile(fileId, outputDirectory);
        
        stopwatch.Stop();
        Console.WriteLine($"File restored successfully!");
        Console.WriteLine($"Restored to: {restoredPath}");
        Console.WriteLine($"Restoration time: {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task ListAllFilesAsync()
    {
        Console.WriteLine("Loading files...");
        var files = await _fileProcessor.GetAllFiles();
        
        if (!files.Any())
        {
            Console.WriteLine("No files found.");
            return;
        }
        
        Console.WriteLine($"\nFound {files.Count} files:");
        Console.WriteLine(new string('-', 120));
        Console.WriteLine($"{"ID",-38} {"File Name",-30} {"Size",-15}  {"CreatedAt4"}");
        Console.WriteLine(new string('-', 120));
        
        foreach (var file in files)
        {
            var sizeText = FormatFileSize(file.FileSize);
            Console.WriteLine($"{file.Id,-38} {TruncateString(file.FileName, 28),-30} {sizeText,-15}  {file.CreatedAt:yyyy-MM-dd HH:mm}");
        }
        
        Console.WriteLine(new string('-', 120));
    }

    private async Task DeleteFileAsync()
    {
       
    }

    private async Task ShowFileDetailsAsync()
    {
      Console.Write("Enter File ID: ");
        var fileIdInput = Console.ReadLine();
        
        if (!Guid.TryParse(fileIdInput, out var fileId))
        {
            Console.WriteLine("Invalid File ID format.");
            return;
        }
        
        var fileMetadata = await _fileProcessor.GetFileMetadataAsync(fileId);
        if (fileMetadata == null)
        {
            Console.WriteLine("File not found.");
            return;
        }
        
        Console.WriteLine($"\nFile Details:");
        Console.WriteLine($"ID: {fileMetadata.Id}");
        Console.WriteLine($"File Name: {fileMetadata.FileName}");
        Console.WriteLine($"Original Path: {fileMetadata.FilePath}");
        Console.WriteLine($"File Size: {FormatFileSize(fileMetadata.FileSize)}");
        Console.WriteLine($"Checksum: {fileMetadata.Checksum}");
        Console.WriteLine($"Created: {fileMetadata.CreatedAt}");
        
        if (fileMetadata.Chunks?.Any() == true)
        {
            Console.WriteLine($"\nChunk Details:");
            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"{"Index",-6} {"Size",-15} {"Provider",-15} {"Checksum",-20} {"Created"}");
            Console.WriteLine(new string('-', 100));
        
            foreach (var chunk in fileMetadata.Chunks.OrderBy(c => c.Order))
            {
                var chunkSizeText = FormatFileSize(chunk.ChunkSize);
                Console.WriteLine($"{chunkSizeText,-15} {chunk.CreatedAt:yyyy-MM-dd HH:mm}");
            }
            Console.WriteLine(new string('-', 100));
        }
    }
    
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
    
    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return input[..(maxLength - 3)] + "...";
    }
}