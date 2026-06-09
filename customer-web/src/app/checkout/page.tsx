"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { readCart, clearCart, cartTotal, type CartItem } from "@/lib/cart";
import { formatMoney } from "@/lib/format";

export default function CheckoutPage() {
  const router = useRouter();
  const [cart, setCart] = useState<CartItem[]>([]);
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setCart(readCart());
  }, []);

  const currency = cart[0]?.currency || "Tk";

  async function placeOrder() {
    setSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/orders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          type: "Takeaway",
          tableId: null,
          notes: notes.trim() || null,
          lines: cart.map((c) => ({ menuItemId: c.menuItemId, quantity: c.quantity })),
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
          <div key={c.menuItemId} className="flex justify-between text-sm">
            <span>{c.name} × {c.quantity}</span>
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
        <div className="text-sm text-slate-600">Takeaway</div>
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
