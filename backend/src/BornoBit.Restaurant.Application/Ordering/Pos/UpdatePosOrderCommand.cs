using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// Edits an open POS order's metadata: order type, table (dine-in) and customer details.
/// Lines and billing are untouched. Allowed until the order is paid, completed or cancelled.
/// </summary>
public record UpdatePosOrderCommand(
    Guid OrderId,
    OrderType Type,
    Guid? TableId,
    string? CustomerPhone,
    string? CustomerName,
    string? CustomerAddress) : IRequest<PlaceOrderResult>;

public class UpdatePosOrderCommandValidator : AbstractValidator<UpdatePosOrderCommand>
{
    public UpdatePosOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.TableId)
            .NotNull()
            .When(x => x.Type == OrderType.DineIn)
            .WithMessage("Dine-in orders require a table.");
        RuleFor(x => x.CustomerPhone).MaximumLength(40);
        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.CustomerAddress).MaximumLength(500);
    }
}

public class UpdatePosOrderCommandHandler : IRequestHandler<UpdatePosOrderCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;

    public UpdatePosOrderCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PlaceOrderResult> Handle(UpdatePosOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        if (request.Type == OrderType.DineIn && request.TableId is { } tableId)
            await PosTableGuard.EnsureAvailableAsync(_db, tableId, currentOrderId: order.Id, cancellationToken);

        var customerId = await PosCustomerResolver.ResolveAsync(
            _db, request.CustomerPhone, request.CustomerName, request.CustomerAddress, cancellationToken);

        try
        {
            order.UpdateTypeAndTable(request.Type, request.TableId);
            order.ReassignCustomer(customerId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }
}
