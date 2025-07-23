namespace FileStorage.ConsoleApp.Services;

public interface IFileProcessor
{
    Task<Guid> ProcessFileAsync(string filePath);
}