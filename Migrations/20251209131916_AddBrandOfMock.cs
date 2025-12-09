using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace brickisbrickapp.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandOfMock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MockType",
                table: "mocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MockType",
                table: "mocks");
        }
    }
}
