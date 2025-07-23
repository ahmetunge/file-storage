using FileStorage.ConsoleApp.Data;
using FileStorage.ConsoleApp.Data.Entities;
using FileStorage.ConsoleApp.Extensions;
using FileStorage.ConsoleApp.Providers;
using Microsoft.Extensions.Logging;

namespace FileStorage.ConsoleApp.Services;

public class FileProcessor : IFileProcessor
{
    private readonly ILogger<FileProcessor> _logger;
    private readonly FileStorageDbContext _fileStorageDbContext;
    private readonly IEnumerable<IStorageProvider> _storageProviders;
    private readonly IChunkingService _chunkingService;

    public FileProcessor(
        ILogger<FileProcessor> logger,
        FileStorageDbContext fileStorageDbContext,
        IEnumerable<IStorageProvider> storageProviders,
        IChunkingService chunkingService)
    {
        _logger = logger;
        _fileStorageDbContext = fileStorageDbContext;
        _storageProviders = storageProviders;
        _chunkingService = chunkingService;
    }


    public async Task<Guid> ProcessFileAsync(string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException();
        }

        _logger.LogInformation("Starting to process file: {FileName}", fileInfo.Name);

        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

        var checksum = fileBytes.ComputeChecksum();

        DateTime now = DateTime.UtcNow;

        Guid fileMetadataId = Guid.NewGuid();

        var fileMetadata = new FileMetadata
        {
            Id = fileMetadataId,
            FileSize = fileInfo.Length,
            Checksum = checksum,
            FileName = fileInfo.Name,
            FilePath = filePath,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = nameof(FileProcessor),
            UpdatedBy = nameof(FileProcessor),
        };

        _fileStorageDbContext.FileMetadata.Add(fileMetadata);

        await using var fileStream = fileInfo.OpenRead();

        var chunks = await _chunkingService.ChunkFile(fileStream, fileInfo.Length);

        var storageProvidersList = _storageProviders.ToList();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var storageProvider = storageProvidersList[i % storageProvidersList.Count];

            var chunkId = Guid.NewGuid();

            await storageProvider.SaveChunk(chunkId.ToString(), chunk);

            var chunkMetadata = new ChunkMetadata
            {
                Id = chunkId,
                FileMetadataId = fileMetadataId,
                ChunkSize = chunk.Length,
                StorageProviderType = storageProvider.ProviderType,
                Order = i + 1,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = nameof(FileProcessor),
                UpdatedBy = nameof(FileProcessor),
            };

            _fileStorageDbContext.ChunkMetadata.Add(chunkMetadata);
        }

        await _fileStorageDbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully processed and stored all chunks for file: {FileName}", fileInfo.Name);

        return fileMetadataId;
    }
}