using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentStorage.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectFolderName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add column as nullable so existing rows can survive the transition.
            migrationBuilder.AddColumn<string>(
                name: "FolderName",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: null);

            // 2. Backfill from Name, sanitizing any path separators so we don't
            //    accidentally produce sub-folder structures on object storage.
            migrationBuilder.Sql(@"
                UPDATE [Projects]
                SET [FolderName] = REPLACE(REPLACE([Name], '/', '_'), '\', '_')
                WHERE [FolderName] IS NULL;");

            // 3. Optional: replace spaces with underscores for cleaner folder naming.
            //    (Commented out — uncomment if you prefer "Test_Project_E2E" over "Test Project E2E".)
            // migrationBuilder.Sql(@"UPDATE [Projects] SET [FolderName] = REPLACE([FolderName], ' ', '_');");

            // 4. Backfill any remaining NULLs (defensive — should not happen after step 2).
            migrationBuilder.Sql(@"UPDATE [Projects] SET [FolderName] = [Id] WHERE [FolderName] IS NULL;");

            // 5. Create the column as NOT NULL.
            migrationBuilder.AlterColumn<string>(
                name: "FolderName",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // 6. Now safe to apply the unique index — every row has a distinct FolderName.
            migrationBuilder.CreateIndex(
                name: "IX_Projects_FolderName",
                table: "Projects",
                column: "FolderName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_FolderName",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FolderName",
                table: "Projects");
        }
    }
}
