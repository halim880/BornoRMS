using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Suppliers;

public record StoreSupplierDto(
    Guid Id,
    string Code,
    string Name,
    string? Phone,
    string? Address,
    int PaymentTermsDays,
    string? Notes,
    bool IsActive);

// ---- Query ----

public record GetStoreSuppliersQuery : IRequest<IReadOnlyList<StoreSupplierDto>>;

public class GetStoreSuppliersQueryHandler : IRequestHandler<GetStoreSuppliersQuery, IReadOnlyList<StoreSupplierDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreSuppliersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreSupplierDto>> Handle(GetStoreSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await _db.StoreSuppliers
            .OrderBy(s => s.Name)
            .Select(s => new StoreSupplierDto(s.Id, s.Code, s.Name, s.Phone, s.Address, s.PaymentTermsDays, s.Notes, s.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// ---- Create ----

public record CreateStoreSupplierCommand(string Code, string Name, string? Phone, string? Address, int PaymentTermsDays, string? Notes) : IRequest<Guid>;

public class CreateStoreSupplierCommandValidator : AbstractValidator<CreateStoreSupplierCommand>
{
    public CreateStoreSupplierCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateStoreSupplierCommandHandler : IRequestHandler<CreateStoreSupplierCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateStoreSupplierCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStoreSupplierCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.StoreSuppliers.AnyAsync(s => s.Code == code, cancellationToken))
            throw new ValidationException($"A store supplier with code '{code}' already exists.");

        var entity = StoreSupplier.Create(code, request.Name, request.Phone, request.Address, request.PaymentTermsDays, request.Notes);
        _db.StoreSuppliers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// ---- Update ----

public record UpdateStoreSupplierCommand(Guid Id, string Name, string? Phone, string? Address, int PaymentTermsDays, string? Notes) : IRequest<Unit>;

public class UpdateStoreSupplierCommandValidator : AbstractValidator<UpdateStoreSupplierCommand>
{
    public UpdateStoreSupplierCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateStoreSupplierCommandHandler : IRequestHandler<UpdateStoreSupplierCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateStoreSupplierCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStoreSupplierCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreSuppliers.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store supplier {request.Id} not found.");
        entity.UpdateDetails(request.Name, request.Phone, request.Address, request.PaymentTermsDays, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- SetActive ----

public record SetStoreSupplierActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetStoreSupplierActiveCommandValidator : AbstractValidator<SetStoreSupplierActiveCommand>
{
    public SetStoreSupplierActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetStoreSupplierActiveCommandHandler : IRequestHandler<SetStoreSupplierActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public SetStoreSupplierActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetStoreSupplierActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreSuppliers.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store supplier {request.Id} not found.");
        if (request.IsActive) entity.Activate(); else entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
