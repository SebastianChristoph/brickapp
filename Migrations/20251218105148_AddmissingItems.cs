using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class AddmissingItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissingItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalPartNum = table.Column<string>(type: "text", nullable: true),
                    ExternalColorId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    MockId = table.Column<int>(type: "integer", nullable: true),
                    WantedListId = table.Column<int>(type: "integer", nullable: true),
                    WantedListItemId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissingItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissingItem_mocks_MockId",
                        column: x => x.MockId,
                        principalTable: "mocks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MissingItem_wantedlistitems_WantedListItemId",
                        column: x => x.WantedListItemId,
                        principalTable: "wantedlistitems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissingItem_MockId",
                table: "MissingItem",
                column: "MockId");

            migrationBuilder.CreateIndex(
                name: "IX_MissingItem_WantedListItemId",
                table: "MissingItem",
                column: "WantedListItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissingItem");
        }
    }
}
