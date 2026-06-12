using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Categories;

public record UpdateCategoryCommand(Guid Id, string Name, TransactionType Type) : IRequest;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Type).IsInEnum();
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _db.FinanceCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        var name = request.Name.Trim();
        if (await _db.FinanceCategories.AnyAsync(c => c.Id != request.Id && c.Type == request.Type && c.Name == name, cancellationToken))
            throw new ConflictException($"A {request.Type} category named '{name}' already exists.");

        category.Update(name, request.Type);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
