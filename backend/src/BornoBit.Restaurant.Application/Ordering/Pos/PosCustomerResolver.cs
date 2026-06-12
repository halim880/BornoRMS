using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// Find-or-create customer resolution shared by the POS order commands: looks up by phone
/// (the shared walk-in customer when no phone is given), fills in a missing name, and keeps
/// the latest address — but never writes personal details onto the shared walk-in record.
/// </summary>
internal static class PosCustomerResolver
{
    public static async Task<Guid> ResolveAsync(
        IAppDbContext db, string? phone, string? name, string? address, CancellationToken cancellationToken)
    {
        var lookup = string.IsNullOrWhiteSpace(phone) ? Customer.WalkInPhone : phone.Trim();

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Phone == lookup, cancellationToken);
        if (customer is null)
        {
            customer = Customer.Create(lookup, name);
            if (lookup != Customer.WalkInPhone && !string.IsNullOrWhiteSpace(address))
                customer.UpdateAddress(address);
            db.Customers.Add(customer);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (lookup != Customer.WalkInPhone)
        {
            if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(customer.FullName))
                customer.UpdateName(name);
            if (!string.IsNullOrWhiteSpace(address))
                customer.UpdateAddress(address);
            await db.SaveChangesAsync(cancellationToken);
        }

        return customer.Id;
    }
}
