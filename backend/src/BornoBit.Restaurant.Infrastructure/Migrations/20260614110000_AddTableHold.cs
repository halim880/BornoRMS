using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HeldByUserId",
                table: "RestaurantTables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeldByName",
                table: "RestaurantTables",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HeldUntilUtc",
                table: "RestaurantTables",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HeldByUserId", table: "RestaurantTables");
            migrationBuilder.DropColumn(name: "HeldByName", table: "RestaurantTables");
            migrationBuilder.DropColumn(name: "HeldUntilUtc", table: "RestaurantTables");
        }
    }
}
