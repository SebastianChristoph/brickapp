using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace brickapp.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "colors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RebrickableColorId = table.Column<int>(type: "integer", nullable: false),
                    Rgb = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "itemsets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    SetNum = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemsets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mappedBricks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HasAtLeastOneMapping = table.Column<bool>(type: "boolean", nullable: false),
                    Uuid = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LegoPartNum = table.Column<string>(type: "text", nullable: true),
                    LegoName = table.Column<string>(type: "text", nullable: true),
                    BluebrixxPartNum = table.Column<string>(type: "text", nullable: true),
                    BluebrixxName = table.Column<string>(type: "text", nullable: true),
                    CadaPartNum = table.Column<string>(type: "text", nullable: true),
                    CadaName = table.Column<string>(type: "text", nullable: true),
                    PantasyPartNum = table.Column<string>(type: "text", nullable: true),
                    PantasyName = table.Column<string>(type: "text", nullable: true),
                    MouldKingPartNum = table.Column<string>(type: "text", nullable: true),
                    MouldKingName = table.Column<string>(type: "text", nullable: true),
                    UnknownPartNum = table.Column<string>(type: "text", nullable: true),
                    UnknownName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mappedBricks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "newSetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    SetNo = table.Column<string>(type: "text", nullable: false),
                    SetName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReasonRejected = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newSetRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.UniqueConstraint("AK_users_Uuid", x => x.Uuid);
                });

            migrationBuilder.CreateTable(
                name: "itemsetbricks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemSetId = table.Column<int>(type: "integer", nullable: false),
                    MappedBrickId = table.Column<int>(type: "integer", nullable: false),
                    BrickColorId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemsetbricks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_itemsetbricks_colors_BrickColorId",
                        column: x => x.BrickColorId,
                        principalTable: "colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itemsetbricks_itemsets_ItemSetId",
                        column: x => x.ItemSetId,
                        principalTable: "itemsets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itemsetbricks_mappedBricks_MappedBrickId",
                        column: x => x.MappedBrickId,
                        principalTable: "mappedBricks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "newSetRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NewSetRequestId = table.Column<int>(type: "integer", nullable: false),
                    ItemIdOrName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newSetRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_newSetRequestItems_newSetRequests_NewSetRequestId",
                        column: x => x.NewSetRequestId,
                        principalTable: "newSetRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MappedBrickId = table.Column<int>(type: "integer", nullable: false),
                    BrickColorId = table.Column<int>(type: "integer", nullable: false),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_colors_BrickColorId",
                        column: x => x.BrickColorId,
                        principalTable: "colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_mappedBricks_MappedBrickId",
                        column: x => x.MappedBrickId,
                        principalTable: "mappedBricks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mappingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrickId = table.Column<int>(type: "integer", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    MappingName = table.Column<string>(type: "text", nullable: false),
                    MappingItemId = table.Column<string>(type: "text", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReasonRejected = table.Column<string>(type: "text", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mappingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mappingRequests_mappedBricks_BrickId",
                        column: x => x.BrickId,
                        principalTable: "mappedBricks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mappingRequests_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mappingRequests_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    WebSource = table.Column<string>(type: "text", nullable: true),
                    MockType = table.Column<string>(type: "text", nullable: false),
                    UserUuid = table.Column<string>(type: "text", nullable: false)
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
                name: "newItemRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PartNum = table.Column<string>(type: "text", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReasonRejected = table.Column<string>(type: "text", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newItemRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_newItemRequests_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_newItemRequests_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "useritemsets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    ItemSetId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UserItemSetId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_useritemsets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_useritemsets_itemsets_ItemSetId",
                        column: x => x.ItemSetId,
                        principalTable: "itemsets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_useritemsets_useritemsets_UserItemSetId",
                        column: x => x.UserItemSetId,
                        principalTable: "useritemsets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_useritemsets_users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserUuid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_userNotifications_users_UserUuid",
                        column: x => x.UserUuid,
                        principalTable: "users",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mockitems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MockId = table.Column<int>(type: "integer", nullable: false),
                    MappedBrickId = table.Column<int>(type: "integer", nullable: true),
                    BrickColorId = table.Column<int>(type: "integer", nullable: true),
                    ExternalPartNum = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "IsAdmin", "Name", "Uuid" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Admin", "111" },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Uwe", "222" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_AppUserId",
                table: "inventory",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_BrickColorId",
                table: "inventory",
                column: "BrickColorId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_MappedBrickId",
                table: "inventory",
                column: "MappedBrickId");

            migrationBuilder.CreateIndex(
                name: "IX_itemsetbricks_BrickColorId",
                table: "itemsetbricks",
                column: "BrickColorId");

            migrationBuilder.CreateIndex(
                name: "IX_itemsetbricks_ItemSetId",
                table: "itemsetbricks",
                column: "ItemSetId");

            migrationBuilder.CreateIndex(
                name: "IX_itemsetbricks_MappedBrickId",
                table: "itemsetbricks",
                column: "MappedBrickId");

            migrationBuilder.CreateIndex(
                name: "IX_mappingRequests_ApprovedByUserId",
                table: "mappingRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_mappingRequests_BrickId",
                table: "mappingRequests",
                column: "BrickId");

            migrationBuilder.CreateIndex(
                name: "IX_mappingRequests_RequestedByUserId",
                table: "mappingRequests",
                column: "RequestedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_newItemRequests_ApprovedByUserId",
                table: "newItemRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_newItemRequests_RequestedByUserId",
                table: "newItemRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_newSetRequestItems_NewSetRequestId",
                table: "newSetRequestItems",
                column: "NewSetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_useritemsets_AppUserId",
                table: "useritemsets",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_useritemsets_ItemSetId",
                table: "useritemsets",
                column: "ItemSetId");

            migrationBuilder.CreateIndex(
                name: "IX_useritemsets_UserItemSetId",
                table: "useritemsets",
                column: "UserItemSetId");

            migrationBuilder.CreateIndex(
                name: "IX_userNotifications_UserUuid",
                table: "userNotifications",
                column: "UserUuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory");

            migrationBuilder.DropTable(
                name: "itemsetbricks");

            migrationBuilder.DropTable(
                name: "mappingRequests");

            migrationBuilder.DropTable(
                name: "mockitems");

            migrationBuilder.DropTable(
                name: "newItemRequests");

            migrationBuilder.DropTable(
                name: "newSetRequestItems");

            migrationBuilder.DropTable(
                name: "useritemsets");

            migrationBuilder.DropTable(
                name: "userNotifications");

            migrationBuilder.DropTable(
                name: "colors");

            migrationBuilder.DropTable(
                name: "mappedBricks");

            migrationBuilder.DropTable(
                name: "mocks");

            migrationBuilder.DropTable(
                name: "newSetRequests");

            migrationBuilder.DropTable(
                name: "itemsets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
