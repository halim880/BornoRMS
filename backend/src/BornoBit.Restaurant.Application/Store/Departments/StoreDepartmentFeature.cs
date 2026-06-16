using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Departments;

public record StoreDepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    string? Description,
    int DisplayOrder,
    bool IsActive);

// ---- Query ----

public record GetStoreDepartmentsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<StoreDepartmentDto>>;

public class GetStoreDepartmentsQueryHandler : IRequestHandler<GetStoreDepartmentsQuery, IReadOnlyList<StoreDepartmentDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreDepartmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreDepartmentDto>> Handle(GetStoreDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.StoreDepartments.AsQueryable();
        if (!request.IncludeInactive) query = query.Where(d => d.IsActive);

        return await query
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name)
            .Select(d => new StoreDepartmentDto(d.Id, d.Code, d.Name, d.BanglaName, d.Description, d.DisplayOrder, d.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// ---- Create ----

public record CreateStoreDepartmentCommand(string Code, string Name, string? BanglaName, string? Description, int DisplayOrder) : IRequest<Guid>;

public class CreateStoreDepartmentCommandValidator : AbstractValidator<CreateStoreDepartmentCommand>
{
    public CreateStoreDepartmentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateStoreDepartmentCommandHandler : IRequestHandler<CreateStoreDepartmentCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateStoreDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStoreDepartmentCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.StoreDepartments.AnyAsync(d => d.Code == code, cancellationToken))
            throw new ValidationException($"A department with code '{code}' already exists.");

        var entity = StoreDepartment.Create(request.Code, request.Name, request.DisplayOrder, request.BanglaName, request.Description);
        _db.StoreDepartments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// ---- Update ----

public record UpdateStoreDepartmentCommand(Guid Id, string Code, string Name, string? BanglaName, string? Description, int DisplayOrder) : IRequest<Unit>;

public class UpdateStoreDepartmentCommandValidator : AbstractValidator<UpdateStoreDepartmentCommand>
{
    public UpdateStoreDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateStoreDepartmentCommandHandler : IRequestHandler<UpdateStoreDepartmentCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateStoreDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStoreDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreDepartments.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store department {request.Id} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.StoreDepartments.AnyAsync(d => d.Code == code && d.Id != request.Id, cancellationToken))
            throw new ValidationException($"A department with code '{code}' already exists.");

        entity.UpdateDetails(request.Code, request.Name, request.DisplayOrder, request.BanglaName, request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- SetActive ----

public record SetStoreDepartmentActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetStoreDepartmentActiveCommandValidator : AbstractValidator<SetStoreDepartmentActiveCommand>
{
    public SetStoreDepartmentActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetStoreDepartmentActiveCommandHandler : IRequestHandler<SetStoreDepartmentActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public SetStoreDepartmentActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetStoreDepartmentActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreDepartments.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store department {request.Id} not found.");
        if (request.IsActive) entity.Activate(); else entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
