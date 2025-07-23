using Microsoft.Extensions.Logging;

namespace FileStorage.ConsoleApp.Services;

public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;
    private const int MinChunkSize = 1024 * 1024;
    private const int MaxChunkSize = 10 * 1024 * 1024;

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
    }

    public async  Task<List<byte[]>> ChunkFile(Stream stream, long fileSize)
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
        if (fileSize <= 50 * 1024 * 1024)
        {
            return MinChunkSize;
        }

        if (fileSize >= 1024 * 1024 * 1024)
        {
            return MaxChunkSize;
        }

        var ratio = (double)fileSize / (1024 * 1024 * 1024);
        var chunkSize = (int)(MinChunkSize + (MaxChunkSize - MinChunkSize) * ratio);

        return Math.Max(MinChunkSize, Math.Min(MaxChunkSize, chunkSize));
    }
}