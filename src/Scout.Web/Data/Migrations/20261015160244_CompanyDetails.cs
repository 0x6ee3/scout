using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scout.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompanyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Companies",
                type: "TEXT",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Companies",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalNotes",
                table: "Companies",
                type: "TEXT",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Companies",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PersonalNotes",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Companies");
        }
    }
}
