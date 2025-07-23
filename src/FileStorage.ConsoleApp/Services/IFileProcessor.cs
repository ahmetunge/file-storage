using FileStorage.ConsoleApp.Data.Entities;

namespace FileStorage.ConsoleApp.Services;

public interface IFileProcessor
{
    Task<Guid> ProcessFile(string filePath);
    
    Task<List<Guid>> ProcessFolder(string folderPath);

    Task<string> RestoreFile(Guid fileId, string outputDirectory);

    Task<List<FileMetadata>> GetAllFiles();
}