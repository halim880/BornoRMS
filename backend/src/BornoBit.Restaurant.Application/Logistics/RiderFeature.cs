using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Logistics;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Logistics;

public record RiderDto(Guid Id, string Name, string Phone, string? Vehicle, bool IsActive);

// ---------- query ----------

public record GetRidersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<RiderDto>>;

public class GetRidersQueryHandler : IRequestHandler<GetRidersQuery, IReadOnlyList<RiderDto>>
{
    private readonly IAppDbContext _db;
    public GetRidersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RiderDto>> Handle(GetRidersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Riders.AsQueryable();
        if (!request.IncludeInactive) query = query.Where(r => r.IsActive);

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RiderDto(r.Id, r.Name, r.Phone, r.Vehicle, r.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// ---------- create ----------

public record CreateRiderCommand(string Name, string Phone, string? Vehicle) : IRequest<Guid>;

public class CreateRiderCommandValidator : AbstractValidator<CreateRiderCommand>
{
    public CreateRiderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Vehicle).MaximumLength(120);
    }
}

public class CreateRiderCommandHandler : IRequestHandler<CreateRiderCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateRiderCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateRiderCommand request, CancellationToken cancellationToken)
    {
        var rider = Rider.Create(request.Name, request.Phone, request.Vehicle);
        _db.Riders.Add(rider);
        await _db.SaveChangesAsync(cancellationToken);
        return rider.Id;
    }
}

// ---------- update ----------

public record UpdateRiderCommand(Guid Id, string Name, string Phone, string? Vehicle) : IRequest<Unit>;

public class UpdateRiderCommandValidator : AbstractValidator<UpdateRiderCommand>
{
    public UpdateRiderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Vehicle).MaximumLength(120);
    }
}

public class UpdateRiderCommandHandler : IRequestHandler<UpdateRiderCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateRiderCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateRiderCommand request, CancellationToken cancellationToken)
    {
        var rider = await _db.Riders.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Rider not found.");
        rider.Update(request.Name, request.Phone, request.Vehicle);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---------- activate / retire ----------

public record SetRiderActiveCommand(Guid Id, bool Active) : IRequest<Unit>;

public class SetRiderActiveCommandHandler : IRequestHandler<SetRiderActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public SetRiderActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetRiderActiveCommand request, CancellationToken cancellationToken)
    {
        var rider = await _db.Riders.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Rider not found.");
        rider.SetActive(request.Active);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
