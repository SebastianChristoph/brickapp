using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class addNewPendingMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingReason",
                table: "newSetRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingReason",
                table: "newItemRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingReason",
                table: "mappingRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingReason",
                table: "itemImageRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingReason",
                table: "newSetRequests");

            migrationBuilder.DropColumn(
                name: "PendingReason",
                table: "newItemRequests");

            migrationBuilder.DropColumn(
                name: "PendingReason",
                table: "mappingRequests");

            migrationBuilder.DropColumn(
                name: "PendingReason",
                table: "itemImageRequests");
        }
    }
}
