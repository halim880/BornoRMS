using BornoBit.Restaurant.Application.Inventory.Categories;
using BornoBit.Restaurant.Application.Inventory.Dashboard;
using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Application.Inventory.Movements;
using BornoBit.Restaurant.Application.Inventory.PurchaseOrders;
using BornoBit.Restaurant.Application.Inventory.Purchases;
using BornoBit.Restaurant.Application.Inventory.Recipes;
using BornoBit.Restaurant.Application.Inventory.Skus;
using BornoBit.Restaurant.Application.Inventory.Suppliers;
using BornoBit.Restaurant.Application.Inventory.Units;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Stock / Inventory module — mirrors the Blazor staff console
/// Stock pages (StockDashboard, StockItems, Skus, LowStock, Recipes, Suppliers, PurchaseOrders,
/// GoodsReceipts, Wastage, StockMovements). Every route forwards to an existing Application-layer
/// MediatR handler (registered via AddApplication()). Mounted under the versioned group →
/// /api/v1/staff/stock/*. Mobile/desktop polls, so there is no SignalR here.
///
/// Read coverage is complete. Write coverage is limited to the simple, single-entity commands
/// (supplier create/update/active, stock item create/update/active, SKU create, recipe upsert,
/// stock adjust, record wastage). The complex multi-line PO and GRN flows (create / receive / post)
/// are intentionally read-only here — they need a dedicated builder UI and are deferred.
/// </summary>
public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/stock")
            .RequireCors("Frontends")
            .RequireAuthorization("Inventory")
            .WithTags("Stock");

        // ---------- dashboard ----------
        // Aggregate read: every dashboard tile in one round-trip.
        group.MapGet("/dashboard", (ISender sender, int? days, CancellationToken ct) =>
            Exec(async () =>
            {
                var window = days is > 0 ? days.Value : 30;
                var summary = await sender.Send(new GetInventoryStockSummaryQuery(), ct);
                var valuation = await sender.Send(new GetStockValuationQuery(), ct);
                var lowStock = await sender.Send(new GetLowStockItemsQuery(), ct);
                var outOfStock = await sender.Send(new GetOutOfStockQuery(), ct);
                var waste = await sender.Send(new GetWastePercentQuery(window, 10), ct);
                var movers = await sender.Send(new GetFastSlowMoversQuery(window, 5), ct);
                var consumption = await sender.Send(new GetIngredientConsumptionQuery(window, 10), ct);
                return Results.Ok(new { summary, valuation, lowStock, outOfStock, waste, movers, consumption });
            }));

        group.MapGet("/valuation", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStockValuationQuery(), ct))));

        // ---------- items ----------
        group.MapGet("/items", (
            ISender sender,
            string? search, Guid? categoryId, InventoryItemType? itemType,
            bool? lowStockOnly, bool? includeInactive,
            string? sortBy, bool? sortDesc, int? page, int? pageSize,
            CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetInventoryItemsQuery(
                Search: string.IsNullOrWhiteSpace(search) ? null : search,
                CategoryId: categoryId,
                ItemType: itemType,
                LowStockOnly: lowStockOnly ?? false,
                IncludeInactive: includeInactive ?? true,
                SortBy: sortBy,
                SortDesc: sortDesc ?? false,
                Page: page ?? 1,
                PageSize: pageSize ?? 50), ct))));

        group.MapPost("/items", (CreateItemRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateInventoryItemCommand(
                    body.Code, body.Name, body.BanglaName, body.InventoryCategoryId, body.ItemType,
                    body.BaseUnitId, body.ReorderLevel, body.ReorderQty, body.IsPerishable,
                    body.ProductId, body.PackSize, body.PackNote), ct);
                return Results.Ok(new { id });
            }));

        group.MapPut("/items/{id:guid}", (Guid id, UpdateItemRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateInventoryItemCommand(
                    id, body.Code, body.Name, body.BanglaName, body.InventoryCategoryId, body.ItemType,
                    body.BaseUnitId, body.ReorderLevel, body.ReorderQty, body.IsPerishable,
                    body.ProductId, body.PackSize, body.PackNote), ct);
                return Results.NoContent();
            }));

        group.MapPost("/items/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetInventoryItemActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        // ---------- skus ----------
        group.MapGet("/skus", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductSkusQuery(), ct))));

        group.MapPost("/skus", (CreateSkuRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateSkuForProductCommand(
                    body.ProductId, body.VariantId, body.Code, body.Name, body.BanglaName,
                    body.InventoryCategoryId, body.BaseUnitId, body.ReorderLevel, body.ReorderQty,
                    body.IsPerishable, body.PackSize, body.PackNote), ct);
                return Results.Ok(new { id });
            }));

        // ---------- low stock ----------
        group.MapGet("/low-stock", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetLowStockItemsQuery(), ct))));

        group.MapGet("/out-of-stock", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetOutOfStockQuery(), ct))));

        // ---------- recipes ----------
        group.MapGet("/recipes", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetRecipesQuery(), ct))));

        group.MapGet("/recipes/{productId:guid}", (Guid productId, Guid? variantId, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var recipe = await sender.Send(new GetRecipeByProductQuery(productId, variantId), ct);
                return recipe is null ? Results.NotFound(new { message = "No recipe defined." }) : Results.Ok(recipe);
            }));

        group.MapPost("/recipes", (UpsertRecipeRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var items = (body.Items ?? new List<RecipeItemRequest>())
                    .Select(i => new RecipeItemInput(i.Id, i.InventoryItemId, i.Quantity, i.UnitId))
                    .ToList();
                var id = await sender.Send(new UpsertRecipeCommand(body.ProductId, body.VariantId, body.Yield, items), ct);
                return Results.Ok(new { id });
            }));

        // ---------- suppliers ----------
        group.MapGet("/suppliers", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetSuppliersQuery(), ct))));

        group.MapPost("/suppliers", (CreateSupplierRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateSupplierCommand(
                    body.Code, body.Name, body.Phone, body.Address, body.PaymentTermsDays, body.Notes), ct);
                return Results.Ok(new { id });
            }));

        group.MapPut("/suppliers/{id:guid}", (Guid id, UpdateSupplierRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateSupplierCommand(
                    id, body.Name, body.Phone, body.Address, body.PaymentTermsDays, body.Notes), ct);
                return Results.NoContent();
            }));

        group.MapPost("/suppliers/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetSupplierActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        // ---------- purchase orders (read-only: complex multi-line create/receive deferred) ----------
        group.MapGet("/purchase-orders", (ISender sender, PurchaseOrderStatus? status, int? page, int? pageSize, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetPurchaseOrdersQuery(status, page ?? 1, pageSize ?? 50), ct))));

        group.MapGet("/purchase-orders/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetPurchaseOrderQuery(id), ct))));

        // ---------- goods receipts (read-only: complex multi-line create/post deferred) ----------
        group.MapGet("/goods-receipts", (ISender sender, GoodsReceiptStatus? status, int? page, int? pageSize, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetGoodsReceiptsQuery(status, page ?? 1, pageSize ?? 50), ct))));

        group.MapGet("/goods-receipts/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetGoodsReceiptQuery(id), ct))));

        // ---------- wastage + adjustments (write into the stock ledger) ----------
        group.MapPost("/wastage", (RecordWastageRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new RecordWastageCommand(body.ItemId, body.QtyBase, body.Reason), ct);
                return Results.NoContent();
            }));

        group.MapPost("/adjust", (AdjustStockRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new AdjustStockCommand(body.ItemId, body.CountedQtyBase, body.Reason), ct);
                return Results.NoContent();
            }));

        // ---------- stock movements / history ----------
        group.MapGet("/stock-movements", (
            ISender sender, Guid? itemId, StockMovementType? movementType,
            DateTime? fromUtc, DateTime? toUtc, int? page, int? pageSize,
            CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetStockMovementsQuery(
                ItemId: itemId,
                MovementType: movementType,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: page ?? 1,
                PageSize: pageSize ?? 50), ct))));

        // ---------- reference data (pickers for the create/edit dialogs) ----------
        group.MapGet("/categories", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetInventoryCategoriesQuery(), ct))));

        group.MapGet("/units", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetUnitsQuery(), ct))));

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

    // ---------- request bodies ----------
    public record CreateItemRequest(
        string Code, string Name, string? BanglaName, Guid InventoryCategoryId, InventoryItemType ItemType,
        Guid BaseUnitId, decimal ReorderLevel, decimal ReorderQty, bool IsPerishable,
        Guid? ProductId, decimal? PackSize, string? PackNote);

    public record UpdateItemRequest(
        string Code, string Name, string? BanglaName, Guid InventoryCategoryId, InventoryItemType ItemType,
        Guid BaseUnitId, decimal ReorderLevel, decimal ReorderQty, bool IsPerishable,
        Guid? ProductId, decimal? PackSize, string? PackNote);

    public record CreateSkuRequest(
        Guid ProductId, Guid? VariantId, string Code, string Name, string? BanglaName,
        Guid InventoryCategoryId, Guid BaseUnitId, decimal ReorderLevel, decimal ReorderQty,
        bool IsPerishable, decimal? PackSize, string? PackNote);

    public record SetActiveRequest(bool IsActive);
    public record CreateSupplierRequest(string Code, string Name, string? Phone, string? Address, int PaymentTermsDays, string? Notes);
    public record UpdateSupplierRequest(string Name, string? Phone, string? Address, int PaymentTermsDays, string? Notes);
    public record RecordWastageRequest(Guid ItemId, decimal QtyBase, string Reason);
    public record AdjustStockRequest(Guid ItemId, decimal CountedQtyBase, string? Reason);
    public record RecipeItemRequest(Guid? Id, Guid InventoryItemId, decimal Quantity, Guid UnitId);
    public record UpsertRecipeRequest(Guid ProductId, Guid? VariantId, decimal Yield, List<RecipeItemRequest> Items);
}
