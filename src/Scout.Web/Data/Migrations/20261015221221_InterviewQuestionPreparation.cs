using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterviewQuestionPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployerQuestionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    FollowUpQuestions = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    PreparationNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestions_EmployerQuestions_EmployerQuestionId",
                        column: x => x.EmployerQuestionId,
                        principalTable: "EmployerQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InterviewQuestions_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_EmployerQuestionId",
                table: "InterviewQuestions",
                column: "EmployerQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_InterviewRoundId_EmployerQuestionId",
                table: "InterviewQuestions",
                columns: new[] { "InterviewRoundId", "EmployerQuestionId" },
                unique: true,
                filter: "\"EmployerQuestionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_InterviewRoundId_IsArchived_DisplayOrder",
                table: "InterviewQuestions",
                columns: new[] { "InterviewRoundId", "IsArchived", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewQuestions");
        }
    }
}
