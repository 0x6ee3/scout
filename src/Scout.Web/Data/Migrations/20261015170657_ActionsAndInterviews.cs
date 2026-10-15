using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ActionsAndInterviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterviewRoundId",
                table: "TimelineEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Interviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LocationOrMeetingUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    PreparationNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    InterviewNotes = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interviews_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewParticipants",
                columns: table => new
                {
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewParticipants", x => new { x.InterviewRoundId, x.ContactId });
                    table.ForeignKey(
                        name: "FK_InterviewParticipants_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewParticipants_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NextActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: true),
                    PositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    InterviewRoundId = table.Column<int>(type: "INTEGER", nullable: true),
                    TimelineEventId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NextActions", x => x.Id);
                    table.CheckConstraint("CK_NextAction_Context", "(InterviewRoundId IS NULL OR TimelineEventId IS NULL) AND (PositionId IS NULL OR (InterviewRoundId IS NULL AND TimelineEventId IS NULL))");
                    table.CheckConstraint("CK_NextAction_Owner", "(ApplicationId IS NOT NULL AND PositionId IS NULL) OR (ApplicationId IS NULL AND PositionId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_NextActions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NextActions_Interviews_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NextActions_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NextActions_TimelineEvents_TimelineEventId",
                        column: x => x.TimelineEventId,
                        principalTable: "TimelineEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_InterviewRoundId",
                table: "TimelineEvents",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewParticipants_ContactId",
                table: "InterviewParticipants",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ApplicationId",
                table: "Interviews",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_NextActions_ApplicationId",
                table: "NextActions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_NextActions_InterviewRoundId",
                table: "NextActions",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_NextActions_PositionId",
                table: "NextActions",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_NextActions_Status_DueOn",
                table: "NextActions",
                columns: new[] { "Status", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_NextActions_TimelineEventId",
                table: "NextActions",
                column: "TimelineEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineEvents_Interviews_InterviewRoundId",
                table: "TimelineEvents",
                column: "InterviewRoundId",
                principalTable: "Interviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimelineEvents_Interviews_InterviewRoundId",
                table: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "InterviewParticipants");

            migrationBuilder.DropTable(
                name: "NextActions");

            migrationBuilder.DropTable(
                name: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_TimelineEvents_InterviewRoundId",
                table: "TimelineEvents");

            migrationBuilder.DropColumn(
                name: "InterviewRoundId",
                table: "TimelineEvents");
        }
    }
}
