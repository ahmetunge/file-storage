using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileStorage.ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChunkMetadata_FileMetadataId",
                table: "ChunkMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkMetadata_FileMetadataId",
                table: "ChunkMetadata",
                column: "FileMetadataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChunkMetadata_FileMetadataId",
                table: "ChunkMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkMetadata_FileMetadataId",
                table: "ChunkMetadata",
                column: "FileMetadataId",
                unique: true);
        }
    }
}
