using FileStorage.ConsoleApp.Constants;
using Microsoft.Extensions.Logging;

namespace FileStorage.ConsoleApp.Services;

public class ChunkingService : IChunkingService
{
    private const int MinChunkSize = 20 * 1024;
    private const int MaxChunkSize = 200 * 1024;
    
    public async Task<List<byte[]>> ChunkFile(Stream stream, long fileSize)
    {
        var allChunks = new List<byte[]>();

        var chunkSize = CalculateChunkSize(fileSize);
        var buffer = new byte[chunkSize];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var chunk = new byte[bytesRead];
            Array.Copy(buffer, 0, chunk, 0, bytesRead);

            allChunks.Add(chunk);
        }

        return allChunks;
    }

    public int CalculateChunkSize(long fileSize)
    {
        if (fileSize <= AppConstants.DefaultMinSizeInMb)
        {
            return MinChunkSize;
        }

        if (fileSize >= AppConstants.DefaultMaxSizeInMb)
        {
            return MaxChunkSize;
        }

        var ratio = (double)fileSize / (AppConstants.DefaultMaxSizeInMb);
        var chunkSize = (int)(MinChunkSize + (MaxChunkSize - MinChunkSize) * ratio);

        return Math.Max(MinChunkSize, Math.Min(MaxChunkSize, chunkSize));
    }
}