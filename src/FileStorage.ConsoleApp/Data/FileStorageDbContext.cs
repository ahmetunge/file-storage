using FileStorage.ConsoleApp.Data.Configurations;
using FileStorage.ConsoleApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileStorage.ConsoleApp.Data;

public class FileStorageDbContext: DbContext
{
    public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new ChunkMetadataConfiguration());
        builder.ApplyConfiguration(new FileMetadataConfiguration());
    }
    
    public DbSet<ChunkMetadata> ChunkMetadata { get; set; }
    public DbSet<FileMetadata> FileMetadata { get; set; }
}