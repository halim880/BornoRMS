using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BornoBit.Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreDepartmentsAndRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreDepartmentId",
                table: "StoreIssues",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreRequisitionId",
                table: "StoreIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BanglaName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_StoreDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StoreDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredByUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StoreRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreRequisitions_StoreDepartments_StoreDepartmentId",
                        column: x => x.StoreDepartmentId,
                        principalTable: "StoreDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreRequisitionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreRequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedQtyBase = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ApprovedQtyBase = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IssuedQtyBase = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreRequisitionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreRequisitionLines_StoreItems_StoreItemId",
                        column: x => x.StoreItemId,
                        principalTable: "StoreItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreRequisitionLines_StoreRequisitions_StoreRequisitionId",
                        column: x => x.StoreRequisitionId,
                        principalTable: "StoreRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreRequisitionLines_StoreUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "StoreUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreIssues_StoreDepartmentId",
                table: "StoreIssues",
                column: "StoreDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreIssues_StoreRequisitionId",
                table: "StoreIssues",
                column: "StoreRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreDepartments_Code",
                table: "StoreDepartments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreDepartments_DisplayOrder",
                table: "StoreDepartments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRequisitionLines_StoreItemId",
                table: "StoreRequisitionLines",
                column: "StoreItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRequisitionLines_StoreRequisitionId",
                table: "StoreRequisitionLines",
                column: "StoreRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRequisitionLines_UnitId",
                table: "StoreRequisitionLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRequisitions_RequisitionNumber",
                table: "StoreRequisitions",
                column: "RequisitionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreRequisitions_Status",
                table: "StoreRequisitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRequisitions_StoreDepartmentId",
                table: "StoreRequisitions",
                column: "StoreDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreIssues_StoreDepartments_StoreDepartmentId",
                table: "StoreIssues",
                column: "StoreDepartmentId",
                principalTable: "StoreDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreIssues_StoreRequisitions_StoreRequisitionId",
                table: "StoreIssues",
                column: "StoreRequisitionId",
                principalTable: "StoreRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreIssues_StoreDepartments_StoreDepartmentId",
                table: "StoreIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreIssues_StoreRequisitions_StoreRequisitionId",
                table: "StoreIssues");

            migrationBuilder.DropTable(
                name: "StoreRequisitionLines");

            migrationBuilder.DropTable(
                name: "StoreRequisitions");

            migrationBuilder.DropTable(
                name: "StoreDepartments");

            migrationBuilder.DropIndex(
                name: "IX_StoreIssues_StoreDepartmentId",
                table: "StoreIssues");

            migrationBuilder.DropIndex(
                name: "IX_StoreIssues_StoreRequisitionId",
                table: "StoreIssues");

            migrationBuilder.DropColumn(
                name: "StoreDepartmentId",
                table: "StoreIssues");

            migrationBuilder.DropColumn(
                name: "StoreRequisitionId",
                table: "StoreIssues");
        }
    }
}
