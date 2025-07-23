using FileStorage.ConsoleApp.Models;

namespace FileStorage.ConsoleApp.Services;

public interface IChunkingService
{
    Task<List<byte[]>> ChunkFile(Stream stream, long fileSize);
    int CalculateChunkSize(long fileSize);
}