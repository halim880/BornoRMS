using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Numbering;

public record CreateNumberingScopeCommand(
    string Code,
    string Name,
    string Prefix,
    NumberingCadence Cadence,
    byte Digits,
    bool ResetByOutlet
) : IRequest<Guid>;

public class CreateNumberingScopeCommandValidator : AbstractValidator<CreateNumberingScopeCommand>
{
    public CreateNumberingScopeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Prefix).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Digits).InclusiveBetween((byte)1, (byte)10);
    }
}

public class CreateNumberingScopeCommandHandler : IRequestHandler<CreateNumberingScopeCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateNumberingScopeCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateNumberingScopeCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.NumberingScopes.AnyAsync(s => s.Code == code, cancellationToken))
            throw new ConflictException($"Numbering scope '{code}' already exists.");

        var scope = NumberingScope.Create(code, request.Name, request.Prefix, request.Cadence, request.Digits, request.ResetByOutlet);
        _db.NumberingScopes.Add(scope);
        await _db.SaveChangesAsync(cancellationToken);
        return scope.Id;
    }
}
