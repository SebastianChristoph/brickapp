using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class AddWantedLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wantedlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AppUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wantedlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wantedlistitems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WantedListId = table.Column<int>(type: "integer", nullable: false),
                    MappedBrickId = table.Column<int>(type: "integer", nullable: false),
                    BrickColorId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wantedlistitems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wantedlistitems_colors_BrickColorId",
                        column: x => x.BrickColorId,
                        principalTable: "colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wantedlistitems_mappedBricks_MappedBrickId",
                        column: x => x.MappedBrickId,
                        principalTable: "mappedBricks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wantedlistitems_wantedlists_WantedListId",
                        column: x => x.WantedListId,
                        principalTable: "wantedlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wantedlistitems_BrickColorId",
                table: "wantedlistitems",
                column: "BrickColorId");

            migrationBuilder.CreateIndex(
                name: "IX_wantedlistitems_MappedBrickId",
                table: "wantedlistitems",
                column: "MappedBrickId");

            migrationBuilder.CreateIndex(
                name: "IX_wantedlistitems_WantedListId",
                table: "wantedlistitems",
                column: "WantedListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wantedlistitems");

            migrationBuilder.DropTable(
                name: "wantedlists");
        }
    }
}
