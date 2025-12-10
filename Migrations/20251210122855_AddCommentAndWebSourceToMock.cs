using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class AddCommentAndWebSourceToMock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "mocks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebSource",
                table: "mocks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "mocks");

            migrationBuilder.DropColumn(
                name: "WebSource",
                table: "mocks");
        }
    }
}
