namespace BornoBit.Restaurant.Application.Inventory.Suppliers;

public record SupplierDto(
    Guid Id,
    string Code,
    string Name,
    string? Phone,
    string? Address,
    int PaymentTermsDays,
    string? Notes,
    bool IsActive);
