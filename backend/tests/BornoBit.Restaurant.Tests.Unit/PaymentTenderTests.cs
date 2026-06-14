using BornoBit.Restaurant.Domain.Ordering;
using Xunit;

namespace BornoBit.Restaurant.Tests.Unit;

/// <summary>Single-tender rules on the Payment value: cash change vs. exact non-cash capture.</summary>
public class PaymentTenderTests
{
    [Fact]
    public void Cash_tender_records_change()
    {
        var p = Payment.Capture(Guid.NewGuid(), PaymentMethod.Cash, null, amount: 180m, tendered: 200m, null, "cashier");

        Assert.Equal(180m, p.Amount);
        Assert.Equal(200m, p.Tendered);
        Assert.Equal(20m, p.Change);
        Assert.Equal(PaymentKind.Charge, p.Kind);
        Assert.Equal(PaymentEntryStatus.Captured, p.Status);
    }

    [Fact]
    public void Noncash_tender_is_forced_to_the_exact_amount()
    {
        var p = Payment.Capture(Guid.NewGuid(), PaymentMethod.Card, null, amount: 180m, tendered: 999m, null, "cashier");

        Assert.Equal(180m, p.Amount);
        Assert.Equal(180m, p.Tendered);   // tendered coerced to amount for non-cash
        Assert.Equal(0m, p.Change);
    }

    [Fact]
    public void Cash_tender_below_amount_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Payment.Capture(Guid.NewGuid(), PaymentMethod.Cash, null, amount: 180m, tendered: 100m, null, "cashier"));
    }

    [Fact]
    public void Voided_charge_contributes_zero_signed_amount()
    {
        var p = Payment.Capture(Guid.NewGuid(), PaymentMethod.Cash, null, 180m, 180m, null, "cashier");
        p.Void("mistake");

        Assert.Equal(PaymentEntryStatus.Voided, p.Status);
        Assert.Equal(0m, p.SignedAmount);
    }
}
