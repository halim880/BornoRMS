using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

public enum PosRoundingMode
{
    None = 0,
    Floor = 1,
    Ceil = 2
}

/// <summary>
/// Cash round-off at checkout: Floor drops the fractional part of the amount due, Ceil raises it
/// to the next whole unit, None restores the exact total. The adjustment is computed server-side
/// from the current subtotal/discount and persisted so payment validation sees the rounded total.
/// </summary>
public record ApplyPosRoundingCommand(Guid OrderId, PosRoundingMode Mode) : IRequest<BillSummaryDto>;

public class ApplyPosRoundingCommandValidator : AbstractValidator<ApplyPosRoundingCommand>
{
    public ApplyPosRoundingCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Mode).IsInEnum();
    }
}

public class ApplyPosRoundingCommandHandler : IRequestHandler<ApplyPosRoundingCommand, BillSummaryDto>
{
    private readonly IAppDbContext _db;

    public ApplyPosRoundingCommandHandler(IAppDbContext db) => _db = db;

    public async Task<BillSummaryDto> Handle(ApplyPosRoundingCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) throw new NotFoundException("Order not found.");

        var baseTotal = Math.Max(0m, order.Subtotal - order.DiscountAmount);
        var adjustment = request.Mode switch
        {
            PosRoundingMode.Floor => Math.Floor(baseTotal) - baseTotal,
            PosRoundingMode.Ceil => Math.Ceiling(baseTotal) - baseTotal,
            _ => 0m
        };

        try
        {
            order.ApplyRounding(adjustment);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApplyDiscountCommandHandler.ToSummary(order);
    }
}
