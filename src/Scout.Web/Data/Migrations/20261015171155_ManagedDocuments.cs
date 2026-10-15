using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManagedDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredRelativePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationDocuments",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDocuments", x => new { x.ApplicationId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_ApplicationDocuments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDocuments",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDocuments", x => new { x.CompanyId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_CompanyDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewDocuments",
                columns: table => new
                {
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewDocuments", x => new { x.InterviewRoundId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_InterviewDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewDocuments_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionDocuments",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionDocuments", x => new { x.PositionId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_PositionDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionDocuments_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEventDocuments",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEventDocuments", x => new { x.EventId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_TimelineEventDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimelineEventDocuments_TimelineEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "TimelineEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_DocumentId",
                table: "ApplicationDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocuments_DocumentId",
                table: "CompanyDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StoredRelativePath",
                table: "Documents",
                column: "StoredRelativePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewDocuments_DocumentId",
                table: "InterviewDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionDocuments_DocumentId",
                table: "PositionDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEventDocuments_DocumentId",
                table: "TimelineEventDocuments",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDocuments");

            migrationBuilder.DropTable(
                name: "CompanyDocuments");

            migrationBuilder.DropTable(
                name: "InterviewDocuments");

            migrationBuilder.DropTable(
                name: "PositionDocuments");

            migrationBuilder.DropTable(
                name: "TimelineEventDocuments");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
