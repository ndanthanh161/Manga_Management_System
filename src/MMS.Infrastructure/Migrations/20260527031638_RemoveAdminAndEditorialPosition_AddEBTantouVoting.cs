using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdminAndEditorialPosition_AddEBTantouVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateApplications_Users_ReviewedByUserId",
                table: "CandidateApplications");

            migrationBuilder.DropColumn(
                name: "EditorialPosition",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ReviewedByUserId",
                table: "CandidateApplications",
                newName: "ApprovedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateApplications_ReviewedByUserId",
                table: "CandidateApplications",
                newName: "IX_CandidateApplications_ApprovedByUserId");

            migrationBuilder.CreateTable(
                name: "ApplicationVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoardMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProposedTantouId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationVotes_CandidateApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "CandidateApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationVotes_Users_BoardMemberId",
                        column: x => x.BoardMemberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeriesVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoardMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProposedTantouId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesVotes_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesVotes_Users_BoardMemberId",
                        column: x => x.BoardMemberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVotes_ApplicationId_BoardMemberId",
                table: "ApplicationVotes",
                columns: new[] { "ApplicationId", "BoardMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVotes_BoardMemberId",
                table: "ApplicationVotes",
                column: "BoardMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesVotes_BoardMemberId",
                table: "SeriesVotes",
                column: "BoardMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesVotes_SeriesId_BoardMemberId",
                table: "SeriesVotes",
                columns: new[] { "SeriesId", "BoardMemberId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateApplications_Users_ApprovedByUserId",
                table: "CandidateApplications",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateApplications_Users_ApprovedByUserId",
                table: "CandidateApplications");

            migrationBuilder.DropTable(
                name: "ApplicationVotes");

            migrationBuilder.DropTable(
                name: "SeriesVotes");

            migrationBuilder.RenameColumn(
                name: "ApprovedByUserId",
                table: "CandidateApplications",
                newName: "ReviewedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateApplications_ApprovedByUserId",
                table: "CandidateApplications",
                newName: "IX_CandidateApplications_ReviewedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "EditorialPosition",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateApplications_Users_ReviewedByUserId",
                table: "CandidateApplications",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
