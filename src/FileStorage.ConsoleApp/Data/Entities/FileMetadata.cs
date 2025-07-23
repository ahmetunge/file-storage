using FileStorage.ConsoleApp.Data.Enums;

namespace FileStorage.ConsoleApp.Data.Entities;

public class FileMetadata
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required long FileSize { get; set; }
    public required string Checksum { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public required string UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public List<ChunkMetadata>? Chunks { get; set; }
}