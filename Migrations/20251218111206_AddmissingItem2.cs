using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class AddmissingItem2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WantedListMissingItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WantedListId = table.Column<int>(type: "integer", nullable: false),
                    ExternalPartNum = table.Column<string>(type: "text", nullable: true),
                    ExternalColorId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WantedListMissingItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WantedListMissingItem_wantedlists_WantedListId",
                        column: x => x.WantedListId,
                        principalTable: "wantedlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WantedListMissingItem_WantedListId",
                table: "WantedListMissingItem",
                column: "WantedListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WantedListMissingItem");
        }
    }
}
