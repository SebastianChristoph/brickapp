using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class addSourceToWantedList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "wantedlists",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "wantedlists");
        }
    }
}
