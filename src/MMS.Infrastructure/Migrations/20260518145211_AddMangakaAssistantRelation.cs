using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMangakaAssistantRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MangakaId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_MangakaId",
                table: "Users",
                column: "MangakaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_MangakaId",
                table: "Users",
                column: "MangakaId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_MangakaId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MangakaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MangakaId",
                table: "Users");
        }
    }
}
