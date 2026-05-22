using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudinaryFieldsToPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicId",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Pages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudinaryPublicId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Pages");
        }
    }
}
