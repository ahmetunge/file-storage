using FileStorage.ConsoleApp.Constants;
using Microsoft.Extensions.Logging;

namespace FileStorage.ConsoleApp.Providers;

public class FileSystemStorageProvider: IStorageProvider
{
    private readonly ILogger<FileSystemStorageProvider> _logger;
    public string ProviderType => "FileSystem";

    public FileSystemStorageProvider(ILogger<FileSystemStorageProvider> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(AppConstants.FileStoragePath);
        _logger.LogInformation("Chunks path: {ChunksPath}", AppConstants.FileStoragePath);
    }
    
    public async Task SaveChunk(string chunkId, byte[] data)
    {
        var filePath = Path.Combine(AppConstants.FileStoragePath, chunkId);
        await File.WriteAllBytesAsync(filePath, data);
        _logger.LogInformation("Chunk {ChunkId} saved to FileSystem at {FilePath}", chunkId, filePath);
    }

    public async Task<byte[]> ReadChunkAsync(string chunkId)
    {
        var filePath = Path.Combine(AppConstants.FileStoragePath, chunkId);
        if (!File.Exists(filePath))
        {
            _logger.LogError("Chunk {ChunkId} not found in FileSystem at {FilePath}", chunkId, filePath);
            throw new FileNotFoundException($"Chunk file not found: {filePath}");
        }
        var data = await File.ReadAllBytesAsync(filePath);
        _logger.LogInformation("Chunk {ChunkId} read from FileSystem", chunkId);
        return data;
    }
}