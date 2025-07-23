using FileStorage.ConsoleApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileStorage.ConsoleApp.Data.Configurations;

public class FileMetadataConfiguration: IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.ToTable("FileMetadata");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FileSize)
            .IsRequired();
        
        builder.Property(e => e.Checksum)
            .IsRequired()
            .HasMaxLength(255);
        
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
    }
}