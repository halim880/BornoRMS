using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

public record ChangeOrderStatusCommand(Guid OrderId, OrderStatus Target, string? CancellationReason = null)
    : IRequest<Unit>;

public class ChangeOrderStatusCommandValidator : AbstractValidator<ChangeOrderStatusCommand>
{
    public ChangeOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public class ChangeOrderStatusCommandHandler : IRequestHandler<ChangeOrderStatusCommand, Unit>
{
    private readonly IAppDbContext _db;

    public ChangeOrderStatusCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) throw new NotFoundException("Order not found.");

        try
        {
            switch (request.Target)
            {
                case OrderStatus.Confirmed: order.Confirm(); break;
                case OrderStatus.Preparing: order.StartPreparing(); break;
                case OrderStatus.Ready: order.MarkReady(); break;
                case OrderStatus.Served: order.MarkServed(); break;
                case OrderStatus.Completed: order.Complete(); break;
                case OrderStatus.Cancelled: order.Cancel(request.CancellationReason); break;
                default: throw new ConflictException($"Unsupported target status '{request.Target}'.");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Illegal transition (e.g. stale UI / concurrent change) — surface as a clean conflict.
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
