using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Raises a service request from a table — used by the QR/customer flow and staff alike.</summary>
public record CreateCustomerRequestCommand(Guid TableId, CustomerRequestType Type, string? Note = null)
    : IRequest<Guid>;

public class CreateCustomerRequestCommandValidator : AbstractValidator<CreateCustomerRequestCommand>
{
    public CreateCustomerRequestCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class CreateCustomerRequestCommandHandler : IRequestHandler<CreateCustomerRequestCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CreateCustomerRequestCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateCustomerRequestCommand request, CancellationToken cancellationToken)
    {
        var tableOk = await _db.RestaurantTables.AnyAsync(t => t.Id == request.TableId && t.IsActive, cancellationToken);
        if (!tableOk) throw new NotFoundException("Table not found.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = CustomerRequest.Create(request.TableId, request.Type, nowUtc, request.Note);

        _db.CustomerRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
