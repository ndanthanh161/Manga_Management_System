using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManuscriptAndPageVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_ChapterId",
                table: "Pages");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Pages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentVersion",
                table: "Pages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "SubmissionId",
                table: "Manuscripts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ChapterId",
                table: "Manuscripts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE m
                SET m.ChapterId = COALESCE(
                    (SELECT TOP 1 c.Id 
                     FROM Chapters c
                     INNER JOIN Submissions s ON s.SeriesId = c.SeriesId
                     WHERE s.Id = m.SubmissionId),
                    (SELECT TOP 1 Id FROM Chapters)
                )
                FROM Manuscripts m
                WHERE m.ChapterId = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentVersion",
                table: "Manuscripts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentManuscriptId",
                table: "Manuscripts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Manuscripts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ManuscriptPageRefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManuscriptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManuscriptPageRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManuscriptPageRefs_Manuscripts_ManuscriptId",
                        column: x => x.ManuscriptId,
                        principalTable: "Manuscripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManuscriptPageRefs_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                WITH CTE AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY ChapterId, PageNumber ORDER BY Id) - 1 AS NewVersion
                    FROM Pages
                )
                UPDATE p
                SET p.Version = c.NewVersion
                FROM Pages p
                INNER JOIN CTE c ON p.Id = c.Id;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ChapterId_PageNumber_Version",
                table: "Pages",
                columns: new[] { "ChapterId", "PageNumber", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manuscripts_ChapterId",
                table: "Manuscripts",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Manuscripts_ParentManuscriptId",
                table: "Manuscripts",
                column: "ParentManuscriptId");

            migrationBuilder.CreateIndex(
                name: "IX_ManuscriptPageRefs_ManuscriptId",
                table: "ManuscriptPageRefs",
                column: "ManuscriptId");

            migrationBuilder.CreateIndex(
                name: "IX_ManuscriptPageRefs_PageId",
                table: "ManuscriptPageRefs",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manuscripts_Chapters_ChapterId",
                table: "Manuscripts",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Manuscripts_Manuscripts_ParentManuscriptId",
                table: "Manuscripts",
                column: "ParentManuscriptId",
                principalTable: "Manuscripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manuscripts_Chapters_ChapterId",
                table: "Manuscripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Manuscripts_Manuscripts_ParentManuscriptId",
                table: "Manuscripts");

            migrationBuilder.DropTable(
                name: "ManuscriptPageRefs");

            migrationBuilder.DropIndex(
                name: "IX_Pages_ChapterId_PageNumber_Version",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Manuscripts_ChapterId",
                table: "Manuscripts");

            migrationBuilder.DropIndex(
                name: "IX_Manuscripts_ParentManuscriptId",
                table: "Manuscripts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "IsCurrentVersion",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Manuscripts");

            migrationBuilder.DropColumn(
                name: "IsCurrentVersion",
                table: "Manuscripts");

            migrationBuilder.DropColumn(
                name: "ParentManuscriptId",
                table: "Manuscripts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Manuscripts");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubmissionId",
                table: "Manuscripts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ChapterId",
                table: "Pages",
                column: "ChapterId");
        }
    }
}
