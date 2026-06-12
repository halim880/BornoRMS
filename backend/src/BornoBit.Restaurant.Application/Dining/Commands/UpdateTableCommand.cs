using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Dining.Commands;

public record UpdateTableCommand(Guid Id, string TableNumber, int Capacity) : IRequest<Unit>;

public class UpdateTableCommandValidator : AbstractValidator<UpdateTableCommand>
{
    public UpdateTableCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TableNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 100);
    }
}

public class UpdateTableCommandHandler : IRequestHandler<UpdateTableCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateTableCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.RestaurantTables
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Table {request.Id} not found.");

        var number = request.TableNumber.Trim();
        var clash = await _db.RestaurantTables
            .AnyAsync(t => t.Id != request.Id && t.TableNumber == number, cancellationToken);
        if (clash) throw new ValidationException($"A table numbered '{number}' already exists.");

        entity.UpdateDetails(number, request.Capacity);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
