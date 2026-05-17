using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMangakaFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MangakaId",
                table: "Submissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SeriesId",
                table: "Submissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "Regions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "PageId",
                table: "Regions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Width",
                table: "Regions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "X",
                table: "Regions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Y",
                table: "Regions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "Manuscripts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_MangakaId",
                table: "Submissions",
                column: "MangakaId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SeriesId",
                table: "Submissions",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_PageId",
                table: "Regions",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_Manuscripts_SubmissionId",
                table: "Manuscripts",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manuscripts_Submissions_SubmissionId",
                table: "Manuscripts",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Regions_Pages_PageId",
                table: "Regions",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Series_SeriesId",
                table: "Submissions",
                column: "SeriesId",
                principalTable: "Series",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_MangakaId",
                table: "Submissions",
                column: "MangakaId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manuscripts_Submissions_SubmissionId",
                table: "Manuscripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Regions_Pages_PageId",
                table: "Regions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Series_SeriesId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_MangakaId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_MangakaId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_SeriesId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Regions_PageId",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Manuscripts_SubmissionId",
                table: "Manuscripts");

            migrationBuilder.DropColumn(
                name: "MangakaId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "X",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Manuscripts");
        }
    }
}
