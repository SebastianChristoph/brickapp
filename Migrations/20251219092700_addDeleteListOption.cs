using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class addDeleteListOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissingItem_mocks_MockId",
                table: "MissingItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MissingItem_wantedlists_WantedListId",
                table: "MissingItem");

            migrationBuilder.AddForeignKey(
                name: "FK_MissingItem_mocks_MockId",
                table: "MissingItem",
                column: "MockId",
                principalTable: "mocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MissingItem_wantedlists_WantedListId",
                table: "MissingItem",
                column: "WantedListId",
                principalTable: "wantedlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MissingItem_mocks_MockId",
                table: "MissingItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MissingItem_wantedlists_WantedListId",
                table: "MissingItem");

            migrationBuilder.AddForeignKey(
                name: "FK_MissingItem_mocks_MockId",
                table: "MissingItem",
                column: "MockId",
                principalTable: "mocks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MissingItem_wantedlists_WantedListId",
                table: "MissingItem",
                column: "WantedListId",
                principalTable: "wantedlists",
                principalColumn: "Id");
        }
    }
}
