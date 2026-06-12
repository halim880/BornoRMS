using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosOrderSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RoundingAdjustment",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoundingAdjustment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Customers");
        }
    }
}
