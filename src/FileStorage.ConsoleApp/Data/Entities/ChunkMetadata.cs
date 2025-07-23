namespace FileStorage.ConsoleApp.Data.Entities;

public class ChunkMetadata
{
    public Guid Id { get; set; }
    public Guid FileMetadataId { get; set; }
    public required long ChunkSize { get; set; }

    public required int Order { get; set; }
    public required string StorageProviderType { get; set; } 
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public required string UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public FileMetadata? File { get; set; }
}