namespace BornoBit.Restaurant.Application.Users;

public record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    bool IsActive,
    bool IsSuperAdmin,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc
);
