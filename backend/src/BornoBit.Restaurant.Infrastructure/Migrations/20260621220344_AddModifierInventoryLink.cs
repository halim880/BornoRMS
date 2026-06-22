using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModifierInventoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsumeQtyBase",
                table: "ProductOptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryItemId",
                table: "ProductOptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_InventoryItemId",
                table: "ProductOptions",
                column: "InventoryItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptions_InventoryItems_InventoryItemId",
                table: "ProductOptions",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptions_InventoryItems_InventoryItemId",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_InventoryItemId",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "ConsumeQtyBase",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "InventoryItemId",
                table: "ProductOptions");
        }
    }
}
