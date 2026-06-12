using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Categories;

public record SetCategoryActiveCommand(Guid Id, bool IsActive) : IRequest;

public class SetCategoryActiveCommandHandler : IRequestHandler<SetCategoryActiveCommand>
{
    private readonly IAppDbContext _db;

    public SetCategoryActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SetCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var category = await _db.FinanceCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        category.SetActive(request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
