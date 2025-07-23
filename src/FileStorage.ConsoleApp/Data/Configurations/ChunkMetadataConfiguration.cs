using FileStorage.ConsoleApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileStorage.ConsoleApp.Data.Configurations;

public class ChunkMetadataConfiguration: IEntityTypeConfiguration<ChunkMetadata>
{
    public void Configure(EntityTypeBuilder<ChunkMetadata> builder)
    {
        builder.ToTable("ChunkMetadata");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(e => e.ChunkSize)
            .IsRequired();
        
        builder.Property(e => e.StorageProviderType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Order)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.HasIndex(e => new { FileId = e.FileMetadataId });
    }
}