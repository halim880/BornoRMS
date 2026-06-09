namespace BornoBit.Restaurant.Application.Customers.Portal;

public record CustomerDto(
    Guid CustomerId,
    string Phone,
    string? FullName);

public record CustomerLoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    CustomerDto Customer);
