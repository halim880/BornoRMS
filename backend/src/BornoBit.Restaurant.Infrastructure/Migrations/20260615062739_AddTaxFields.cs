using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PriceIncludesTax",
                table: "RestaurantBillingSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrepMinutes",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercent",
                table: "ProductCategories",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedReadyAtUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KotPrintStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrepMinutes",
                table: "OrderLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmountSnapshot",
                table: "OrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercentSnapshot",
                table: "OrderLines",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmountSnapshot",
                table: "OrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceIncludesTax",
                table: "RestaurantBillingSettings");

            migrationBuilder.DropColumn(
                name: "PrepMinutes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedReadyAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "KotPrintStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrepMinutes",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxAmountSnapshot",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxRatePercentSnapshot",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "TaxableAmountSnapshot",
                table: "OrderLines");
        }
    }
}
