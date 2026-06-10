using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Suppliers;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    int PaymentTermsDays,
    string? Notes
) : IRequest<Unit>;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateSupplierCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Supplier {request.Id} not found.");

        entity.UpdateDetails(request.Name, request.Phone, request.Address, request.PaymentTermsDays, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
