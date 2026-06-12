using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// A bucket a transaction is filed under (e.g. Sales, Salaries, Rent). Each category is
/// fixed to one <see cref="TransactionType"/> so income and expense lists stay separate.
/// </summary>
public class FinanceCategory : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public TransactionType Type { get; private set; }
    public bool IsActive { get; private set; } = true;

    private FinanceCategory() { }

    public static FinanceCategory Create(string name, TransactionType type)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        return new FinanceCategory
        {
            Name = name.Trim(),
            Type = type,
            IsActive = true
        };
    }

    public void Update(string name, TransactionType type)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
        Type = type;
    }

    public void SetActive(bool active) => IsActive = active;
}
