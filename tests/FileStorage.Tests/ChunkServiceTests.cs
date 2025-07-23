using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileStorage.ConsoleApp.Constants;
using FileStorage.ConsoleApp.Services;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace FileStorage.Tests
{
    public class ChunkServiceTests
    {
        private readonly ChunkingService _chunkingService;
        private const int MinChunkSize = 20 * 1024; 
        private const int MaxChunkSize = 200 * 1024;

        public ChunkServiceTests()
        {
            _chunkingService = new ChunkingService();
        }

        [Theory]
        [InlineData(0)]                                 
        [InlineData(AppConstants.DefaultMinSizeInMb - 1)] 
        [InlineData(AppConstants.DefaultMinSizeInMb)]
        public void CalculateChunkSize_ShouldReturnMinChunkSize_ForSmallFiles(long fileSize)
        {
            // Arrange

            // Act
            var actualChunkSize = _chunkingService.CalculateChunkSize(fileSize);

            // Assert
            Assert.Equal(MinChunkSize, actualChunkSize);
        }

        [Theory]
        [InlineData(AppConstants.DefaultMaxSizeInMb)]    
        [InlineData(AppConstants.DefaultMaxSizeInMb + 1)]
        public void CalculateChunkSize_ShouldReturnMaxChunkSize_ForLargeFiles(long fileSize)
        {
            // Arrange (Parametre, hazırlık aşamasını oluşturur)

            // Act
            var actualChunkSize = _chunkingService.CalculateChunkSize(fileSize);

            // Assert
            Assert.Equal(MaxChunkSize, actualChunkSize);
        }

        [Fact]
        public void CalculateChunkSize_ShouldReturnInterpolatedSize_WhenFileIsInDynamicRange()
        {
            // Arrange
            long fileSize = (AppConstants.DefaultMinSizeInMb + AppConstants.DefaultMaxSizeInMb) / 2;

            var ratio = (double)fileSize / (AppConstants.DefaultMaxSizeInMb);
            var expectedChunkSize = (int)(MinChunkSize + (MaxChunkSize - MinChunkSize) * ratio);

            // Act
            var chunkSize = _chunkingService.CalculateChunkSize(fileSize);

            // Assert
            Assert.Equal(expectedChunkSize, chunkSize);
            Assert.True(chunkSize > MinChunkSize);
            Assert.True(chunkSize < MaxChunkSize);
        }

        [Fact]
        public async Task ChunkFile_ShouldReturnEmptyList_WhenStreamIsEmpty()
        {
            // Arrange
            var emptyStream = new MemoryStream();

            // Act
            var chunks = await _chunkingService.ChunkFile(emptyStream, 0);

            // Assert
            Assert.NotNull(chunks);
            Assert.Empty(chunks);
        }

        [Fact]
        public async Task ChunkFile_ShouldReturnSingleChunk_WhenFileIsSmallerThanMinChunkSize()
        {
            // Arrange
            var fileContent = new byte[10 * 1024];
            new Random().NextBytes(fileContent);
            var stream = new MemoryStream(fileContent);

            // Act
            var chunks = await _chunkingService.ChunkFile(stream, fileContent.Length);

            // Assert
            Assert.Single(chunks);
            Assert.Equal(fileContent.Length, chunks[0].Length);
            Assert.Equal(fileContent, chunks[0]);
        }

        [Fact]
        public async Task ChunkFile_ShouldReturnMultipleChunks_WhenFileIsLargerThanChunkSize()
        {
            // Arrange
            int fileSize = (MinChunkSize * 2) + 1000;
            var fileContent = new byte[fileSize];
            new Random().NextBytes(fileContent);
            var stream = new MemoryStream(fileContent);

            // Act
            var chunks = await _chunkingService.ChunkFile(stream, fileContent.Length);

            // Assert
            Assert.Equal(3, chunks.Count);
            Assert.Equal(MinChunkSize, chunks[0].Length);
            Assert.Equal(MinChunkSize, chunks[1].Length);
            Assert.Equal(1000, chunks[2].Length);

            var reconstructedBytes = new List<byte>();
            reconstructedBytes.AddRange(chunks[0]);
            reconstructedBytes.AddRange(chunks[1]);
            reconstructedBytes.AddRange(chunks[2]);
            Assert.Equal(fileContent, reconstructedBytes.ToArray());
        }

        [Fact]
        public async Task ChunkFile_ShouldHandleFileWithExactMultipleOfChunkSize()
        {
            // Arrange
            int fileSize = MinChunkSize * 3;
            var fileContent = new byte[fileSize];
            new Random().NextBytes(fileContent);
            var stream = new MemoryStream(fileContent);

            // Act
            var chunks = await _chunkingService.ChunkFile(stream, fileContent.Length);

            // Assert
            Assert.Equal(3, chunks.Count);
            Assert.Equal(MinChunkSize, chunks[0].Length);
            Assert.Equal(MinChunkSize, chunks[1].Length);
            Assert.Equal(MinChunkSize, chunks[2].Length);
        }
    }
}
