using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmployerQuestionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployerQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    FollowUpQuestions = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployerQuestionTags",
                columns: table => new
                {
                    EmployerQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerQuestionTags", x => new { x.EmployerQuestionId, x.Tag });
                    table.ForeignKey(
                        name: "FK_EmployerQuestionTags_EmployerQuestions_EmployerQuestionId",
                        column: x => x.EmployerQuestionId,
                        principalTable: "EmployerQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerQuestions_IsArchived",
                table: "EmployerQuestions",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerQuestionTags_Tag",
                table: "EmployerQuestionTags",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployerQuestionTags");

            migrationBuilder.DropTable(
                name: "EmployerQuestions");
        }
    }
}
