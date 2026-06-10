using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.Items;

public record UpdateInventoryItemCommand(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    Guid InventoryCategoryId,
    InventoryItemType ItemType,
    Guid BaseUnitId,
    decimal ReorderLevel,
    decimal ReorderQty,
    bool IsPerishable,
    Guid? ProductId,
    decimal? PackSize,
    string? PackNote
) : IRequest<Unit>;

public class UpdateInventoryItemCommandValidator : AbstractValidator<UpdateInventoryItemCommand>
{
    public UpdateInventoryItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.InventoryCategoryId).NotEmpty();
        RuleFor(x => x.BaseUnitId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PackSize).GreaterThan(0).When(x => x.PackSize.HasValue);
        RuleFor(x => x.PackNote).MaximumLength(200);
    }
}

public class UpdateInventoryItemCommandHandler : IRequestHandler<UpdateInventoryItemCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateInventoryItemCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Stock item {request.Id} not found.");

        if (!await _db.InventoryCategories.AnyAsync(c => c.Id == request.InventoryCategoryId, cancellationToken))
            throw new NotFoundException($"Inventory category {request.InventoryCategoryId} not found.");
        if (!await _db.Units.AnyAsync(u => u.Id == request.BaseUnitId, cancellationToken))
            throw new NotFoundException($"Unit {request.BaseUnitId} not found.");
        if (request.ProductId is { } pid && !await _db.Products.AnyAsync(p => p.Id == pid, cancellationToken))
            throw new NotFoundException($"Product {pid} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.InventoryItems.AnyAsync(i => i.Id != request.Id && i.Code == code, cancellationToken))
            throw new ValidationException($"A stock item with code '{code}' already exists.");

        entity.UpdateDetails(
            code,
            request.Name,
            request.InventoryCategoryId,
            request.ItemType,
            request.BaseUnitId,
            request.BanglaName,
            request.ReorderLevel,
            request.ReorderQty,
            request.IsPerishable,
            request.ProductId,
            request.PackSize,
            request.PackNote);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
