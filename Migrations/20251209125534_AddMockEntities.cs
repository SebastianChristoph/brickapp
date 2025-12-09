using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace brickisbrickapp.Migrations
{
    /// <inheritdoc />
    public partial class AddMockEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UserUuid = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mocks_users_UserUuid",
                        column: x => x.UserUuid,
                        principalTable: "users",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mockitems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MockId = table.Column<int>(type: "INTEGER", nullable: false),
                    MappedBrickId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrickColorId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExternalPartNum = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mockitems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mockitems_colors_BrickColorId",
                        column: x => x.BrickColorId,
                        principalTable: "colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mockitems_mappedBricks_MappedBrickId",
                        column: x => x.MappedBrickId,
                        principalTable: "mappedBricks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mockitems_mocks_MockId",
                        column: x => x.MockId,
                        principalTable: "mocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mockitems_BrickColorId",
                table: "mockitems",
                column: "BrickColorId");

            migrationBuilder.CreateIndex(
                name: "IX_mockitems_MappedBrickId",
                table: "mockitems",
                column: "MappedBrickId");

            migrationBuilder.CreateIndex(
                name: "IX_mockitems_MockId",
                table: "mockitems",
                column: "MockId");

            migrationBuilder.CreateIndex(
                name: "IX_mocks_UserUuid",
                table: "mocks",
                column: "UserUuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mockitems");

            migrationBuilder.DropTable(
                name: "mocks");
        }
    }
}
