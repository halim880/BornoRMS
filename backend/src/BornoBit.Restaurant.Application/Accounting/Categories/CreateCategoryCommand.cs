using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Categories;

public record CreateCategoryCommand(string Name, TransactionType Type) : IRequest<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Type).IsInEnum();
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _db.FinanceCategories.AnyAsync(c => c.Type == request.Type && c.Name == name, cancellationToken))
            throw new ConflictException($"A {request.Type} category named '{name}' already exists.");

        var category = FinanceCategory.Create(name, request.Type);
        _db.FinanceCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
