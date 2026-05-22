using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedTantouId",
                table: "Submissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Submissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubmissionTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionTransitions_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionTransitions_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedTantouId",
                table: "Submissions",
                column: "AssignedTantouId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTransitions_ActorUserId",
                table: "SubmissionTransitions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTransitions_SubmissionId_OccurredAt",
                table: "SubmissionTransitions",
                columns: new[] { "SubmissionId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_AssignedTantouId",
                table: "Submissions",
                column: "AssignedTantouId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_AssignedTantouId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "SubmissionTransitions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignedTantouId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AssignedTantouId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Submissions");
        }
    }
}
