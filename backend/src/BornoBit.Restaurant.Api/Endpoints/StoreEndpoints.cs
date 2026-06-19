using BornoBit.Restaurant.Application.Store.Categories;
using BornoBit.Restaurant.Application.Store.Dashboard;
using BornoBit.Restaurant.Application.Store.Departments;
using BornoBit.Restaurant.Application.Store.Issues;
using BornoBit.Restaurant.Application.Store.Items;
using BornoBit.Restaurant.Application.Store.Ledger;
using BornoBit.Restaurant.Application.Store.Payments;
using BornoBit.Restaurant.Application.Store.Purchases;
using BornoBit.Restaurant.Application.Store.Reports;
using BornoBit.Restaurant.Application.Store.Requisitions;
using BornoBit.Restaurant.Application.Store.Suppliers;
using BornoBit.Restaurant.Application.Store.Units;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// Read-only REST surface for the Flutter Store / Warehouse module — mirrors the Blazor Store console
/// (Web/Components/Pages/Store/*.razor). Every route forwards to an existing Application-layer MediatR
/// handler; mutations stay Web-only for this pass. Mounts under /api/v1 (apiV1.MapStoreEndpoints()).
/// </summary>
public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/store")
            .RequireAuthorization("Store")
            .WithTags("Store");

        // ---------- dashboard ----------
        group.MapGet("/dashboard", (ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var summary = await sender.Send(new GetStoreDashboardSummaryQuery(), ct);
                var lowStock = await sender.Send(new GetStoreLowStockRowsQuery(), ct);
                return Results.Ok(new { summary, lowStock });
            }));

        // ---------- items ----------
        group.MapGet("/items", (
            string? search, Guid? categoryId, bool? lowStockOnly, bool? includeInactive,
            int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreItemsQuery(
                search, categoryId, lowStockOnly ?? false, includeInactive ?? true,
                page ?? 1, pageSize ?? 50), ct))));

        // ---------- categories ----------
        group.MapGet("/categories", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreCategoriesQuery(), ct))));

        // ---------- departments ----------
        group.MapGet("/departments", (bool? includeInactive, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreDepartmentsQuery(includeInactive ?? false), ct))));

        // ---------- suppliers ----------
        group.MapGet("/suppliers", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreSuppliersQuery(), ct))));

        // ---------- units (reference) ----------
        group.MapGet("/units", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreUnitsQuery(), ct))));

        // ---------- goods receipts (GRN) ----------
        group.MapGet("/goods-receipts", (
            StoreGoodsReceiptStatus? status, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreGoodsReceiptsQuery(status, page ?? 1, pageSize ?? 50), ct))));

        group.MapGet("/goods-receipts/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreGoodsReceiptQuery(id), ct))));

        // ---------- requisitions ----------
        group.MapGet("/requisitions", (
            StoreRequisitionStatus? status, Guid? departmentId, int? page, int? pageSize,
            ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreRequisitionsQuery(status, departmentId, page ?? 1, pageSize ?? 50), ct))));

        group.MapGet("/requisitions/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreRequisitionQuery(id), ct))));

        // ---------- issues ----------
        group.MapGet("/issues", (
            StoreIssueStatus? status, Guid? departmentId, int? page, int? pageSize,
            ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreIssuesQuery(status, departmentId, page ?? 1, pageSize ?? 50), ct))));

        group.MapGet("/issues/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStoreIssueQuery(id), ct))));

        // ---------- movement ledger ----------
        group.MapGet("/ledger", (
            Guid? itemId, DateTime? from, DateTime? to, int? take, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreMovementLedgerQuery(itemId, from, to, take ?? 1000), ct))));

        // ---------- supplier payables ----------
        group.MapGet("/payables", (bool? outstandingOnly, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreSupplierPayablesQuery(outstandingOnly ?? false), ct))));

        group.MapGet("/payments", (Guid? supplierId, int? take, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStorePaymentsQuery(supplierId, take ?? 100), ct))));

        // ---------- department consumption report ----------
        group.MapGet("/reports/department-issues", (
            DateTime from, DateTime to, Guid? departmentId, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetStoreDepartmentConsumptionQuery(from, to, departmentId), ct))));

        return app;
    }

    // Shared error translation so FluentValidation failures surface as 400, not 500.
    private static async Task<IResult> Exec(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new { message = errors.FirstOrDefault() ?? "Validation failed.", errors });
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }
}
