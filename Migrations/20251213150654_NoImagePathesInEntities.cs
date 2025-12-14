using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class NoImagePathesInEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "newSetRequests");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "newItemRequests");

            migrationBuilder.DropColumn(
                name: "MockImagePath",
                table: "mocks");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "mappedBricks");

            migrationBuilder.AddColumn<string>(
                name: "PartNum",
                table: "newItemRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Uuid",
                table: "newItemRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Uuid",
                table: "mappedBricks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartNum",
                table: "newItemRequests");

            migrationBuilder.DropColumn(
                name: "Uuid",
                table: "newItemRequests");

            migrationBuilder.DropColumn(
                name: "Uuid",
                table: "mappedBricks");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "newSetRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "newItemRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MockImagePath",
                table: "mocks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "mappedBricks",
                type: "TEXT",
                nullable: true);
        }
    }
}
