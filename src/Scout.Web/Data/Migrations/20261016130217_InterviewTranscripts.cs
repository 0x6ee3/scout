using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterviewTranscripts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Recordings_Id_InterviewRoundId",
                table: "Recordings",
                columns: new[] { "Id", "InterviewRoundId" });

            migrationBuilder.CreateTable(
                name: "Transcripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 1000000, nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalDocumentId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordingId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentTranscriptId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmissionToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcripts", x => x.Id);
                    table.UniqueConstraint("AK_Transcripts_Id_InterviewRoundId", x => new { x.Id, x.InterviewRoundId });
                    table.CheckConstraint("CK_Transcript_Origin", "(Origin = 0 AND OriginalDocumentId IS NOT NULL AND ParentTranscriptId IS NULL) OR (Origin = 1 AND ParentTranscriptId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Transcripts_Documents_OriginalDocumentId",
                        column: x => x.OriginalDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transcripts_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transcripts_Recordings_RecordingId_InterviewRoundId",
                        columns: x => new { x.RecordingId, x.InterviewRoundId },
                        principalTable: "Recordings",
                        principalColumns: new[] { "Id", "InterviewRoundId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transcripts_Transcripts_ParentTranscriptId_InterviewRoundId",
                        columns: x => new { x.ParentTranscriptId, x.InterviewRoundId },
                        principalTable: "Transcripts",
                        principalColumns: new[] { "Id", "InterviewRoundId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_InterviewRoundId",
                table: "Transcripts",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_OriginalDocumentId",
                table: "Transcripts",
                column: "OriginalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_ParentTranscriptId_InterviewRoundId",
                table: "Transcripts",
                columns: new[] { "ParentTranscriptId", "InterviewRoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_RecordingId_InterviewRoundId",
                table: "Transcripts",
                columns: new[] { "RecordingId", "InterviewRoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_SubmissionToken",
                table: "Transcripts",
                column: "SubmissionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transcripts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Recordings_Id_InterviewRoundId",
                table: "Recordings");
        }
    }
}
