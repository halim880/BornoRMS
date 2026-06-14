using BornoBit.Restaurant.Domain.Ordering;
using Xunit;

namespace BornoBit.Restaurant.Tests.Unit;

/// <summary>Split / partial / overpay / refund money-math over the Order aggregate (no DB).</summary>
public class OrderPaymentTests
{
    // 2 × 100 = 200 subtotal, no tax/discount → GrandTotal 200.
    private static Order NewOrder(decimal unitPrice = 100m, int qty = 2)
    {
        var order = Order.Create("ORD-20260614-0001", Guid.NewGuid(), null, OrderType.Takeaway, DateTime.UtcNow, "Tk");
        order.AddLine(Guid.NewGuid(), "P1", "Burger", unitPrice, "Tk", qty);
        return order;
    }

    [Fact]
    public void GrandTotal_is_sum_of_lines()
    {
        var order = NewOrder();
        Assert.Equal(200m, order.GrandTotal);
        Assert.Equal(200m, order.BalanceDue);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
    }

    [Fact]
    public void Partial_payment_leaves_order_PartiallyPaid()
    {
        var order = NewOrder();

        order.AddPayment(PaymentMethod.Cash, null, amount: 120m, tendered: 120m, null, "cashier");

        Assert.Equal(120m, order.AmountPaid);
        Assert.Equal(80m, order.BalanceDue);
        Assert.Equal(PaymentStatus.PartiallyPaid, order.PaymentStatus);
        Assert.False(order.IsPaid);
    }

    [Fact]
    public void Split_cash_plus_mobile_settles_in_full()
    {
        var order = NewOrder();

        order.AddPayment(PaymentMethod.Cash, null, 120m, 120m, null, "cashier");
        order.AddPayment(PaymentMethod.Mobile, PaymentProvider.Bkash, 80m, 80m, null, "cashier", reference: "TX1");

        Assert.Equal(200m, order.AmountPaid);
        Assert.Equal(0m, order.BalanceDue);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.True(order.IsPaid);
    }

    [Fact]
    public void Cash_overpay_is_clamped_and_returns_change()
    {
        var order = NewOrder();

        var payment = order.AddPayment(PaymentMethod.Cash, null, amount: 500m, tendered: 500m, null, "cashier");

        // Applied amount is clamped to the 200 balance; the excess becomes change on the tender.
        Assert.Equal(200m, payment.Amount);
        Assert.Equal(300m, payment.Change);
        Assert.Equal(200m, order.AmountPaid);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
    }

    [Fact]
    public void Noncash_cannot_exceed_balance()
    {
        var order = NewOrder();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            order.AddPayment(PaymentMethod.Card, null, amount: 500m, tendered: 500m, null, "cashier"));

        Assert.Contains("exceeds the balance", ex.Message);
    }

    [Fact]
    public void Paying_an_already_paid_order_throws()
    {
        var order = NewOrder();
        order.AddPayment(PaymentMethod.Cash, null, 200m, 200m, null, "cashier");

        Assert.Throws<InvalidOperationException>(() =>
            order.AddPayment(PaymentMethod.Cash, null, 10m, 10m, null, "cashier"));
    }

    [Fact]
    public void Void_reverses_payment_back_to_pending()
    {
        var order = NewOrder();
        var payment = order.AddPayment(PaymentMethod.Cash, null, 200m, 200m, null, "cashier");

        order.VoidPayment(payment.Id, "keyed wrong");

        Assert.Equal(0m, order.AmountPaid);
        Assert.Equal(200m, order.BalanceDue);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
    }

    [Fact]
    public void Full_refund_marks_order_Refunded()
    {
        var order = NewOrder();
        var payment = order.AddPayment(PaymentMethod.Cash, null, 200m, 200m, null, "cashier");

        var refund = order.RefundPayment(payment.Id, 200m, "customer returned", null, "cashier");

        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
        Assert.Equal(PaymentKind.Refund, refund.Kind);
        Assert.Equal(PaymentEntryStatus.Refunded, payment.Status); // original charge marked refunded
    }

    [Fact]
    public void Discount_percent_uses_decimal_rounding()
    {
        var order = NewOrder(unitPrice: 33.33m, qty: 3); // subtotal 99.99
        order.ApplyDiscount(percent: 10m, amount: null, reason: "loyalty");

        Assert.Equal(10.00m, order.DiscountAmount);     // round(99.99 * 0.10, 2)
        Assert.Equal(89.99m, order.GrandTotal);
    }
}
