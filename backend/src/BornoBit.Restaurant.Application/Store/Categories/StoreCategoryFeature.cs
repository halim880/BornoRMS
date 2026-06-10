using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Categories;

public record StoreCategoryDto(
    Guid Id,
    string Name,
    string? BanglaName,
    string? Description,
    int DisplayOrder,
    bool IsActive);

// ---- Query ----

public record GetStoreCategoriesQuery : IRequest<IReadOnlyList<StoreCategoryDto>>;

public class GetStoreCategoriesQueryHandler : IRequestHandler<GetStoreCategoriesQuery, IReadOnlyList<StoreCategoryDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreCategoryDto>> Handle(GetStoreCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.StoreCategories
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new StoreCategoryDto(c.Id, c.Name, c.BanglaName, c.Description, c.DisplayOrder, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// ---- Create ----

public record CreateStoreCategoryCommand(string Name, string? BanglaName, string? Description, int DisplayOrder) : IRequest<Guid>;

public class CreateStoreCategoryCommandValidator : AbstractValidator<CreateStoreCategoryCommand>
{
    public CreateStoreCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateStoreCategoryCommandHandler : IRequestHandler<CreateStoreCategoryCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateStoreCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStoreCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = StoreCategory.Create(request.Name, request.DisplayOrder, request.BanglaName, request.Description);
        _db.StoreCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// ---- Update ----

public record UpdateStoreCategoryCommand(Guid Id, string Name, string? BanglaName, string? Description, int DisplayOrder) : IRequest<Unit>;

public class UpdateStoreCategoryCommandValidator : AbstractValidator<UpdateStoreCategoryCommand>
{
    public UpdateStoreCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateStoreCategoryCommandHandler : IRequestHandler<UpdateStoreCategoryCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateStoreCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStoreCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store category {request.Id} not found.");
        entity.UpdateDetails(request.Name, request.DisplayOrder, request.BanglaName, request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- SetActive ----

public record SetStoreCategoryActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetStoreCategoryActiveCommandValidator : AbstractValidator<SetStoreCategoryActiveCommand>
{
    public SetStoreCategoryActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetStoreCategoryActiveCommandHandler : IRequestHandler<SetStoreCategoryActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public SetStoreCategoryActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetStoreCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store category {request.Id} not found.");
        if (request.IsActive) entity.Activate(); else entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
