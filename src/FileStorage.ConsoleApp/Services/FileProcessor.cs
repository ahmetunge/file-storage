using FileStorage.ConsoleApp.Data;
using FileStorage.ConsoleApp.Data.Entities;
using FileStorage.ConsoleApp.Extensions;
using FileStorage.ConsoleApp.Providers;
using Microsoft.EntityFrameworkCore;
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


    public async Task<Guid> ProcessFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            _logger.LogError("File not found");

            return Guid.Empty;
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

        if (chunks is null || chunks.Count == 0)
        {
            _logger.LogError("An error while chunk files");

            return Guid.Empty;
        }

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

    public async Task<List<Guid>> ProcessFolder(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            _logger.LogError("No files found in folder: {FolderPath}", folderPath);
            return new List<Guid>();
        }

        List<Guid> fileGuids = new List<Guid>();

        foreach (var file in files)
        {
            try
            {
                var id = await ProcessFile(file);

                fileGuids.Add(id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing file: {FileName}", file);
            }
        }

        return fileGuids;
    }

    public async Task<string> RestoreFile(Guid fileId, string outputDirectory)
    {
        var fileMetadata = await _fileStorageDbContext.FileMetadata
            .Include(f => f.Chunks!.OrderBy(c => c.Order))
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (fileMetadata == null)
        {
            _logger.LogError("File with ID {FileId} not found in metadata.", fileId);

            return string.Empty;
        }

        _logger.LogInformation("Starting to restore file: {FileName}", fileMetadata.FileName);

        Directory.CreateDirectory(outputDirectory);
        var outputFilePath = Path.Combine(outputDirectory, $"Restored_{fileMetadata.FileName}");

        await using (var outputFileStream = new FileStream(outputFilePath, FileMode.Create))
        {
            foreach (var chunkMetadata in fileMetadata!.Chunks)
            {
                var storageProvider =
                    _storageProviders.FirstOrDefault(p => p.ProviderType == chunkMetadata.StorageProviderType);
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider {ProviderType} not found for chunk {ChunkId}",
                        chunkMetadata.StorageProviderType, chunkMetadata.Id);
                    outputFileStream.Close();
                    File.Delete(outputFilePath);

                    return string.Empty;
                }

                var chunkData = await storageProvider.ReadChunkAsync(chunkMetadata.Id.ToString());

                await outputFileStream.WriteAsync(chunkData, 0, chunkData.Length);
            }
        }

        _logger.LogInformation("File restored at: {OutputFilePath}", outputFilePath);

        byte[] fileBytes = await File.ReadAllBytesAsync(outputFilePath);

        var newChecksum = fileBytes.ComputeChecksum();

        if (newChecksum == fileMetadata.Checksum)
        {
            _logger.LogInformation("Checksum VERIFIED. File integrity is confirmed. [SUCCESS]");
        }
        else
        {
            _logger.LogError("Checksum MISMATCH. Original: {OriginalChecksum}, restored: {NewChecksum}. [FAILURE]",
                fileMetadata.Checksum, newChecksum);
        }

        return outputFilePath;
    }

    public async Task<List<FileMetadata>> GetAllFiles()
    {
        var files = await _fileStorageDbContext.FileMetadata.AsTracking().ToListAsync();

        return files;
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(Guid fileId)
    {
        var file = await _fileStorageDbContext.FileMetadata.AsTracking().FirstOrDefaultAsync(f => f.Id == fileId);

        return file;
    }
}