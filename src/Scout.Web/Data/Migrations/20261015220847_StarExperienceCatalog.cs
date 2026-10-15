using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class StarExperienceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfessionalExperiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MemoryCue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Situation = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Task = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Reflection = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    UsefulFor = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalExperiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExperienceTags",
                columns: table => new
                {
                    ProfessionalExperienceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienceTags", x => new { x.ProfessionalExperienceId, x.Tag });
                    table.ForeignKey(
                        name: "FK_ExperienceTags_ProfessionalExperiences_ProfessionalExperienceId",
                        column: x => x.ProfessionalExperienceId,
                        principalTable: "ProfessionalExperiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceTags_Tag",
                table: "ExperienceTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalExperiences_IsArchived",
                table: "ProfessionalExperiences",
                column: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperienceTags");

            migrationBuilder.DropTable(
                name: "ProfessionalExperiences");
        }
    }
}
