using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KitchenDisplaySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_KitchenStation_KitchenStationId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KitchenStation",
                table: "KitchenStation");

            migrationBuilder.RenameTable(
                name: "KitchenStation",
                newName: "KitchenStations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "KitchenStations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ColorHex",
                table: "KitchenStations",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "KitchenStations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitchenStations",
                table: "KitchenStations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStations_IsActive_DisplayOrder",
                table: "KitchenStations",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStations_Name",
                table: "KitchenStations",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_KitchenStations_KitchenStationId",
                table: "Products",
                column: "KitchenStationId",
                principalTable: "KitchenStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_KitchenStations_KitchenStationId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KitchenStations",
                table: "KitchenStations");

            migrationBuilder.DropIndex(
                name: "IX_KitchenStations_IsActive_DisplayOrder",
                table: "KitchenStations");

            migrationBuilder.DropIndex(
                name: "IX_KitchenStations_Name",
                table: "KitchenStations");

            migrationBuilder.RenameTable(
                name: "KitchenStations",
                newName: "KitchenStation");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "KitchenStation",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "ColorHex",
                table: "KitchenStation",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "KitchenStation",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitchenStation",
                table: "KitchenStation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_KitchenStation_KitchenStationId",
                table: "Products",
                column: "KitchenStationId",
                principalTable: "KitchenStation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
