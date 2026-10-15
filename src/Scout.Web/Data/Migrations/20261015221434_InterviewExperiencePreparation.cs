using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterviewExperiencePreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewExperiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfessionalExperienceId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreparationNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewExperiences_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewExperiences_ProfessionalExperiences_ProfessionalExperienceId",
                        column: x => x.ProfessionalExperienceId,
                        principalTable: "ProfessionalExperiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewExperiences_InterviewRoundId_IsArchived_DisplayOrder",
                table: "InterviewExperiences",
                columns: new[] { "InterviewRoundId", "IsArchived", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewExperiences_InterviewRoundId_ProfessionalExperienceId",
                table: "InterviewExperiences",
                columns: new[] { "InterviewRoundId", "ProfessionalExperienceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewExperiences_ProfessionalExperienceId",
                table: "InterviewExperiences",
                column: "ProfessionalExperienceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewExperiences");
        }
    }
}
