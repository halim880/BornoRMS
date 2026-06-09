namespace BornoBit.Restaurant.Application.Common.Identity;

public record CustomerTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface ICustomerTokenService
{
    CustomerTokenResult IssueAccessToken(Guid customerId, string phone, string? fullName);
}
