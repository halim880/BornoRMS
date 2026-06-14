using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

public record ToggleOrderPriorityCommand(Guid OrderId, bool IsPriority) : IRequest<Unit>;
public record UpdateKitchenNotesCommand(Guid OrderId, string? Notes) : IRequest<Unit>;

public class ToggleOrderPriorityCommandValidator : AbstractValidator<ToggleOrderPriorityCommand>
{
    public ToggleOrderPriorityCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

public class UpdateKitchenNotesCommandValidator : AbstractValidator<UpdateKitchenNotesCommand>
{
    public UpdateKitchenNotesCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class ToggleOrderPriorityCommandHandler : IRequestHandler<ToggleOrderPriorityCommand, Unit>
{
    private readonly IAppDbContext _db;
    public ToggleOrderPriorityCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ToggleOrderPriorityCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");
        try
        {
            order.SetPriority(request.IsPriority);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class UpdateKitchenNotesCommandHandler : IRequestHandler<UpdateKitchenNotesCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateKitchenNotesCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateKitchenNotesCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");
        order.UpdateKitchenNotes(request.Notes);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
