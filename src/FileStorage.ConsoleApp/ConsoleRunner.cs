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

        var fileId = await _fileProcessor.ProcessFileAsync(filePath);

        stopwatch.Stop();
        Console.WriteLine($"File processed successfully!");
        Console.WriteLine($"File ID: {fileId}");
        Console.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task ProcessMultipleFilesAsync()
    {
       
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
      
    }

    private async Task DeleteFileAsync()
    {
       
    }

    private async Task ShowFileDetailsAsync()
    {
      
    }
}