"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { readCart, setQuantity, cartTotal, type CartItem } from "@/lib/cart";
import { formatMoney } from "@/lib/format";

export default function CartPage() {
  const [cart, setCart] = useState<CartItem[]>([]);

  useEffect(() => {
    const update = () => setCart(readCart());
    update();
    window.addEventListener("bb_cart_changed", update);
    return () => window.removeEventListener("bb_cart_changed", update);
  }, []);

  const currency = cart[0]?.currency || "Tk";

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Your Cart</h1>

      {cart.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-6 text-center text-sm text-slate-500">
          Cart is empty. <Link href="/menu" className="text-emerald-700 underline">Browse the menu</Link>.
        </div>
      ) : (
        <>
          <div className="space-y-2">
            {cart.map((item) => (
              <div key={item.menuItemId} className="flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4">
                <div>
                  <div className="font-medium">{item.name}</div>
                  <div className="text-sm text-slate-500">{formatMoney(item.price, item.currency)} each</div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-2">
                    <button
                      className="h-7 w-7 rounded-md border border-slate-300 hover:bg-slate-50"
                      onClick={() => setQuantity(item.menuItemId, item.quantity - 1)}
                    >
                      −
                    </button>
                    <span className="w-6 text-center">{item.quantity}</span>
                    <button
                      className="h-7 w-7 rounded-md border border-slate-300 hover:bg-slate-50"
                      onClick={() => setQuantity(item.menuItemId, item.quantity + 1)}
                    >
                      +
                    </button>
                  </div>
                  <div className="w-24 text-right font-semibold">
                    {formatMoney(item.price * item.quantity, item.currency)}
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div className="flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4">
            <span className="font-semibold">Total</span>
            <span className="text-lg font-bold text-emerald-700">{formatMoney(cartTotal(cart), currency)}</span>
          </div>

          <Link
            href="/checkout"
            className="block rounded-lg bg-emerald-600 px-4 py-3 text-center font-medium text-white hover:bg-emerald-700"
          >
            Proceed to Checkout
          </Link>
        </>
      )}
    </div>
  );
}
