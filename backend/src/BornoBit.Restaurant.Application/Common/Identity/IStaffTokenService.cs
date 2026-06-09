namespace BornoBit.Restaurant.Application.Common.Identity;

public record StaffTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface IStaffTokenService
{
    StaffTokenResult IssueAccessToken(Guid userId, string userName, string? email, string fullName, IEnumerable<string> roles);
}
