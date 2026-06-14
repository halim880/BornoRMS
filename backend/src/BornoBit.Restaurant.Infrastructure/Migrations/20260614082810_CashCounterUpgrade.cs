using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CashCounterUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CashDrawerSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrawerNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashReceived = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashPaidOut = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CountedClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CloseNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_CashDrawerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDrawerSessions_CashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "CashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tendered = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CashDrawerSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantBillingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VatPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ServiceChargePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    TipEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HighDiscountThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantBillingSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AccountedAtUtc",
                table: "Orders",
                column: "AccountedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatus_OrderedAtUtc",
                table: "Orders",
                columns: new[] { "PaymentStatus", "OrderedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerSessions_CashAccountId",
                table: "CashDrawerSessions",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerSessions_CashierUserId_Status",
                table: "CashDrawerSessions",
                columns: new[] { "CashierUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerSessions_DrawerNumber",
                table: "CashDrawerSessions",
                column: "DrawerNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerSessions_OpenedAtUtc",
                table: "CashDrawerSessions",
                column: "OpenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerSessions_Status",
                table: "CashDrawerSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_Action",
                table: "FinancialAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_EntityType_EntityId",
                table: "FinancialAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_TimestampUtc",
                table: "FinancialAuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CashDrawerSessionId",
                table: "Payments",
                column: "CashDrawerSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAtUtc",
                table: "Payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAtUtc_Method",
                table: "Payments",
                columns: new[] { "CreatedAtUtc", "Method" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Method",
                table: "Payments",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            // Backfill: synthesise one captured charge Payment for every order already paid through the
            // legacy inline fields, so the new ledger/board show historical takings. Grand total is the
            // pre-tip formula (tip is a brand-new column, 0 for legacy orders).
            migrationBuilder.Sql(@"
WITH OrderTotals AS (
    SELECT o.Id AS OrderId, o.PaymentMethod, o.AmountTendered, o.ChangeGiven, o.PaidAtUtc, o.OrderedAtUtc,
           CASE WHEN (ISNULL(s.Subtotal,0) - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.RoundingAdjustment) < 0
                THEN 0
                ELSE (ISNULL(s.Subtotal,0) - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.RoundingAdjustment)
           END AS GrandTotal
    FROM Orders o
    OUTER APPLY (SELECT SUM(ol.UnitPriceSnapshot * ol.Quantity) AS Subtotal FROM OrderLines ol WHERE ol.OrderId = o.Id) s
    WHERE o.IsPaid = 1 AND o.IsDeleted = 0
)
INSERT INTO Payments (Id, OrderId, Method, Provider, Amount, Tendered, Change, Kind, Status, CreatedAtUtc,
                      CashierUserId, CashierName, CashDrawerSessionId, OriginalPaymentId, Reference, Notes, VoidReason)
SELECT NEWID(), t.OrderId, ISNULL(t.PaymentMethod, 0), NULL, t.GrandTotal,
       ISNULL(t.AmountTendered, t.GrandTotal), ISNULL(t.ChangeGiven, 0), 0, 0,
       ISNULL(t.PaidAtUtc, t.OrderedAtUtc), NULL, NULL, NULL, NULL, NULL,
       N'Backfilled from legacy inline payment', NULL
FROM OrderTotals t
WHERE NOT EXISTS (SELECT 1 FROM Payments p WHERE p.OrderId = t.OrderId);");

            migrationBuilder.Sql("UPDATE Orders SET PaymentStatus = 2 WHERE IsPaid = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashDrawerSessions");

            migrationBuilder.DropTable(
                name: "FinancialAuditLogs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RestaurantBillingSettings");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AccountedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatus_OrderedAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TipAmount",
                table: "Orders");
        }
    }
}
