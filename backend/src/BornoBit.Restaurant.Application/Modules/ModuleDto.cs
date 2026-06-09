namespace BornoBit.Restaurant.Application.Modules;

public record ModuleDto(
    Guid Id,
    string Title,
    string? Icon,
    int DisplayOrder,
    bool IsActive,
    string? RequiredRole,
    string? FirstAccessibleUrl,
    int AccessibleMenuCount,
    IReadOnlyList<string>? AccessibleUrls = null);
