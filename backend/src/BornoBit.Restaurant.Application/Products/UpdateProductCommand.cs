using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

public record UpdateProductCommand(
    Guid Id,
    Guid ProductCategoryId,
    string Code,
    string Name,
    string? BanglaName,
    decimal Price,
    string? Description,
    string? ImagePath,
    int DisplayOrder
) : IRequest<Unit>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ProductCategoryId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ImagePath).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProductCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product {request.Id} not found.");

        var categoryExists = await _db.ProductCategories
            .AnyAsync(c => c.Id == request.ProductCategoryId, cancellationToken);
        if (!categoryExists) throw new NotFoundException($"Product category {request.ProductCategoryId} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        var clash = await _db.Products.AnyAsync(p => p.Id != request.Id && p.Code == code, cancellationToken);
        if (clash) throw new ValidationException($"A product with code '{code}' already exists.");

        entity.UpdateDetails(
            request.ProductCategoryId,
            code,
            request.Name,
            request.Price,
            request.BanglaName,
            request.ImagePath,
            request.Description,
            request.DisplayOrder);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
