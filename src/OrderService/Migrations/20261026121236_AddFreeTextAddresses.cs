using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeTextAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Points",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCatalog",
                table: "Points",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Points",
                keyColumn: "Id",
                keyValue: new Guid("a1f4e7c2-9b8d-4a3f-6c5e-2b1d9a8c7e3f"),
                columns: new[] { "Address", "IsCatalog" },
                values: new object[] { null, true });

            migrationBuilder.UpdateData(
                table: "Points",
                keyColumn: "Id",
                keyValue: new Guid("b2a1c9e8-d7f6-4e5d-8c9b-0a1f2e3d4c5b"),
                columns: new[] { "Address", "IsCatalog" },
                values: new object[] { null, true });

            migrationBuilder.UpdateData(
                table: "Points",
                keyColumn: "Id",
                keyValue: new Guid("c3e2d1f4-8a9b-4c7e-5d6f-3b2a1c9e8d7f"),
                columns: new[] { "Address", "IsCatalog" },
                values: new object[] { null, true });

            migrationBuilder.UpdateData(
                table: "Points",
                keyColumn: "Id",
                keyValue: new Guid("d8b3c9a2-5f6e-4b1a-8c2d-9e7f3a1b4c6d"),
                columns: new[] { "Address", "IsCatalog" },
                values: new object[] { null, true });

            migrationBuilder.UpdateData(
                table: "Points",
                keyColumn: "Id",
                keyValue: new Guid("e5d4c3b2-a1f9-4e8d-7c6b-5a4f3e2d1c9b"),
                columns: new[] { "Address", "IsCatalog" },
                values: new object[] { null, true });

            migrationBuilder.UpdateData(
                table: "Points",
                keyColumn: "Id",
                keyValue: new Guid("f9e8d7c6-b5a4-4f3e-2d1c-9b8a7c6e5d4f"),
                columns: new[] { "Address", "IsCatalog" },
                values: new object[] { null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Points");

            migrationBuilder.DropColumn(
                name: "IsCatalog",
                table: "Points");
        }
    }
}
