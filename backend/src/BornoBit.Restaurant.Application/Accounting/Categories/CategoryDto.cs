using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Application.Accounting.Categories;

public record CategoryDto(
    Guid Id,
    string Name,
    TransactionType Type,
    bool IsActive);
