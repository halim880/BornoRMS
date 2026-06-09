using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Ordering;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// Staff/waiter order entry. Resolves the customer (find-or-create by phone, or the shared walk-in
/// customer when no phone is given) then delegates to <see cref="PlaceOrderCommand"/>.
/// </summary>
public record PlaceWaiterOrderCommand(
    string? CustomerPhone,
    string? CustomerName,
    Guid? TableId,
    OrderType Type,
    string? Notes,
    IReadOnlyList<PlaceOrderLineInput> Lines) : IRequest<PlaceOrderResult>;

public class PlaceWaiterOrderCommandValidator : AbstractValidator<PlaceWaiterOrderCommand>
{
    public PlaceWaiterOrderCommandValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item is required.");
        RuleFor(x => x.TableId)
            .NotNull()
            .When(x => x.Type == OrderType.DineIn)
            .WithMessage("Dine-in orders require a table.");
    }
}

public class PlaceWaiterOrderCommandHandler : IRequestHandler<PlaceWaiterOrderCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;
    private readonly ISender _sender;

    public PlaceWaiterOrderCommandHandler(IAppDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<PlaceOrderResult> Handle(PlaceWaiterOrderCommand request, CancellationToken cancellationToken)
    {
        var customerId = await ResolveCustomerIdAsync(request.CustomerPhone, request.CustomerName, cancellationToken);

        return await _sender.Send(
            new PlaceOrderCommand(customerId, request.TableId, request.Type, request.Notes, request.Lines),
            cancellationToken);
    }

    private async Task<Guid> ResolveCustomerIdAsync(string? phone, string? name, CancellationToken cancellationToken)
    {
        var lookup = string.IsNullOrWhiteSpace(phone) ? Customer.WalkInPhone : phone.Trim();

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == lookup, cancellationToken);
        if (customer is null)
        {
            customer = Customer.Create(lookup, name);
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(customer.FullName)
                 && lookup != Customer.WalkInPhone)
        {
            customer.UpdateName(name);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return customer.Id;
    }
}
