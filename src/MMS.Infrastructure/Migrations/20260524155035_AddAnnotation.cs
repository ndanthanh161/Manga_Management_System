using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Annotations_Users_UserId",
                table: "Annotations");

            migrationBuilder.DropIndex(
                name: "IX_Annotations_PageId",
                table: "Annotations");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Annotations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Annotations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Annotations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                table: "Annotations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "Annotations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Resolved",
                table: "Annotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Annotations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedByUserId",
                table: "Annotations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "Annotations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Annotations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrokeData",
                table: "Annotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Width",
                table: "Annotations",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_ClosedByUserId",
                table: "Annotations",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_PageId_Status",
                table: "Annotations",
                columns: new[] { "PageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_ResolvedByUserId",
                table: "Annotations",
                column: "ResolvedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_Users_ClosedByUserId",
                table: "Annotations",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_Users_ResolvedByUserId",
                table: "Annotations",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_Users_UserId",
                table: "Annotations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Annotations_Users_ClosedByUserId",
                table: "Annotations");

            migrationBuilder.DropForeignKey(
                name: "FK_Annotations_Users_ResolvedByUserId",
                table: "Annotations");

            migrationBuilder.DropForeignKey(
                name: "FK_Annotations_Users_UserId",
                table: "Annotations");

            migrationBuilder.DropIndex(
                name: "IX_Annotations_ClosedByUserId",
                table: "Annotations");

            migrationBuilder.DropIndex(
                name: "IX_Annotations_PageId_Status",
                table: "Annotations");

            migrationBuilder.DropIndex(
                name: "IX_Annotations_ResolvedByUserId",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "Resolved",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "StrokeData",
                table: "Annotations");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Annotations");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Annotations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_PageId",
                table: "Annotations",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_Users_UserId",
                table: "Annotations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
