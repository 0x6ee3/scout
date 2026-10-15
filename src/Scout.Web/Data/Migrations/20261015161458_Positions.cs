using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Positions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OriginalVacancyUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    JobDescriptionSnapshot = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    DescriptionCapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    RemoteWorkNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    EmploymentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PersonalNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DiscoveredOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ClosingOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CompanyId",
                table: "Positions",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
