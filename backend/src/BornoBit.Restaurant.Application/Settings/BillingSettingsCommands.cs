using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Settings;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Settings;

public record BillingSettingsDto(
    decimal VatPercent,
    decimal ServiceChargePercent,
    string Currency,
    bool TipEnabled,
    decimal HighDiscountThresholdPercent);

// ----- Query -----

/// <summary>Reads the restaurant-wide billing defaults (falls back to sensible defaults if unseeded).</summary>
public record GetBillingSettingsQuery : IRequest<BillingSettingsDto>;

public class GetBillingSettingsQueryHandler : IRequestHandler<GetBillingSettingsQuery, BillingSettingsDto>
{
    private readonly IAppDbContext _db;
    public GetBillingSettingsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<BillingSettingsDto> Handle(GetBillingSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await _db.RestaurantBillingSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (s is null) return new BillingSettingsDto(0m, 0m, "Tk", true, 20m);
        return new BillingSettingsDto(s.VatPercent, s.ServiceChargePercent, s.Currency, s.TipEnabled, s.HighDiscountThresholdPercent);
    }
}

// ----- Update -----

public record UpdateBillingSettingsCommand(
    decimal VatPercent,
    decimal ServiceChargePercent,
    string Currency,
    bool TipEnabled,
    decimal HighDiscountThresholdPercent) : IRequest<BillingSettingsDto>;

public class UpdateBillingSettingsCommandValidator : AbstractValidator<UpdateBillingSettingsCommand>
{
    public UpdateBillingSettingsCommandValidator()
    {
        RuleFor(x => x.VatPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ServiceChargePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.HighDiscountThresholdPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(8);
    }
}

public class UpdateBillingSettingsCommandHandler : IRequestHandler<UpdateBillingSettingsCommand, BillingSettingsDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateBillingSettingsCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BillingSettingsDto> Handle(UpdateBillingSettingsCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin);

        var settings = await _db.RestaurantBillingSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = RestaurantBillingSettings.CreateDefault();
            _db.RestaurantBillingSettings.Add(settings);
        }

        try
        {
            settings.Update(request.VatPercent, request.ServiceChargePercent, request.Currency, request.TipEnabled, request.HighDiscountThresholdPercent);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new BillingSettingsDto(settings.VatPercent, settings.ServiceChargePercent, settings.Currency, settings.TipEnabled, settings.HighDiscountThresholdPercent);
    }
}
