"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { readCart, clearCart, cartTotal, lineKey, type CartItem } from "@/lib/cart";
import { readTable, clearTable, type TableInfo } from "@/lib/table";
import { formatMoney } from "@/lib/format";

export default function CheckoutPage() {
  const router = useRouter();
  const [cart, setCart] = useState<CartItem[]>([]);
  const [table, setTable] = useState<TableInfo | null>(null);
  const [notes, setNotes] = useState("");
  // When there's no table, the guest chooses takeaway or delivery.
  const [fulfillment, setFulfillment] = useState<"Takeaway" | "Delivery">("Takeaway");
  const [address, setAddress] = useState("");
  const [phone, setPhone] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const orderType = table ? "DineIn" : fulfillment;
  const isDelivery = orderType === "Delivery";

  useEffect(() => {
    setCart(readCart());
    const updateTable = () => setTable(readTable());
    updateTable();
    window.addEventListener("bb_table_changed", updateTable);
    return () => window.removeEventListener("bb_table_changed", updateTable);
  }, []);

  const currency = cart[0]?.currency || "Tk";

  async function placeOrder() {
    if (isDelivery && address.trim().length === 0) {
      setError("Please enter a delivery address.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/orders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          type: orderType,
          tableId: table?.id ?? null,
          notes: notes.trim() || null,
          deliveryAddress: isDelivery ? address.trim() : null,
          contactPhone: isDelivery && phone.trim() ? phone.trim() : null,
          lines: cart.map((c) => ({
            menuItemId: c.menuItemId,
            variantId: c.variantId,
            quantity: c.quantity,
            optionIds: c.options.map((o) => o.optionId),
          })),
        }),
      });
      const data = await res.json();
      if (!res.ok) {
        setError(data.message || "Could not place order.");
        setSubmitting(false);
        return;
      }
      clearCart();
      router.push(`/orders/${data.orderId}`);
    } catch {
      setError("Network error. Please try again.");
      setSubmitting(false);
    }
  }

  if (cart.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white px-4 py-6 text-center text-sm text-slate-500">
        Your cart is empty.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Checkout</h1>

      <div className="space-y-2 rounded-xl border border-slate-200 bg-white p-4">
        {cart.map((c) => (
          <div key={lineKey(c)} className="flex justify-between text-sm">
            <span>
              {c.name}
              {c.variantName ? ` (${c.variantName})` : ""} × {c.quantity}
              {c.options.length > 0 && (
                <span className="block text-xs text-emerald-700">
                  {c.options.map((o) => (o.priceDelta > 0 ? `${o.name} (+${formatMoney(o.priceDelta, c.currency)})` : o.name)).join(", ")}
                </span>
              )}
            </span>
            <span>{formatMoney(c.price * c.quantity, c.currency)}</span>
          </div>
        ))}
        <div className="mt-2 flex justify-between border-t border-slate-100 pt-2 font-semibold">
          <span>Total</span>
          <span className="text-emerald-700">{formatMoney(cartTotal(cart), currency)}</span>
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <div className="mb-1 text-sm font-medium">Order type</div>
        {table ? (
          <div className="flex items-center justify-between text-sm text-slate-600">
            <span>
              Dine-in — <strong>Table {table.tableNumber}</strong>
            </span>
            <button
              type="button"
              onClick={clearTable}
              className="rounded-md border border-slate-300 px-2 py-1 text-xs hover:bg-slate-50"
            >
              Remove table (switch to takeaway)
            </button>
          </div>
        ) : (
          <div className="space-y-3">
            <div className="flex gap-2">
              {(["Takeaway", "Delivery"] as const).map((opt) => (
                <button
                  key={opt}
                  type="button"
                  onClick={() => setFulfillment(opt)}
                  className={`rounded-md border px-3 py-1.5 text-sm ${
                    fulfillment === opt
                      ? "border-emerald-600 bg-emerald-50 font-medium text-emerald-700"
                      : "border-slate-300 text-slate-600 hover:bg-slate-50"
                  }`}
                >
                  {opt}
                </button>
              ))}
            </div>
            {isDelivery && (
              <div className="space-y-2">
                <div>
                  <label className="block text-sm font-medium">Delivery address</label>
                  <textarea
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    rows={2}
                    className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                    placeholder="House, road, area"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium">Contact phone (optional)</label>
                  <input
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                    placeholder="01XXXXXXXXX"
                  />
                </div>
                <p className="text-xs text-slate-500">A delivery charge will be added to your bill.</p>
              </div>
            )}
          </div>
        )}
        <label className="mt-4 block text-sm font-medium">Notes (optional)</label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
          placeholder="e.g. extra spicy, no onions"
        />
      </div>

      {error && (
        <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
      )}

      <button
        onClick={placeOrder}
        disabled={submitting}
        className="block w-full rounded-lg bg-emerald-600 px-4 py-3 text-center font-medium text-white hover:bg-emerald-700 disabled:opacity-60"
      >
        {submitting ? "Placing order…" : "Place Order"}
      </button>
    </div>
  );
}
