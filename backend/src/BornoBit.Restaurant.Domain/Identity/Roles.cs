namespace BornoBit.Restaurant.Domain.Identity;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Waiter = "Waiter";
    public const string Chef = "Chef";
    public const string Cashier = "Cashier";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        SuperAdmin, Admin, Manager, Waiter, Chef, Cashier
    };

    // Staff roles allowed to view/manage orders in the admin module.
    public static IReadOnlyList<string> StaffOrderManagers { get; } = new[]
    {
        SuperAdmin, Admin, Manager, Waiter, Chef, Cashier
    };
}
