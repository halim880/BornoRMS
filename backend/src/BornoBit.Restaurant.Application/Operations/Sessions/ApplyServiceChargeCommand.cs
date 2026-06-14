using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// Sets the service charge on an order, by percent of the subtotal or a fixed amount. Existing tax is
/// preserved. Authorized staff only.
/// </summary>
public record ApplyServiceChargeCommand(Guid OrderId, decimal? Percent, decimal? Amount) : IRequest<BillSummaryDto>;

public class ApplyServiceChargeCommandValidator : AbstractValidator<ApplyServiceChargeCommand>
{
    public ApplyServiceChargeCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Percent).InclusiveBetween(0, 100).When(x => x.Percent.HasValue);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
    }
}

public class ApplyServiceChargeCommandHandler : IRequestHandler<ApplyServiceChargeCommand, BillSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ApplyServiceChargeCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BillSummaryDto> Handle(ApplyServiceChargeCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager, Roles.Cashier);

        var order = await _db.Orders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var charge = request.Percent is { } p
            ? Math.Round(order.Subtotal * p / 100m, 2)
            : request.Amount ?? 0m;

        try { order.ApplyCharges(order.TaxAmount, charge); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApplyDiscountCommandHandler.ToSummary(order);
    }
}
