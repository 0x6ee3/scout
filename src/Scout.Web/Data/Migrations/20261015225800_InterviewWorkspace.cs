using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterviewWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_InterviewQuestions_InterviewRoundId_Id",
                table: "InterviewQuestions",
                columns: new[] { "InterviewRoundId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_InterviewExperiences_InterviewRoundId_Id",
                table: "InterviewExperiences",
                columns: new[] { "InterviewRoundId", "Id" });

            migrationBuilder.CreateTable(
                name: "InterviewWorkspaceConfiguration",
                columns: table => new
                {
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigurationRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RoundTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PositionTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    JobDescription = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    PreparationNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    InterviewNotesEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmissionId = table.Column<int>(type: "INTEGER", nullable: true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewWorkspaceConfiguration", x => x.InterviewRoundId);
                    table.CheckConstraint("CK_Workspace_Cv", "(SubmissionId IS NULL AND DocumentId IS NULL) OR (SubmissionId IS NOT NULL AND DocumentId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_InterviewWorkspaceConfiguration_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewWorkspaceConfiguration_SubmissionFiles_SubmissionId_DocumentId",
                        columns: x => new { x.SubmissionId, x.DocumentId },
                        principalTable: "SubmissionFiles",
                        principalColumns: new[] { "SubmissionId", "DocumentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceExperience",
                columns: table => new
                {
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterviewExperienceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MemoryCue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Situation = table.Column<string>(type: "TEXT", nullable: true),
                    Task = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", nullable: true),
                    Reflection = table.Column<string>(type: "TEXT", nullable: true),
                    UsefulFor = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    PreparationNotes = table.Column<string>(type: "TEXT", nullable: true),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceExperience", x => new { x.InterviewRoundId, x.InterviewExperienceId });
                    table.ForeignKey(
                        name: "FK_WorkspaceExperience_InterviewExperiences_InterviewRoundId_InterviewExperienceId",
                        columns: x => new { x.InterviewRoundId, x.InterviewExperienceId },
                        principalTable: "InterviewExperiences",
                        principalColumns: new[] { "InterviewRoundId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceExperience_InterviewWorkspaceConfiguration_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "InterviewWorkspaceConfiguration",
                        principalColumn: "InterviewRoundId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkspacePriorAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspacePriorAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspacePriorAnswer_InterviewQuestions_SourceQuestionId",
                        column: x => x.SourceQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspacePriorAnswer_InterviewWorkspaceConfiguration_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "InterviewWorkspaceConfiguration",
                        principalColumn: "InterviewRoundId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceQuestion",
                columns: table => new
                {
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterviewQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Intent = table.Column<string>(type: "TEXT", nullable: true),
                    FollowUpQuestions = table.Column<string>(type: "TEXT", nullable: true),
                    PreparationNotes = table.Column<string>(type: "TEXT", nullable: true),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AnswerEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceQuestion", x => new { x.InterviewRoundId, x.InterviewQuestionId });
                    table.ForeignKey(
                        name: "FK_WorkspaceQuestion_InterviewQuestions_InterviewRoundId_InterviewQuestionId",
                        columns: x => new { x.InterviewRoundId, x.InterviewQuestionId },
                        principalTable: "InterviewQuestions",
                        principalColumns: new[] { "InterviewRoundId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceQuestion_InterviewWorkspaceConfiguration_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "InterviewWorkspaceConfiguration",
                        principalColumn: "InterviewRoundId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewWorkspaceConfiguration_SubmissionId_DocumentId",
                table: "InterviewWorkspaceConfiguration",
                columns: new[] { "SubmissionId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewWorkspaceConfiguration_WorkspaceId",
                table: "InterviewWorkspaceConfiguration",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspacePriorAnswer_InterviewRoundId",
                table: "WorkspacePriorAnswer",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspacePriorAnswer_SourceQuestionId",
                table: "WorkspacePriorAnswer",
                column: "SourceQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkspaceExperience");

            migrationBuilder.DropTable(
                name: "WorkspacePriorAnswer");

            migrationBuilder.DropTable(
                name: "WorkspaceQuestion");

            migrationBuilder.DropTable(
                name: "InterviewWorkspaceConfiguration");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_InterviewQuestions_InterviewRoundId_Id",
                table: "InterviewQuestions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_InterviewExperiences_InterviewRoundId_Id",
                table: "InterviewExperiences");
        }
    }
}
