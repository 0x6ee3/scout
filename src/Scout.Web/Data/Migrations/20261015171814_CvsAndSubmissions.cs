using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CvsAndSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubmissionId",
                table: "TimelineEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PreparedCvs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TechnologyFocus = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparedCvs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreparedCvs_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmittedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsIncorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    CorrectionReason = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionFiles",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    LabelSnapshot = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFiles", x => new { x.SubmissionId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_SubmissionId",
                table: "TimelineEvents",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparedCvs_DocumentId",
                table: "PreparedCvs",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_DocumentId",
                table: "SubmissionFiles",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_SubmissionId",
                table: "SubmissionFiles",
                column: "SubmissionId",
                unique: true,
                filter: "Role = 'CV'");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ApplicationId",
                table: "Submissions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_RecordToken",
                table: "Submissions",
                column: "RecordToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineEvents_Submissions_SubmissionId",
                table: "TimelineEvents",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimelineEvents_Submissions_SubmissionId",
                table: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "PreparedCvs");

            migrationBuilder.DropTable(
                name: "SubmissionFiles");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_TimelineEvents_SubmissionId",
                table: "TimelineEvents");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "TimelineEvents");
        }
    }
}
