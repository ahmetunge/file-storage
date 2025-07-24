using System.Text;
using FileStorage.ConsoleApp.Constants;
using FileStorage.ConsoleApp.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileStorage.Tests;

public class FileSystemStorageProviderTests : IDisposable
{
    private readonly FileSystemStorageProvider _storageProvider;
    private readonly string _testStoragePath;

    public FileSystemStorageProviderTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "StorageProviderTests_" + Guid.NewGuid());
        
        AppConstants.FileStoragePath = _testStoragePath;

        var logger = NullLogger<FileSystemStorageProvider>.Instance;
        
        _storageProvider = new FileSystemStorageProvider(logger);
    }
    
    [Fact]
    public void Constructor_ShouldCreateStorageDirectory_WhenItDoesNotExist()
    {
        // Arrange

        // Act
        bool directoryExists = Directory.Exists(_testStoragePath);

        // Assert
        Assert.True(directoryExists);
    }

    [Fact]
    public async Task SaveChunk_ShouldCreateFile_WithCorrectContent()
    {
        // Arrange
        var chunkId = "test-chunk-001";
        var fileContent = Encoding.UTF8.GetBytes("this is a file content");
        var expectedFilePath = Path.Combine(_testStoragePath, chunkId);

        // Act
        await _storageProvider.SaveChunk(chunkId, fileContent);

        // Assert
        Assert.True(File.Exists(expectedFilePath));
        
        var actualFileContent = await File.ReadAllBytesAsync(expectedFilePath);
        Assert.Equal(fileContent, actualFileContent);
    }

    [Fact]
    public async Task ReadChunkAsync_ShouldReturnCorrectData_WhenFileExists()
    {
        // Arrange
        var chunkId = "existing-chunk-002";
        var expectedData = Encoding.UTF8.GetBytes("this is expected data");
        var filePath = Path.Combine(_testStoragePath, chunkId);
        
        await File.WriteAllBytesAsync(filePath, expectedData);

        // Act
        var actualData = await _storageProvider.ReadChunkAsync(chunkId);

        // Assert
        Assert.NotNull(actualData);
        Assert.Equal(expectedData, actualData);
    }

    [Fact]
    public async Task ReadChunkAsync_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentChunkId = "yok-boyle-bir-chunk";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _storageProvider.ReadChunkAsync(nonExistentChunkId));
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, recursive: true);
        }
    }
}