using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using FileStorage.ConsoleApp.Data;
using FileStorage.ConsoleApp.Data.Entities;
using FileStorage.ConsoleApp.Extensions;
using FileStorage.ConsoleApp.Providers;
using FileStorage.ConsoleApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace FileStorage.Tests
{
    public class FileProcessorTests : IDisposable
    {
        private readonly Mock<ILogger<FileProcessor>> _mockLogger;
        private readonly Mock<IChunkingService> _mockChunkingService;
        private readonly Mock<IStorageProvider> _mockStorageProvider;
        private readonly List<IStorageProvider> _storageProviders;

        private FileStorageDbContext _dbContext;

        private readonly string _tempFilePath;
        private readonly string _tempFileName;
        private readonly byte[] _fileContent;
        private readonly string _tempFolderPath;
        private readonly string _testOutputDirectory;

        private const string ProviderType = "ProviderType";

        public FileProcessorTests()
        {
            _mockLogger = new Mock<ILogger<FileProcessor>>();
            _mockChunkingService = new Mock<IChunkingService>();
            _mockStorageProvider = new Mock<IStorageProvider>();

            _mockStorageProvider.Setup(p => p.ProviderType).Returns(ProviderType);
            _storageProviders = new List<IStorageProvider> { _mockStorageProvider.Object };

            _tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolderPath);

            _tempFileName = "testfile.txt";
            _tempFilePath = Path.Combine(Path.GetTempPath(), _tempFileName);
            _fileContent = new byte[1000];
            _testOutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            new Random().NextBytes(_fileContent);
            File.WriteAllBytes(_tempFilePath, _fileContent);
        }

        [Fact]
        public async Task ProcessFile_ShouldReturnEmptyGuid_WhenFileDoesNotExist()
        {
            // Arrange
            var nonExistentFilePath = "C:\\non_existent_file.tmp";

            // Act

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );

            var result = await fileProcessor.ProcessFile(nonExistentFilePath);

            // & Assert
            Assert.Equal(result, Guid.Empty);
        }

        [Fact]
        public async Task ProcessFile_ShouldReturnEmptyGuid_WhenCFileCoulnotChunked()
        {
            // Arrange
            var nonExistentFilePath = _tempFilePath;

            List<byte[]> bytes = new List<byte[]>();

            _mockChunkingService.Setup(x => x.ChunkFile(It.IsAny<Stream>(), It.IsAny<long>()))
               .ReturnsAsync(bytes);

            // Act

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );

            var result = await fileProcessor.ProcessFile(nonExistentFilePath);

            // & Assert
            Assert.Equal(result, Guid.Empty);
        }

        [Fact]
        public async Task ProcessFile_ShouldProcessFileAndSaveChanges_WhenFileExists()
        {
            // Arrange
            var chunks = new List<byte[]>
            {
                new byte[400],
                new byte[400],
                new byte[200]
            };

            _mockChunkingService.Setup(s => s.ChunkFile(It.IsAny<Stream>(), It.IsAny<long>()))
                               .ReturnsAsync(chunks);

            // Act

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );

            var fileId = await fileProcessor.ProcessFile(_tempFilePath);

            // Assert
            Assert.Equal(1, _dbContext.FileMetadata.Count());
            var fileMeta = await _dbContext.FileMetadata.FindAsync(fileId);
            Assert.NotNull(fileMeta);
            Assert.Equal(_fileContent.Length, fileMeta.FileSize);
            Assert.Equal(_tempFileName, fileMeta.FileName);

            Assert.Equal(3, _dbContext.ChunkMetadata.Count());
            var chunkMetas = _dbContext.ChunkMetadata.OrderBy(c => c.Order).ToList();

            Assert.Equal(1, chunkMetas[0].Order);
            Assert.Equal(chunks[0].Length, chunkMetas[0].ChunkSize);
            Assert.Equal(ProviderType, chunkMetas[0].StorageProviderType);

            _mockStorageProvider.Verify(p => p.SaveChunk(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(3)); // 1. ve 3. chunk
            _mockChunkingService.Verify(s => s.ChunkFile(It.IsAny<Stream>(), _fileContent.Length), Times.Once);
        }

        [Fact]
        public async Task ProcessFolder_ShouldReturnEmptyListAndLog_WhenFolderIsEmpty()
        {
            // Arrange

            // Act

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );

            var result = await fileProcessor.ProcessFolder(_tempFolderPath); ;

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
                log => log.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No files found in folder")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFolder_ShouldProcessAllFiles_WhenAllSucceed()
        {
            // Arrange
            var file1Path = Path.Combine(_tempFolderPath, "file1.txt");
            var file2Path = Path.Combine(_tempFolderPath, "file2.txt");
            File.Create(file1Path).Dispose();
            File.Create(file2Path).Dispose();

            var chunks = new List<byte[]>
            {
                new byte[400],
                new byte[400],
                new byte[200]
            };

            _mockChunkingService.Setup(s => s.ChunkFile(It.IsAny<Stream>(), It.IsAny<long>()))
                               .ReturnsAsync(chunks);

            // Act
            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );
            var result = await fileProcessor.ProcessFolder(_tempFolderPath);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(2, _dbContext.FileMetadata.Count());

            Assert.Equal(6, _dbContext.ChunkMetadata.Count());

            _mockStorageProvider.Verify(p => p.SaveChunk(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(6));
            _mockChunkingService.Verify(s => s.ChunkFile(It.IsAny<Stream>(), It.IsAny<long>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RestoreFile_ReturnEmptyString_WhenFileNotFound()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            // Act
            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );
            var result = await fileProcessor.RestoreFile(It.IsAny<Guid>(), It.IsAny<string>());

            // Assert
            Assert.Equal(string.Empty, result);

            _mockLogger.Verify(
               log => log.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File with ID")),
                   null,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Fact]
        public async Task RestoreFile_ReturnOutputFile_WhenChecksumMatches()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();
            var fileName = "test.txt";
            var chunkData = Encoding.UTF8.GetBytes("Hello World");
            var checksum = chunkData.ComputeChecksum();

            var createdAt = DateTime.UtcNow.AddDays(-1);
            var createdBy = "faker.user";

            var chunkFaker = new Faker<ChunkMetadata>()
                .RuleFor(c => c.Id, f => chunkId)
                .RuleFor(c => c.FileMetadataId, _ => fileId)
                .RuleFor(c => c.ChunkSize, f => f.Random.Long(512 * 1024, 2 * 1024 * 1024))
                .RuleFor(c => c.Order, f => f.IndexFaker + 1)
                .RuleFor(c => c.StorageProviderType, f => ProviderType)
                .RuleFor(c => c.CreatedAt, _ => createdAt)
                .RuleFor(c => c.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(c => c.CreatedBy, _ => createdBy)
                .RuleFor(c => c.UpdatedBy, _ => createdBy)
                .RuleFor(c => c.IsDeleted, f => f.Random.Bool(0.05f))
                .RuleFor(c => c.File, _ => null);

            var fakeChunk = chunkFaker.Generate();

            var fileFaker = new Faker<FileMetadata>()
                .RuleFor(f => f.Id, _ => fileId)
                .RuleFor(f => f.FileName, f => f.System.FileName())
                .RuleFor(f => f.FilePath, f => f.System.FilePath())
                .RuleFor(f => f.FileSize, f => f.Random.Long(5 * 1024 * 1024, 100 * 1024 * 1024))
                .RuleFor(f => f.Checksum, f => f.Random.Hash())
                .RuleFor(f => f.CreatedAt, _ => createdAt)
                .RuleFor(f => f.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(f => f.CreatedBy, _ => createdBy)
                .RuleFor(f => f.UpdatedBy, _ => createdBy)
                .RuleFor(f => f.IsDeleted, f => f.Random.Bool(0.1f))
                .RuleFor(f => f.Chunks, f => new List<ChunkMetadata> { fakeChunk });

            var fakeFile = fileFaker.Generate();

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            _dbContext.FileMetadata.Add(fakeFile);
            await _dbContext.SaveChangesAsync();

            _mockStorageProvider.Setup(x => x.ReadChunkAsync(chunkId.ToString()))
                .ReturnsAsync(chunkData);

            // Act
            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );


            var result = await fileProcessor.RestoreFile(fileId, _testOutputDirectory);

            // Assert
            Assert.NotEmpty(result);
            Assert.True(File.Exists(result));
            Assert.Equal($"Restored_{fakeFile.FileName}", Path.GetFileName(result));

            var restoredContent = await File.ReadAllBytesAsync(result);
            Assert.Equal(chunkData, restoredContent);

            _mockLogger.Verify(
               log => log.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File restored at:")),
                   null,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Fact]
        public async Task RestoreFile_ShouldLogsError_WhenMismatchFaile()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();
            var fileName = "test.txt";
            var chunkData = Encoding.UTF8.GetBytes("Hello World");
            var checksum = chunkData.ComputeChecksum();

            var createdAt = DateTime.UtcNow.AddDays(-1);
            var createdBy = "faker.user";

            var chunkFaker = new Faker<ChunkMetadata>()
                .RuleFor(c => c.Id, f => Guid.NewGuid())
                .RuleFor(c => c.FileMetadataId, _ => fileId)
                .RuleFor(c => c.ChunkSize, f => f.Random.Long(512 * 1024, 2 * 1024 * 1024))
                .RuleFor(c => c.Order, f => f.IndexFaker + 1)
                .RuleFor(c => c.StorageProviderType, f => ProviderType)
                .RuleFor(c => c.CreatedAt, _ => createdAt)
                .RuleFor(c => c.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(c => c.CreatedBy, _ => createdBy)
                .RuleFor(c => c.UpdatedBy, _ => createdBy)
                .RuleFor(c => c.IsDeleted, f => f.Random.Bool(0.05f))
                .RuleFor(c => c.File, _ => null);

            var fakeChunk = chunkFaker.Generate();

            var fileFaker = new Faker<FileMetadata>()
                .RuleFor(f => f.Id, _ => fileId)
                .RuleFor(f => f.FileName, f => f.System.FileName())
                .RuleFor(f => f.FilePath, f => f.System.FilePath())
                .RuleFor(f => f.FileSize, f => f.Random.Long(5 * 1024 * 1024, 100 * 1024 * 1024))
                .RuleFor(f => f.Checksum, f => f.Random.Hash())
                .RuleFor(f => f.CreatedAt, _ => createdAt)
                .RuleFor(f => f.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(f => f.CreatedBy, _ => createdBy)
                .RuleFor(f => f.UpdatedBy, _ => createdBy)
                .RuleFor(f => f.IsDeleted, f => f.Random.Bool(0.1f))
                .RuleFor(f => f.Chunks, f => new List<ChunkMetadata> { fakeChunk });

            var fakeFile = fileFaker.Generate();

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            _dbContext.FileMetadata.Add(fakeFile);
            await _dbContext.SaveChangesAsync();

            _mockStorageProvider.Setup(x => x.ReadChunkAsync(chunkId.ToString()))
                .ReturnsAsync(chunkData);

            // Act
            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );


            var result = await fileProcessor.RestoreFile(fileId, _testOutputDirectory);

            // Assert

            _mockLogger.Verify(
               log => log.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Checksum MISMATCH. Original")),
                   null,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Fact]
        public async Task RestoreFile_ShouldReturnStrinEmpty_WhenProviderNotFound()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();
            var fileName = "test.txt";
            var chunkData = Encoding.UTF8.GetBytes("Hello World");
            var checksum = chunkData.ComputeChecksum();

            var createdAt = DateTime.UtcNow.AddDays(-1);
            var createdBy = "faker.user";

            var chunkFaker = new Faker<ChunkMetadata>()
                .RuleFor(c => c.Id, f => Guid.NewGuid())
                .RuleFor(c => c.FileMetadataId, _ => fileId)
                .RuleFor(c => c.ChunkSize, f => f.Random.Long(512 * 1024, 2 * 1024 * 1024))
                .RuleFor(c => c.Order, f => f.IndexFaker + 1)
                .RuleFor(c => c.StorageProviderType, "MissingProvider")
                .RuleFor(c => c.CreatedAt, _ => createdAt)
                .RuleFor(c => c.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(c => c.CreatedBy, _ => createdBy)
                .RuleFor(c => c.UpdatedBy, _ => createdBy)
                .RuleFor(c => c.IsDeleted, f => f.Random.Bool(0.05f))
                .RuleFor(c => c.File, _ => null);

            var fakeChunk = chunkFaker.Generate();

            var fileFaker = new Faker<FileMetadata>()
                .RuleFor(f => f.Id, _ => fileId)
                .RuleFor(f => f.FileName, f => f.System.FileName())
                .RuleFor(f => f.FilePath, f => f.System.FilePath())
                .RuleFor(f => f.FileSize, f => f.Random.Long(5 * 1024 * 1024, 100 * 1024 * 1024))
                .RuleFor(f => f.Checksum, f => f.Random.Hash())
                .RuleFor(f => f.CreatedAt, _ => createdAt)
                .RuleFor(f => f.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(f => f.CreatedBy, _ => createdBy)
                .RuleFor(f => f.UpdatedBy, _ => createdBy)
                .RuleFor(f => f.IsDeleted, f => f.Random.Bool(0.1f))
                .RuleFor(f => f.Chunks, f => new List<ChunkMetadata> { fakeChunk });

            var fakeFile = fileFaker.Generate();

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            _dbContext.FileMetadata.Add(fakeFile);
            await _dbContext.SaveChangesAsync();

            _mockStorageProvider.Setup(x => x.ReadChunkAsync(chunkId.ToString()))
                .ReturnsAsync(chunkData);

            // Act
            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );


            var result = await fileProcessor.RestoreFile(fileId, _testOutputDirectory);

            // Assert

            Assert.Empty(result);
            _mockLogger.Verify(
               log => log.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Storage provider")),
                   null,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Fact]
        public async Task RestoreFile_ShouldDeleteFile_WhenProviderNotExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();
            var fileName = "test.txt";
            var chunkData = Encoding.UTF8.GetBytes("Hello World");
            var checksum = chunkData.ComputeChecksum();

            var createdAt = DateTime.UtcNow.AddDays(-1);
            var createdBy = "faker.user";

            var chunkFaker = new Faker<ChunkMetadata>()
                .RuleFor(c => c.Id, f => Guid.NewGuid())
                .RuleFor(c => c.FileMetadataId, _ => fileId)
                .RuleFor(c => c.ChunkSize, f => f.Random.Long(512 * 1024, 2 * 1024 * 1024))
                .RuleFor(c => c.Order, f => f.IndexFaker + 1)
                .RuleFor(c => c.StorageProviderType, "MissingProvider")
                .RuleFor(c => c.CreatedAt, _ => createdAt)
                .RuleFor(c => c.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(c => c.CreatedBy, _ => createdBy)
                .RuleFor(c => c.UpdatedBy, _ => createdBy)
                .RuleFor(c => c.IsDeleted, f => f.Random.Bool(0.05f))
                .RuleFor(c => c.File, _ => null);

            var fakeChunk = chunkFaker.Generate();

            var fileFaker = new Faker<FileMetadata>()
                .RuleFor(f => f.Id, _ => fileId)
                .RuleFor(f => f.FileName, f => f.System.FileName())
                .RuleFor(f => f.FilePath, f => f.System.FilePath())
                .RuleFor(f => f.FileSize, f => f.Random.Long(5 * 1024 * 1024, 100 * 1024 * 1024))
                .RuleFor(f => f.Checksum, f => f.Random.Hash())
                .RuleFor(f => f.CreatedAt, _ => createdAt)
                .RuleFor(f => f.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(f => f.CreatedBy, _ => createdBy)
                .RuleFor(f => f.UpdatedBy, _ => createdBy)
                .RuleFor(f => f.IsDeleted, f => f.Random.Bool(0.1f))
                .RuleFor(f => f.Chunks, f => new List<ChunkMetadata> { fakeChunk });

            var fakeFile = fileFaker.Generate();

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            _dbContext.FileMetadata.Add(fakeFile);
            await _dbContext.SaveChangesAsync();

            _mockStorageProvider.Setup(x => x.ReadChunkAsync(chunkId.ToString()))
                .ReturnsAsync(chunkData);

            // Act
            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );


            var result = await fileProcessor.RestoreFile(fileId, _testOutputDirectory);

            // Assert

            var expectedFilePath = Path.Combine(_testOutputDirectory, $"Restored_{fileName}");
            Assert.False(File.Exists(expectedFilePath));
        }

        [Fact]
        public async Task RestoreFile_CreatesOutputDirectory_IfNotExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();
            var fileName = "test.txt";
            var chunkData = Encoding.UTF8.GetBytes("Hello World");
            var checksum = chunkData.ComputeChecksum();

            var createdAt = DateTime.UtcNow.AddDays(-1);
            var createdBy = "faker.user";

            var chunkFaker = new Faker<ChunkMetadata>()
                .RuleFor(c => c.Id, f => Guid.NewGuid())
                .RuleFor(c => c.FileMetadataId, _ => fileId)
                .RuleFor(c => c.ChunkSize, f => f.Random.Long(512 * 1024, 2 * 1024 * 1024))
                .RuleFor(c => c.Order, f => f.IndexFaker + 1)
                .RuleFor(c => c.StorageProviderType, ProviderType)
                .RuleFor(c => c.CreatedAt, _ => createdAt)
                .RuleFor(c => c.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(c => c.CreatedBy, _ => createdBy)
                .RuleFor(c => c.UpdatedBy, _ => createdBy)
                .RuleFor(c => c.IsDeleted, f => f.Random.Bool(0.05f))
                .RuleFor(c => c.File, _ => null);

            var fakeChunk = chunkFaker.Generate();

            var fileFaker = new Faker<FileMetadata>()
                .RuleFor(f => f.Id, _ => fileId)
                .RuleFor(f => f.FileName, f => f.System.FileName())
                .RuleFor(f => f.FilePath, f => f.System.FilePath())
                .RuleFor(f => f.FileSize, f => f.Random.Long(5 * 1024 * 1024, 100 * 1024 * 1024))
                .RuleFor(f => f.Checksum, f => f.Random.Hash())
                .RuleFor(f => f.CreatedAt, _ => createdAt)
                .RuleFor(f => f.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(f => f.CreatedBy, _ => createdBy)
                .RuleFor(f => f.UpdatedBy, _ => createdBy)
                .RuleFor(f => f.IsDeleted, f => f.Random.Bool(0.1f))
                .RuleFor(f => f.Chunks, f => new List<ChunkMetadata> { fakeChunk });

            var fakeFile = fileFaker.Generate();

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            _dbContext.FileMetadata.Add(fakeFile);
            await _dbContext.SaveChangesAsync();

            _mockStorageProvider.Setup(x => x.ReadChunkAsync(chunkId.ToString()))
                .ReturnsAsync(chunkData);

            // Act
            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );

            var nonExistentDirectory = Path.Combine(_testOutputDirectory, "SubDirectory");

            var result = await fileProcessor.RestoreFile(fileId, nonExistentDirectory);

            // Assert
            Assert.True(Directory.Exists(nonExistentDirectory));
            Assert.True(File.Exists(result));
        }

        [Fact]
        public async Task RestoreFile_ReturnEmptyString_WhenChunksNotExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();
            var fileName = "test.txt";
            var chunkData = Encoding.UTF8.GetBytes("Hello World");
            var checksum = chunkData.ComputeChecksum();

            var createdAt = DateTime.UtcNow.AddDays(-1);
            var createdBy = "faker.user";

            var fileFaker = new Faker<FileMetadata>()
                .RuleFor(f => f.Id, _ => fileId)
                .RuleFor(f => f.FileName, f => f.System.FileName())
                .RuleFor(f => f.FilePath, f => f.System.FilePath())
                .RuleFor(f => f.FileSize, f => f.Random.Long(5 * 1024 * 1024, 100 * 1024 * 1024))
                .RuleFor(f => f.Checksum, f => f.Random.Hash())
                .RuleFor(f => f.CreatedAt, _ => createdAt)
                .RuleFor(f => f.UpdatedAt, _ => createdAt.AddMinutes(10))
                .RuleFor(f => f.CreatedBy, _ => createdBy)
                .RuleFor(f => f.UpdatedBy, _ => createdBy)
                .RuleFor(f => f.IsDeleted, f => f.Random.Bool(0.1f));

            var fakeFile = fileFaker.Generate();

            var options = new DbContextOptionsBuilder<FileStorageDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

            _dbContext = new FileStorageDbContext(options);

            _dbContext.FileMetadata.Add(fakeFile);
            await _dbContext.SaveChangesAsync();

            _mockStorageProvider.Setup(x => x.ReadChunkAsync(chunkId.ToString()))
                .ReturnsAsync(chunkData);

            // Act
            var fileProcessor = new FileProcessor(
                _mockLogger.Object,
                _dbContext,
                _storageProviders,
                _mockChunkingService.Object
            );


            var result = await fileProcessor.RestoreFile(fileId, _testOutputDirectory);

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
               log => log.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File with chunks mot exists")),
                   null,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }


        public void Dispose()
        {
            if (Directory.Exists(_tempFolderPath))
            {
                Directory.Delete(_tempFolderPath, true);
            }

            if (Directory.Exists(_tempFilePath))
            {
                Directory.Delete(_tempFilePath, true);
            }

            if (Directory.Exists(_testOutputDirectory))
            {
                Directory.Delete(_testOutputDirectory, true);
            }
        }
    }
}
