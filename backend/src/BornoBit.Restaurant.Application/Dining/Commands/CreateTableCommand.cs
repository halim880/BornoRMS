using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Dining.Commands;

public record CreateTableCommand(string TableNumber, int Capacity) : IRequest<Guid>;

public class CreateTableCommandValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableCommandValidator()
    {
        RuleFor(x => x.TableNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 100);
    }
}

public class CreateTableCommandHandler : IRequestHandler<CreateTableCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateTableCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateTableCommand request, CancellationToken cancellationToken)
    {
        var number = request.TableNumber.Trim();
        var clash = await _db.RestaurantTables.AnyAsync(t => t.TableNumber == number, cancellationToken);
        if (clash) throw new ValidationException($"A table numbered '{number}' already exists.");

        var entity = RestaurantTable.Create(number, request.Capacity);
        _db.RestaurantTables.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
