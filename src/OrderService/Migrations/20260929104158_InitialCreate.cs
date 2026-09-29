using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Points",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Points", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    RecipientData = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Duration = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Points_DestinationPointId",
                        column: x => x.DestinationPointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Points_OriginPointId",
                        column: x => x.OriginPointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Points",
                columns: new[] { "Id", "Latitude", "Longitude", "Name" },
                values: new object[,]
                {
                    { new Guid("a1f4e7c2-9b8d-4a3f-6c5e-2b1d9a8c7e3f"), 41.561399999999999, -8.3972999999999995, "Ponto Universidade Minho" },
                    { new Guid("b2a1c9e8-d7f6-4e5d-8c9b-0a1f2e3d4c5b"), 41.444400000000002, -8.2960999999999991, "Ponto Guimarães" },
                    { new Guid("c3e2d1f4-8a9b-4c7e-5d6f-3b2a1c9e8d7f"), 41.536000000000001, -8.6250999999999998, "Ponto Barcelos" },
                    { new Guid("d8b3c9a2-5f6e-4b1a-8c2d-9e7f3a1b4c6d"), 41.5503, -8.4199999999999999, "Ponto Braga Centro" },
                    { new Guid("e5d4c3b2-a1f9-4e8d-7c6b-5a4f3e2d1c9b"), 41.407899999999998, -8.5192999999999994, "Ponto Famalicão" },
                    { new Guid("f9e8d7c6-b5a4-4f3e-2d1c-9b8a7c6e5d4f"), 41.536700000000003, -8.6277000000000008, "Ponto IPCA" }
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Key", "Value" },
                values: new object[] { "PesoMaximoKg", "30" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DestinationPointId",
                table: "Orders",
                column: "DestinationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OriginPointId",
                table: "Orders",
                column: "OriginPointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Points");
        }
    }
}
