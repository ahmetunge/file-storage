namespace FileStorage.ConsoleApp.Models;

public class ChunkInfo
{
    public int Index { get; set; }
    public required byte[] Data { get; set; }
    public required string Checksum { get; set; }
    public long Size { get; set; }
}