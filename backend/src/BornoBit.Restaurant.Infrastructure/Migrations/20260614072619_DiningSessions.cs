using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DiningSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiningSessionId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiningSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RestaurantTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaiterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaiterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MergedIntoSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiningSessions_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiningSessionId",
                table: "Orders",
                column: "DiningSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_RestaurantTableId",
                table: "DiningSessions",
                column: "RestaurantTableId");

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_SessionNumber",
                table: "DiningSessions",
                column: "SessionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_Status",
                table: "DiningSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_WaiterUserId",
                table: "DiningSessions",
                column: "WaiterUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DiningSessions_DiningSessionId",
                table: "Orders",
                column: "DiningSessionId",
                principalTable: "DiningSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DiningSessions_DiningSessionId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DiningSessions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DiningSessionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiningSessionId",
                table: "Orders");
        }
    }
}
