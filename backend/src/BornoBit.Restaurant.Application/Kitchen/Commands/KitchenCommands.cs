using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KitchenEntity = BornoBit.Restaurant.Domain.Kitchen.Kitchen;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

public record CreateKitchenCommand(string Name, string? Code, string? ColorHex, string? PrinterName, int DisplayOrder) : IRequest<Guid>;
public record UpdateKitchenCommand(Guid Id, string Name, string? Code, string? ColorHex, string? PrinterName, int DisplayOrder) : IRequest<Unit>;
public record ToggleKitchenActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class CreateKitchenCommandValidator : AbstractValidator<CreateKitchenCommand>
{
    public CreateKitchenCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).MaximumLength(20);
        RuleFor(x => x.ColorHex).MaximumLength(9);
        RuleFor(x => x.PrinterName).MaximumLength(120);
    }
}

public class UpdateKitchenCommandValidator : AbstractValidator<UpdateKitchenCommand>
{
    public UpdateKitchenCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).MaximumLength(20);
        RuleFor(x => x.ColorHex).MaximumLength(9);
        RuleFor(x => x.PrinterName).MaximumLength(120);
    }
}

public class CreateKitchenCommandHandler : IRequestHandler<CreateKitchenCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateKitchenCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateKitchenCommand request, CancellationToken cancellationToken)
    {
        var kitchen = KitchenEntity.Create(request.Name, request.Code, request.ColorHex, request.PrinterName, request.DisplayOrder);
        _db.Kitchens.Add(kitchen);
        await _db.SaveChangesAsync(cancellationToken);
        return kitchen.Id;
    }
}

public class UpdateKitchenCommandHandler : IRequestHandler<UpdateKitchenCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateKitchenCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateKitchenCommand request, CancellationToken cancellationToken)
    {
        var kitchen = await _db.Kitchens.FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Kitchen not found.");
        kitchen.UpdateDetails(request.Name, request.Code, request.ColorHex, request.PrinterName, request.DisplayOrder);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class ToggleKitchenActiveCommandHandler : IRequestHandler<ToggleKitchenActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public ToggleKitchenActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ToggleKitchenActiveCommand request, CancellationToken cancellationToken)
    {
        var kitchen = await _db.Kitchens.FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Kitchen not found.");
        if (request.IsActive) kitchen.Activate(); else kitchen.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
