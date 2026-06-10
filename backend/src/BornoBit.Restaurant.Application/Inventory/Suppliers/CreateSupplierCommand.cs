using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Suppliers;

public record CreateSupplierCommand(
    string Code,
    string Name,
    string? Phone,
    string? Address,
    int PaymentTermsDays,
    string? Notes
) : IRequest<Guid>;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateSupplierCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var clash = await _db.Suppliers.AnyAsync(s => s.Code == code, cancellationToken);
        if (clash) throw new ValidationException($"A supplier with code '{code}' already exists.");

        var entity = Supplier.Create(code, request.Name, request.Phone, request.Address, request.PaymentTermsDays, request.Notes);
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
