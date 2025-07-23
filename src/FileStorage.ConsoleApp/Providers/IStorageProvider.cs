namespace FileStorage.ConsoleApp.Providers;

public interface IStorageProvider
{
    string ProviderType { get; }
    
    Task SaveChunk(string chunkId, byte[] data);
    
    Task<byte[]> ReadChunkAsync(string chunkId);
}