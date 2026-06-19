using BornoBit.Restaurant.Application.Dining.Commands;
using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Application.Products;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Catalog admin screens — mirrors the Blazor inventory pages
/// (Products.razor, ProductCategories.razor, Tables.razor). Every route forwards to an existing
/// Application-layer MediatR handler (registered via AddApplication()). Admin-only.
/// Mounted under the versioned group → /api/v1/staff/catalog/*.
/// </summary>
public static class CatalogAdminEndpoints
{
    public static IEndpointRouteBuilder MapCatalogAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/catalog")
            .RequireCors("Frontends")
            .RequireAuthorization("Admin")
            .WithTags("CatalogAdmin");

        // ---------- products ----------
        group.MapGet("/products", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductsQuery(), ct))));

        group.MapPost("/products", (CreateProductRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateProductCommand(
                    body.ProductCategoryId, body.Code, body.Name, body.BanglaName, body.Price,
                    body.Description, body.ImagePath, body.DisplayOrder, ToVariants(body.Variants)), ct);
                return Results.Created($"/api/v1/staff/catalog/products/{id}", new { id });
            }));

        group.MapPatch("/products/{id:guid}", (Guid id, UpdateProductRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateProductCommand(
                    id, body.ProductCategoryId, body.Code, body.Name, body.BanglaName, body.Price,
                    body.Description, body.ImagePath, body.DisplayOrder, ToVariants(body.Variants)), ct);
                return Results.NoContent();
            }));

        group.MapPost("/products/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetProductActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        // ---------- categories ----------
        group.MapGet("/categories", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductCategoriesQuery(), ct))));

        group.MapPost("/categories", (CreateCategoryRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateProductCategoryCommand(
                    body.Name, body.Description, body.DisplayOrder, body.TaxRatePercent), ct);
                return Results.Created($"/api/v1/staff/catalog/categories/{id}", new { id });
            }));

        group.MapPatch("/categories/{id:guid}", (Guid id, UpdateCategoryRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateProductCategoryCommand(
                    id, body.Name, body.Description, body.DisplayOrder, body.TaxRatePercent), ct);
                return Results.NoContent();
            }));

        group.MapPost("/categories/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetProductCategoryActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        // ---------- tables ----------
        group.MapGet("/tables", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetAllTablesQuery(), ct))));

        group.MapPost("/tables", (CreateTableRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateTableCommand(body.TableNumber, body.Capacity), ct);
                return Results.Created($"/api/v1/staff/catalog/tables/{id}", new { id });
            }));

        group.MapPatch("/tables/{id:guid}", (Guid id, UpdateTableRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateTableCommand(id, body.TableNumber, body.Capacity), ct);
                return Results.NoContent();
            }));

        group.MapPost("/tables/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetTableActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        return app;
    }

    private static List<ProductVariantInput>? ToVariants(IEnumerable<VariantRequest>? variants) =>
        variants is null
            ? null
            : variants.Select(v => new ProductVariantInput(v.Id, v.Name, v.Price, v.DisplayOrder)).ToList();

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
    public record VariantRequest(Guid? Id, string Name, decimal Price, int DisplayOrder);

    public record CreateProductRequest(
        Guid ProductCategoryId, string Code, string Name, string? BanglaName, decimal Price,
        string? Description, string? ImagePath, int DisplayOrder, List<VariantRequest>? Variants = null);

    public record UpdateProductRequest(
        Guid ProductCategoryId, string Code, string Name, string? BanglaName, decimal Price,
        string? Description, string? ImagePath, int DisplayOrder, List<VariantRequest>? Variants = null);

    public record CreateCategoryRequest(string Name, string? Description, int DisplayOrder, decimal? TaxRatePercent = null);
    public record UpdateCategoryRequest(string Name, string? Description, int DisplayOrder, decimal? TaxRatePercent = null);

    public record CreateTableRequest(string TableNumber, int Capacity);
    public record UpdateTableRequest(string TableNumber, int Capacity);

    public record SetActiveRequest(bool IsActive);
}
