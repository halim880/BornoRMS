using BornoBit.Restaurant.Application.Users;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly UserManager<ApplicationUser> _users;

    public GetUsersQueryHandler(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _users.Users.IgnoreQueryFilters().AsQueryable();
        if (!request.IncludeInactive) query = query.Where(u => !u.IsDeleted);

        var list = await query.OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        var dtos = new List<UserDto>(list.Count);
        foreach (var u in list)
        {
            var roles = await _users.GetRolesAsync(u);
            dtos.Add(new UserDto(
                u.Id,
                u.UserName ?? string.Empty,
                u.Email ?? string.Empty,
                u.FullName,
                !u.IsDeleted,
                u.IsSuperAdmin,
                roles.ToArray(),
                u.CreatedAtUtc));
        }
        return dtos;
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly UserManager<ApplicationUser> _users;

    public CreateUserCommandHandler(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.FindByNameAsync(userName) is not null)
            throw new ConflictException($"A user with username '{userName}' already exists.");
        if (await _users.FindByEmailAsync(email) is not null)
            throw new ConflictException($"A user with email '{email}' already exists.");

        var password = string.IsNullOrWhiteSpace(request.InitialPassword)
            ? GenerateTempPassword()
            : request.InitialPassword!;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            IsSuperAdmin = false
        };

        var create = await _users.CreateAsync(user, password);
        if (!create.Succeeded)
            throw new ConflictException(string.Join("; ", create.Errors.Select(e => e.Description)));

        var rolesToAssign = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r) && Roles.All.Contains(r) && r != Roles.SuperAdmin)
            .ToArray();
        if (rolesToAssign.Length > 0)
        {
            var addRoles = await _users.AddToRolesAsync(user, rolesToAssign);
            if (!addRoles.Succeeded)
                throw new ConflictException(string.Join("; ", addRoles.Errors.Select(e => e.Description)));
        }

        return new CreateUserResult(user.Id, password);
    }

    internal static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*";
        var pool = upper + lower + digits + symbols;
        Span<char> chars = stackalloc char[16];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < chars.Length; i++) chars[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        // Fisher-Yates shuffle
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly UserManager<ApplicationUser> _users;

    public UpdateUserCommandHandler(UserManager<ApplicationUser> users) => _users = users;

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(request.Id.ToString())
            ?? throw new NotFoundException($"User {request.Id} not found.");

        var newUserName = request.UserName.Trim();
        var newEmail = request.Email.Trim().ToLowerInvariant();

        if (!string.Equals(user.UserName, newUserName, StringComparison.OrdinalIgnoreCase))
        {
            var clash = await _users.FindByNameAsync(newUserName);
            if (clash is not null && clash.Id != user.Id)
                throw new ConflictException($"A user with username '{newUserName}' already exists.");
            user.UserName = newUserName;
            user.NormalizedUserName = _users.NormalizeName(newUserName);
        }
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var clash = await _users.FindByEmailAsync(newEmail);
            if (clash is not null && clash.Id != user.Id)
                throw new ConflictException($"A user with email '{newEmail}' already exists.");
            user.Email = newEmail;
            user.NormalizedEmail = _users.NormalizeEmail(newEmail);
        }
        user.FullName = request.FullName.Trim();

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
            throw new ConflictException(string.Join("; ", update.Errors.Select(e => e.Description)));

        if (user.IsSuperAdmin) return; // never adjust super-admin role assignments via this command

        var existing = await _users.GetRolesAsync(user);
        var desired = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r) && Roles.All.Contains(r) && r != Roles.SuperAdmin)
            .ToArray();
        var toRemove = existing.Except(desired).ToArray();
        var toAdd = desired.Except(existing).ToArray();
        if (toRemove.Length > 0)
        {
            var r = await _users.RemoveFromRolesAsync(user, toRemove);
            if (!r.Succeeded) throw new ConflictException(string.Join("; ", r.Errors.Select(e => e.Description)));
        }
        if (toAdd.Length > 0)
        {
            var a = await _users.AddToRolesAsync(user, toAdd);
            if (!a.Succeeded) throw new ConflictException(string.Join("; ", a.Errors.Select(e => e.Description)));
        }
    }
}

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly UserManager<ApplicationUser> _users;

    public SetUserActiveCommandHandler(UserManager<ApplicationUser> users) => _users = users;

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(request.Id.ToString())
            ?? throw new NotFoundException($"User {request.Id} not found.");

        if (user.IsSuperAdmin && !request.IsActive)
            throw new ForbiddenException("Cannot deactivate a super-admin from this UI.");

        user.IsDeleted = !request.IsActive;
        user.LockoutEnabled = !request.IsActive;
        user.LockoutEnd = request.IsActive ? null : DateTimeOffset.MaxValue;

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
            throw new ConflictException(string.Join("; ", update.Errors.Select(e => e.Description)));
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, string>
{
    private readonly UserManager<ApplicationUser> _users;

    public ResetPasswordCommandHandler(UserManager<ApplicationUser> users) => _users = users;

    public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(request.Id.ToString())
            ?? throw new NotFoundException($"User {request.Id} not found.");

        var newPassword = CreateUserCommandHandler.GenerateTempPassword();
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new ConflictException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return newPassword;
    }
}
