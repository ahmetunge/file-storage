namespace FileStorage.ConsoleApp.Services;

public interface IFileProcessor
{
    Task<Guid> ProcessFileAsync(string filePath);
    
    Task<List<Guid>> ProcessFolderAsync(string folderPath);

    Task<string> RestoreFile(Guid fileId, string outputDirectory);
}