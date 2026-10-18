using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterviewRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RecordingChoiceEnabled",
                table: "InterviewWorkspaceConfiguration",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RecordingDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    Choice = table.Column<int>(type: "INTEGER", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrivateNote = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordingDecisions", x => x.Id);
                    table.UniqueConstraint("AK_RecordingDecisions_Id_InterviewRoundId", x => new { x.Id, x.InterviewRoundId });
                    table.ForeignKey(
                        name: "FK_RecordingDecisions_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsentDecisionId = table.Column<int>(type: "INTEGER", nullable: false),
                    StorageKind = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: true),
                    WorkingRelativePath = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    RegistrationToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.Id);
                    table.CheckConstraint("CK_Recording_Source", "(StorageKind = 0 AND DocumentId IS NOT NULL AND WorkingRelativePath IS NULL) OR (StorageKind = 1 AND DocumentId IS NULL AND WorkingRelativePath IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Recordings_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recordings_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recordings_RecordingDecisions_ConsentDecisionId_InterviewRoundId",
                        columns: x => new { x.ConsentDecisionId, x.InterviewRoundId },
                        principalTable: "RecordingDecisions",
                        principalColumns: new[] { "Id", "InterviewRoundId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecordingDecisions_InterviewRoundId",
                table: "RecordingDecisions",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_ConsentDecisionId_InterviewRoundId",
                table: "Recordings",
                columns: new[] { "ConsentDecisionId", "InterviewRoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_DocumentId",
                table: "Recordings",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_InterviewRoundId",
                table: "Recordings",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_RegistrationToken",
                table: "Recordings",
                column: "RegistrationToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recordings");

            migrationBuilder.DropTable(
                name: "RecordingDecisions");

            migrationBuilder.DropColumn(
                name: "RecordingChoiceEnabled",
                table: "InterviewWorkspaceConfiguration");
        }
    }
}
