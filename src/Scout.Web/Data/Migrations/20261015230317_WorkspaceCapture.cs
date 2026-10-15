using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotesRevision",
                table: "Interviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContentRevision",
                table: "InterviewQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreationToken",
                table: "InterviewQuestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpRequired",
                table: "InterviewQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAsked",
                table: "InterviewQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "InterviewQuestions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Library");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_InterviewRoundId_CreationToken",
                table: "InterviewQuestions",
                columns: new[] { "InterviewRoundId", "CreationToken" },
                unique: true,
                filter: "CreationToken IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Question_Origin",
                table: "InterviewQuestions",
                sql: "Origin IN ('Library','AdHoc') AND (Origin != 'AdHoc' OR EmployerQuestionId IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestions_InterviewRoundId_CreationToken",
                table: "InterviewQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Question_Origin",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "NotesRevision",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "ContentRevision",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "CreationToken",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "FollowUpRequired",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "IsAsked",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "InterviewQuestions");
        }
    }
}
