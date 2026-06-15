using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemVariantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "InventoryItems",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "InventoryItems");
        }
    }
}
